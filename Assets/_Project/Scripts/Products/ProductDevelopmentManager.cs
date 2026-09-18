using UnityEngine;
using System;
using System.Collections.Generic;
using GameDevStudio.AI;
using GameDevStudio.Characters;
using GameDevStudio.Core;
using GameDevStudio.Economy;
using GameDevStudio.Save;

namespace GameDevStudio.Products
{
    public enum ProductDevelopmentState
    {
        None,
        Developing,
        Paused,
        Completed
    }

    /// <summary>
    /// Historical record of a completed product. Single unified source of truth for future Reviews & Sales.
    /// </summary>
    [Serializable]
    public class ProductHistoryEntry
    {
        public string      ProductId;
        public string      ProductName;
        public ProductType ProductType;
        public int         FinalQualityScore;
        public int         CompletionDay;
        public int         TotalCostSpent;
        public float       TotalDevelopmentTimeDays;
        public List<string> UsedTechnologies = new List<string>();
        public int         TeamSizeAtCompletion;
    }

    /// <summary>
    /// Singleton. Manages active product development lifecycle, progress ticking via GameTimeManager,
    /// employee contributions with diminishing returns, and completed product history.
    /// </summary>
    public class ProductDevelopmentManager : MonoBehaviour
    {
        public static ProductDevelopmentManager Instance { get; private set; }

        // ── Active Development State ─────────────────────────────────────────
        public ProductData             ActiveProduct          { get; private set; }
        public float                   ActiveProgress         { get; private set; } // 0.0 - 1.0
        public ProductDevelopmentState DevelopmentState       { get; private set; } = ProductDevelopmentState.None;
        public float                   AccumulatedQuality     { get; private set; }
        public int                     TotalCostSpent         { get; private set; }
        public float                   ElapsedDevelopmentDays { get; private set; }

        private readonly List<ProductHistoryEntry> _completedProducts = new List<ProductHistoryEntry>();
        public IReadOnlyList<ProductHistoryEntry> CompletedProducts => _completedProducts;

        // ── Events ────────────────────────────────────────────────────────────
        public event Action<ProductData>                OnProductStarted;
        public event Action<ProductData, float>         OnProductProgressUpdated;
        public event Action<ProductData>                OnProductPaused;
        public event Action<ProductData>                OnProductResumed;
        public event Action<ProductHistoryEntry>       OnProductCompleted;
        public event Action<string>                     OnProductFailed;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ── Public API ────────────────────────────────────────────────────────

        public bool CanStartDevelopment(ProductData product, out string reason)
        {
            reason = "";
            if (product == null) { reason = "No product selected."; return false; }
            if (DevelopmentState == ProductDevelopmentState.Developing || DevelopmentState == ProductDevelopmentState.Paused)
            {
                reason = $"Development already active for {ActiveProduct?.productName ?? "another product"}.";
                return false;
            }

            // Tech & Capability Check
            if (ProductDatabase.Instance != null && !ProductDatabase.Instance.IsProductUnlocked(product))
            {
                reason = "Required technology or capabilities not unlocked.";
                return false;
            }

            // Money Check
            if (MoneyManager.Instance == null || !MoneyManager.Instance.HasEnoughMoney(product.baseCost))
            {
                reason = $"Insufficient funds. Need ${product.baseCost:N0}.";
                return false;
            }

            // Employee Count Check
            int workingEmps = GetWorkingEmployeeCount();
            if (workingEmps < product.minEmployeesRequired)
            {
                reason = $"Need at least {product.minEmployeesRequired} working employee(s). Currently {workingEmps}.";
                return false;
            }

            // Infrastructure Capacity Threshold Checks
            if (AICore.Instance != null)
            {
                if (AICore.Instance.CurrentComputeCapacity < product.minComputeRequired)
                {
                    reason = $"Insufficient Compute Capacity. Need {product.minComputeRequired}, have {AICore.Instance.CurrentComputeCapacity}.";
                    return false;
                }
                if (AICore.Instance.CurrentEnergyCapacity < product.minEnergyRequired)
                {
                    reason = $"Insufficient Energy Capacity. Need {product.minEnergyRequired}, have {AICore.Instance.CurrentEnergyCapacity}.";
                    return false;
                }
            }

            return true;
        }

        public bool TryStartDevelopment(ProductData product)
        {
            if (!CanStartDevelopment(product, out string failReason))
            {
                Debug.LogWarning($"[ProductDevManager] Cannot start development: {failReason}");
                OnProductFailed?.Invoke(failReason);
                return false;
            }

            // Spend cost
            if (MoneyManager.Instance != null && !MoneyManager.Instance.TrySpendMoney(product.baseCost))
            {
                OnProductFailed?.Invoke($"Failed to spend ${product.baseCost:N0}.");
                return false;
            }

            ActiveProduct          = product;
            ActiveProgress         = 0f;
            AccumulatedQuality     = 0f;
            TotalCostSpent         = product.baseCost;
            ElapsedDevelopmentDays = 0f;
            DevelopmentState       = ProductDevelopmentState.Developing;

            Debug.Log($"[ProductDevManager] Development started: '{product.productName}' (${product.baseCost}, {product.baseDevelopmentDays} days)");
            OnProductStarted?.Invoke(product);
            return true;
        }

        public void PauseDevelopment()
        {
            if (DevelopmentState != ProductDevelopmentState.Developing || ActiveProduct == null) return;
            DevelopmentState = ProductDevelopmentState.Paused;
            Debug.Log($"[ProductDevManager] Development paused: '{ActiveProduct.productName}'");
            OnProductPaused?.Invoke(ActiveProduct);
        }

        public void ResumeDevelopment()
        {
            if (DevelopmentState != ProductDevelopmentState.Paused || ActiveProduct == null) return;
            DevelopmentState = ProductDevelopmentState.Developing;
            Debug.Log($"[ProductDevManager] Development resumed: '{ActiveProduct.productName}'");
            OnProductResumed?.Invoke(ActiveProduct);
        }

        public void CancelDevelopment()
        {
            if (ActiveProduct == null) return;
            Debug.Log($"[ProductDevManager] Development cancelled for '{ActiveProduct.productName}'");
            ActiveProduct    = null;
            ActiveProgress   = 0f;
            DevelopmentState = ProductDevelopmentState.None;
        }

        // ── GameTime Update Loop ──────────────────────────────────────────────
        private void Update()
        {
            if (DevelopmentState != ProductDevelopmentState.Developing || ActiveProduct == null) return;
            if (GameTimeManager.Instance == null || GameTimeManager.Instance.IsPaused) return;

            float speedMult = GameTimeManager.Instance.CurrentSpeedMultiplier;
            float dt        = Time.deltaTime * speedMult;

            // Time scaling: 1 game-day = 24h * 60m * 0.1s = 144 real-seconds at 1x speed
            float realSecPerGameDay = 144f;
            float baseDaysPerRealSec = 1f / (ActiveProduct.baseDevelopmentDays * realSecPerGameDay);

            // Employee diminishing returns team multiplier
            float empMultiplier = CalculateEmployeeTeamMultiplier();

            // AI Core Speed bonus
            float aiSpeedFactor = 1.0f;
            if (AICore.Instance != null)
            {
                aiSpeedFactor = 1.0f + (AICore.Instance.Speed * 0.005f); // up to +50% speed
            }

            float dayIncrement = baseDaysPerRealSec * empMultiplier * aiSpeedFactor * dt;
            ElapsedDevelopmentDays += dayIncrement * ActiveProduct.baseDevelopmentDays;
            ActiveProgress = Mathf.Clamp01(ActiveProgress + dayIncrement);

            // Quality buildup based on working employee skills and AI Core Quality/Creativity
            AccumulateQualityTick(dt);

            OnProductProgressUpdated?.Invoke(ActiveProduct, ActiveProgress);

            if (ActiveProgress >= 1.0f)
            {
                CompleteDevelopment();
            }
        }

        private void CompleteDevelopment()
        {
            if (ActiveProduct == null) return;

            int currentDay = GameTimeManager.Instance != null ? GameTimeManager.Instance.CurrentDay : 1;
            int workingCount = GetWorkingEmployeeCount();

            // Calculate final quality score (0-100)
            int aiQuality = AICore.Instance != null ? AICore.Instance.Quality : 20;
            int aiPrimary = AICore.Instance != null ? AICore.Instance.GetStat(ActiveProduct.primaryAIStat) : 20;
            float rawQuality = (ActiveProduct.targetQuality * 0.4f) + (aiQuality * 0.3f) + (aiPrimary * 0.3f) + (AccumulatedQuality * 0.1f);
            int finalQualityScore = Mathf.Clamp(Mathf.RoundToInt(rawQuality), 10, 100);

            var entry = new ProductHistoryEntry
            {
                ProductId                = ActiveProduct.Id,
                ProductName              = ActiveProduct.productName,
                ProductType              = ActiveProduct.productType,
                FinalQualityScore        = finalQualityScore,
                CompletionDay            = currentDay,
                TotalCostSpent           = TotalCostSpent,
                TotalDevelopmentTimeDays = ElapsedDevelopmentDays,
                TeamSizeAtCompletion     = workingCount,
                UsedTechnologies         = new List<string>(ActiveProduct.requiredTechIds ?? Array.Empty<string>())
            };

            _completedProducts.Add(entry);

            // Passive AI Learning gain upon completing a product
            if (AICore.Instance != null)
            {
                AICore.Instance.ImproveStat(AIStatType.Learning, 2);
            }

            Debug.Log($"[ProductDevManager] COMPLETED PRODUCT: '{entry.ProductName}' (Quality: {entry.FinalQualityScore}/100, Cost: ${entry.TotalCostSpent})");

            var completedProduct = ActiveProduct;
            ActiveProduct    = null;
            ActiveProgress   = 1.0f;
            DevelopmentState = ProductDevelopmentState.Completed;

            OnProductCompleted?.Invoke(entry);
        }

        // ── Quality & Team Multiplier Calculation ────────────────────────────
        private float CalculateEmployeeTeamMultiplier()
        {
            int working = GetWorkingEmployeeCount();
            if (working <= 0) return 0.1f; // Slow passive progress even without active seated workers

            float mult = 0f;
            float[] contributions = { 1.0f, 0.7f, 0.5f, 0.3f };

            for (int i = 0; i < working; i++)
            {
                int idx = Mathf.Min(i, contributions.Length - 1);
                mult += contributions[idx];
            }
            return mult;
        }

        private void AccumulateQualityTick(float dt)
        {
            if (EmployeeManager.Instance == null) return;
            foreach (var emp in EmployeeManager.Instance.Employees)
            {
                if (emp == null || emp.Data == null) continue;
                if (emp.CurrentState == EmployeeState.WorkingSeated || emp.CurrentState == EmployeeState.WorkingStanding)
                {
                    int skill = emp.Data.GetSkillFor(ActiveProduct.primaryAIStat);
                    AccumulatedQuality += (skill * 0.01f) * dt;
                }
            }
        }

        public int GetWorkingEmployeeCount()
        {
            if (EmployeeManager.Instance == null) return 0;
            int count = 0;
            foreach (var emp in EmployeeManager.Instance.Employees)
            {
                if (emp == null) continue;
                if (emp.CurrentState == EmployeeState.WorkingSeated || emp.CurrentState == EmployeeState.WorkingStanding)
                    count++;
            }
            return count;
        }

        public float GetEstimatedRemainingDays()
        {
            if (ActiveProduct == null) return 0f;
            float remainingProgress = 1.0f - ActiveProgress;
            float empMult = Mathf.Max(0.1f, CalculateEmployeeTeamMultiplier());
            return (remainingProgress * ActiveProduct.baseDevelopmentDays) / empMult;
        }

        // ── Save / Load Persistence ──────────────────────────────────────────

        public ProductDevelopmentSaveData GatherSaveData()
        {
            var data = new ProductDevelopmentSaveData
            {
                ActiveProductId        = ActiveProduct != null ? ActiveProduct.Id : "",
                ActiveProgress         = ActiveProgress,
                DevelopmentState       = (int)DevelopmentState,
                AccumulatedQuality     = AccumulatedQuality,
                TotalCostSpent         = TotalCostSpent,
                ElapsedDevelopmentDays = ElapsedDevelopmentDays
            };

            if (ActiveProduct != null && ActiveProduct.requiredTechIds != null)
            {
                data.UsedTechnologyIds.AddRange(ActiveProduct.requiredTechIds);
            }

            // Gather IDs of working assigned employees
            if (EmployeeManager.Instance != null)
            {
                foreach (var emp in EmployeeManager.Instance.Employees)
                {
                    if (emp == null || emp.Data == null) continue;
                    if (emp.CurrentState == EmployeeState.WorkingSeated || emp.CurrentState == EmployeeState.WorkingStanding)
                    {
                        data.AssignedEmployeeIds.Add(emp.Data.employeeId);
                    }
                }
            }

            // Copy completed products history
            foreach (var entry in _completedProducts)
            {
                if (entry == null) continue;
                data.CompletedProducts.Add(new CompletedProductSaveEntry
                {
                    ProductId                = entry.ProductId,
                    ProductName              = entry.ProductName,
                    ProductType              = (int)entry.ProductType,
                    FinalQuality             = entry.FinalQualityScore,
                    CompletionDay            = entry.CompletionDay,
                    TotalCostSpent           = entry.TotalCostSpent,
                    TotalDevelopmentDays     = entry.TotalDevelopmentTimeDays
                });
            }

            return data;
        }

        public void LoadState(ProductDevelopmentSaveData data)
        {
            ActiveProduct    = null;
            ActiveProgress   = 0f;
            DevelopmentState = ProductDevelopmentState.None;
            _completedProducts.Clear();

            if (data == null) return;

            // Restore Completed Products history single source of truth
            if (data.CompletedProducts != null)
            {
                foreach (var saveEntry in data.CompletedProducts)
                {
                    if (saveEntry == null) continue;
                    _completedProducts.Add(new ProductHistoryEntry
                    {
                        ProductId                = saveEntry.ProductId,
                        ProductName              = saveEntry.ProductName,
                        ProductType              = (ProductType)saveEntry.ProductType,
                        FinalQualityScore        = saveEntry.FinalQuality,
                        CompletionDay            = saveEntry.CompletionDay,
                        TotalCostSpent           = saveEntry.TotalCostSpent,
                        TotalDevelopmentTimeDays = saveEntry.TotalDevelopmentDays
                    });
                }
            }

            // Restore active development state if any
            if (!string.IsNullOrEmpty(data.ActiveProductId) && ProductDatabase.Instance != null)
            {
                var product = ProductDatabase.Instance.GetById(data.ActiveProductId);
                if (product != null)
                {
                    ActiveProduct          = product;
                    ActiveProgress         = Mathf.Clamp01(data.ActiveProgress);
                    DevelopmentState       = (ProductDevelopmentState)data.DevelopmentState;
                    AccumulatedQuality     = data.AccumulatedQuality;
                    TotalCostSpent         = data.TotalCostSpent;
                    ElapsedDevelopmentDays = data.ElapsedDevelopmentDays;

                    Debug.Log($"[ProductDevManager] Restored active development: '{product.productName}' ({ActiveProgress * 100f:F1}%)");
                    OnProductStarted?.Invoke(product);
                }
            }

            Debug.Log($"[ProductDevManager] Loaded {data.CompletedProducts?.Count ?? 0} completed product records from save.");
        }

        /// <summary>
        /// Resets all active product development and completed product history for a New Game.
        /// </summary>
        public void ResetState()
        {
            ActiveProduct          = null;
            ActiveProgress         = 0f;
            DevelopmentState       = ProductDevelopmentState.None;
            AccumulatedQuality     = 0f;
            TotalCostSpent         = 0;
            ElapsedDevelopmentDays = 0f;
            _completedProducts.Clear();

            Debug.Log("[ProductDevManager] Reset state to initial default for New Game.");
        }
    }
}
