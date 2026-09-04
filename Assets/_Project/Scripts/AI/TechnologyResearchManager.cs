using UnityEngine;
using System.Collections.Generic;
using GameDevStudio.Characters;
using GameDevStudio.Economy;
using GameDevStudio.Core;

namespace GameDevStudio.AI
{
    /// <summary>
    /// Manages the single active research slot.
    /// Research progress is driven by game-time and employee contributions
    /// with diminishing returns for additional employees.
    ///
    /// Diminishing returns model:
    ///   1st employee → 100%
    ///   2nd employee → +70%
    ///   3rd employee → +50%
    ///   4th+ employee → +30% each
    /// </summary>
    public class TechnologyResearchManager : MonoBehaviour
    {
        public static TechnologyResearchManager Instance { get; private set; }

        // ── Active research state ────────────────────────────────────────────
        public TechnologyData ActiveResearch { get; private set; }

        /// Progress 0.0 – 1.0
        public float ActiveProgress => ActiveResearch?.ResearchProgress ?? 0f;

        // ── Events ────────────────────────────────────────────────────────────
        public event System.Action<TechnologyData>        OnResearchStarted;
        public event System.Action<TechnologyData, float> OnResearchProgress;  // (tech, progress 0-1)
        public event System.Action<TechnologyData>        OnResearchCompleted;
        public event System.Action<string>                OnResearchFailed;    // reason message

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ── Start Research ────────────────────────────────────────────────────
        public bool TryStartResearch(TechnologyData tech)
        {
            if (tech == null) { Fail("No technology selected."); return false; }
            if (tech.IsCompleted) { Fail($"{tech.Name} is already completed."); return false; }
            if (ActiveResearch != null) { Fail("Another research is already active."); return false; }

            // Prerequisites
            if (TechnologyDatabase.Instance != null)
                TechnologyDatabase.Instance.RefreshAvailability();

            if (tech.IsLocked)
            {
                Fail($"{tech.Name} is locked. Complete prerequisites first.");
                return false;
            }

            // Money
            if (MoneyManager.Instance == null || !MoneyManager.Instance.HasEnoughMoney(tech.MoneyCost))
            {
                Fail($"Insufficient funds. Need ${tech.MoneyCost:N0}.");
                return false;
            }

            // Employees
            int workingCount = CountWorkingEmployees();
            if (workingCount < tech.MinEmployees)
            {
                Fail($"Need at least {tech.MinEmployees} working employee(s). Currently {workingCount}.");
                return false;
            }

            // Compute
            if (AICore.Instance != null && tech.ComputeRequired > AICore.Instance.CurrentComputeCapacity)
            {
                Fail($"Insufficient Compute. Need {tech.ComputeRequired}, have {AICore.Instance.CurrentComputeCapacity}.");
                return false;
            }

            // Energy
            if (AICore.Instance != null && tech.EnergyRequired > AICore.Instance.CurrentEnergyCapacity)
            {
                Fail($"Insufficient Energy. Need {tech.EnergyRequired}, have {AICore.Instance.CurrentEnergyCapacity}.");
                return false;
            }

            // Deduct cost exactly once
            if (!MoneyManager.Instance.TrySpendMoney(tech.MoneyCost))
            {
                Fail($"Failed to spend ${tech.MoneyCost:N0}.");
                return false;
            }

            // Begin
            tech.State            = TechState.Researching;
            tech.ResearchProgress = 0f;
            ActiveResearch        = tech;

            Debug.Log($"[TechResearch] Started: {tech.Name} ({tech.ResearchTimeDays} game-days, ${tech.MoneyCost})");
            OnResearchStarted?.Invoke(tech);
            return true;
        }

        // ── Update — game-time driven progress ───────────────────────────────
        private void Update()
        {
            if (ActiveResearch == null) return;
            if (GameTimeManager.Instance == null || GameTimeManager.Instance.IsPaused) return;

            float speedMult = GameTimeManager.Instance.CurrentSpeedMultiplier;
            float dt = Time.deltaTime * speedMult;

            // 1 game-day = 24 game-hours.
            // GameTimeManager: secondsPerGameMinute = 0.1 → 1 game-hour = 6 real-seconds at 1x
            // So 1 game-day = 24h * 60min = 1440 game-minutes.
            // In real-seconds at 1x: 1440 * 0.1 = 144 real-seconds per game-day.
            // research progress per real-second at 1x = 1 / (researchDays * 144)
            float progressPerRealSecond = 1f / (ActiveResearch.ResearchTimeDays * 144f);

            // Employee contribution (diminishing returns)
            float empMultiplier = CalculateEmployeeMultiplier();

            // Base contribution already accounts for no employees (min 1 required to start)
            // But if all employees log off mid-research, progress slows but doesn't stop at 0.
            float increment = progressPerRealSecond * empMultiplier * dt;
            ActiveResearch.ResearchProgress = Mathf.Clamp01(ActiveResearch.ResearchProgress + increment);

            OnResearchProgress?.Invoke(ActiveResearch, ActiveResearch.ResearchProgress);

            if (ActiveResearch.ResearchProgress >= 1f)
                CompleteResearch();
        }

        // ── Completion ────────────────────────────────────────────────────────
        private void CompleteResearch()
        {
            var completed = ActiveResearch;
            ActiveResearch = null;

            if (TechnologyDatabase.Instance != null)
                TechnologyDatabase.Instance.MarkCompleted(completed);
            else
                completed.State = TechState.Completed;

            Debug.Log($"[TechResearch] COMPLETED: {completed.Name}");
            OnResearchCompleted?.Invoke(completed);
        }

        // ── Diminishing returns ───────────────────────────────────────────────
        private float CalculateEmployeeMultiplier()
        {
            int working = CountWorkingEmployees();
            if (working <= 0) return 0.1f; // Slow passive progress even without workers

            float mult = 0f;
            float[] contributions = { 1.0f, 0.7f, 0.5f, 0.3f }; // per-employee contribution

            for (int i = 0; i < working; i++)
            {
                int idx = Mathf.Min(i, contributions.Length - 1);
                mult += contributions[idx];
            }
            return mult;
        }

        private int CountWorkingEmployees()
        {
            if (EmployeeManager.Instance == null) return 0;
            int count = 0;
            foreach (var emp in EmployeeManager.Instance.Employees)
            {
                if (emp == null) continue;
                if (emp.CurrentState == EmployeeState.WorkingSeated ||
                    emp.CurrentState == EmployeeState.WorkingStanding)
                    count++;
            }
            return count;
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private void Fail(string reason)
        {
            Debug.LogWarning($"[TechResearch] Cannot start research: {reason}");
            OnResearchFailed?.Invoke(reason);
        }

        /// Returns the number of working employees (for UI display)
        public int GetWorkingEmployeeCount() => CountWorkingEmployees();

        /// <summary>Called by SaveManager when loading a save to restore active research state without deducting money.</summary>
        public void LoadActiveResearch(string activeTechId, float progress)
        {
            ActiveResearch = null;

            if (string.IsNullOrEmpty(activeTechId)) return;
            if (TechnologyDatabase.Instance == null) return;

            var tech = TechnologyDatabase.Instance.GetById(activeTechId);
            if (tech != null && !tech.IsCompleted)
            {
                tech.State = TechState.Researching;
                tech.ResearchProgress = Mathf.Clamp01(progress);
                ActiveResearch = tech;

                Debug.Log($"[TechResearch] Active research restored: {tech.Name} ({tech.ResearchProgress * 100f:F1}%)");
                OnResearchStarted?.Invoke(tech);
            }
        }

        /// Estimate remaining time in game-days
        public float GetEstimatedRemainingDays()
        {
            if (ActiveResearch == null) return 0f;
            float remaining = 1f - ActiveResearch.ResearchProgress;
            float empMult = Mathf.Max(0.1f, CalculateEmployeeMultiplier());
            // base speed: 1 / (days * 144 real-sec) progress/real-sec
            // real-seconds remaining = remaining / (empMult / (days * 144))
            // game-days remaining = real-seconds / 144
            float totalRealSec = ActiveResearch.ResearchTimeDays * 144f;
            float remainingRealSec = remaining * totalRealSec / empMult;
            return remainingRealSec / 144f;
        }
    }
}
