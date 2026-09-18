using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using GameDevStudio.Economy;
using GameDevStudio.Characters;
using GameDevStudio.AI;
using GameDevStudio.Office;
using GameDevStudio.Flow;

namespace GameDevStudio.Save
{
    /// <summary>
    /// Singleton. Manages up to 3 save slots using JSON files in Application.persistentDataPath.
    /// Call SaveManager.Instance.Save(slot) / .Load(slot) / .GetSlotInfo(slot).
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        public const int MaxSlots = 3;
        private const string SaveFilePrefix = "save_slot_";
        private const string SaveFileExt    = ".json";

        // ── Events ────────────────────────────────────────────────────────────
        public event Action<int> OnSaved;   // slot index
        public event Action<int> OnLoaded;  // slot index

        // ─────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Returns true if a save file exists for the given slot (0-based).</summary>
        public bool HasSave(int slot) => File.Exists(GetPath(slot));

        /// <summary>Returns a header-only snapshot for displaying in the load menu. Returns null if no save.</summary>
        public GameSaveData GetSlotInfo(int slot)
        {
            if (!HasSave(slot)) return null;
            try
            {
                string json = File.ReadAllText(GetPath(slot));
                return JsonUtility.FromJson<GameSaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to read slot {slot}: {e.Message}");
                return null;
            }
        }

        /// <summary>Saves current game state to slot (0-based).</summary>
        public bool Save(int slot)
        {
            try
            {
                GameSaveData data = GatherSaveData();
                data.SaveDate = DateTime.Now.ToString("o");
                string json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(GetPath(slot), json);
                Debug.Log($"[SaveManager] Saved to slot {slot}: {GetPath(slot)}");
                OnSaved?.Invoke(slot);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Save failed: {e.Message}");
                return false;
            }
        }

        /// <summary>Loads game state from slot (0-based) into current systems.</summary>
        public bool Load(int slot)
        {
            if (!HasSave(slot))
            {
                Debug.LogWarning($"[SaveManager] No save in slot {slot}.");
                return false;
            }
            try
            {
                string json = File.ReadAllText(GetPath(slot));
                GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
                ApplySaveData(data);
                Debug.Log($"[SaveManager] Loaded slot {slot}.");
                OnLoaded?.Invoke(slot);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Load failed: {e.Message}");
                return false;
            }
        }

        /// <summary>Deletes the save file for the given slot.</summary>
        public bool DeleteSave(int slot)
        {
            string path = GetPath(slot);
            if (!File.Exists(path)) return false;
            File.Delete(path);
            Debug.Log($"[SaveManager] Deleted save slot {slot}.");
            return true;
        }

        // ── Data Collection ───────────────────────────────────────────────────

        private GameSaveData GatherSaveData()
        {
            var data = new GameSaveData();

            // Company
            if (DifficultyManager.Instance != null)
            {
                data.Company.DifficultyType = (int)DifficultyManager.Instance.Current.Type;
            }
            if (MoneyManager.Instance != null)
            {
                data.Company.Money = MoneyManager.Instance.CurrentMoney;
            }
            if (GameFlowManager.Instance != null)
            {
                data.Company.CompanyName = GameFlowManager.Instance.CompanyName;
            }

            // Time
            if (Core.GameTimeManager.Instance != null)
            {
                var t = Core.GameTimeManager.Instance;
                data.GameTime.Day    = t.CurrentDay;
                data.GameTime.Hour   = t.CurrentHour;
                data.GameTime.Minute = t.CurrentMinute;
                data.GameTime.SpeedMultiplier = t.CurrentSpeedMultiplier;
            }

            // Office
            if (OfficeManager.Instance != null)
            {
                data.Office.CurrentLevelIndex = OfficeManager.Instance.CurrentLevelIndex;
            }

            // Furniture
            if (FurniturePlacer.Instance != null)
            {
                data.Furniture = FurniturePlacer.Instance.GatherSaveData();
            }

            // Employees
            OfficeGrid officeGrid = FindFirstObjectByType<OfficeGrid>();

            if (EmployeeManager.Instance != null)
            {
                foreach (var emp in EmployeeManager.Instance.Employees)
                {
                    if (emp == null || emp.Data == null) continue;
                    var d = emp.Data;

                    int gx = -1;
                    int gz = -1;
                    if (officeGrid != null && officeGrid.Data != null)
                    {
                        officeGrid.Data.WorldToCell(emp.transform.position, officeGrid.Origin, out gx, out gz);
                    }

                    int wsGx = -1;
                    int wsGz = -1;
                    string wsId = "NONE";
                    if (emp.AssignedWorkstation != null && officeGrid != null && officeGrid.Data != null)
                    {
                        if (officeGrid.Data.WorldToCell(emp.AssignedWorkstation.transform.position, officeGrid.Origin, out int wx, out int wz))
                        {
                            wsGx = wx;
                            wsGz = wz;
                            wsId = $"WS_{wx}_{wz}";
                        }
                    }

                    var entry = new EmployeeSaveEntry
                    {
                        EmployeeName      = d.employeeName,
                        RoleString        = d.role,
                        AppearanceStyle   = (int)d.appearanceStyle,
                        Reasoning         = d.reasoningSkill,
                        Engineering       = d.engineeringSkill,
                        Creativity        = d.creativitySkill,
                        Leadership        = d.dataSkill,  // closest proxy
                        SalaryPerDay      = d.salaryPerDay,
                        TraitFlags        = (int)d.traits,
                        MoveSpeed         = d.moveSpeed,
                        GridX             = gx,
                        GridZ             = gz,
                        WorkstationGridX  = wsGx,
                        WorkstationGridZ  = wsGz,
                        WorkstationId     = wsId,
                        EmployeeWorkState = (int)emp.CurrentState
                    };
                    data.Employees.Add(entry);
                }
            }

            // Product Development
            if (Products.ProductDevelopmentManager.Instance != null)
            {
                data.ProductDevelopment = Products.ProductDevelopmentManager.Instance.GatherSaveData();
            }

            // AI Core
            if (AICore.Instance != null)
            {
                var ai = AICore.Instance;
                data.AICore.Quality         = ai.Quality;
                data.AICore.Speed           = ai.Speed;
                data.AICore.Reasoning       = ai.Reasoning;
                data.AICore.Creativity      = ai.Creativity;
                data.AICore.Reliability     = ai.Reliability;
                data.AICore.Learning        = ai.Learning;
                data.AICore.ComputeCapacity = ai.CurrentComputeCapacity;
                data.AICore.EnergyCapacity  = ai.CurrentEnergyCapacity;
            }

            // Technology
            if (TechnologyDatabase.Instance != null)
            {
                foreach (var tech in TechnologyDatabase.Instance.AllTechnologies)
                {
                    data.Technology.TechStates.Add(new TechnologyStateSaveEntry
                    {
                        TechId = tech.Id,
                        State  = (int)tech.State
                    });
                }
            }
            if (TechnologyResearchManager.Instance != null)
            {
                data.Technology.ActiveTechId   = TechnologyResearchManager.Instance.ActiveResearch?.Id;
                data.Technology.ActiveProgress = TechnologyResearchManager.Instance.ActiveProgress;
            }

            data.SaveDate = DateTime.Now.ToString("o");
            return data;
        }

        private void ApplySaveData(GameSaveData data)
        {
            // 1. Lock Input / State
            GameStateManager.Instance?.GoLoadGame();

            // 2. Basic Systems: Company name & Difficulty
            if (GameFlowManager.Instance != null)
                GameFlowManager.Instance.SetCompanyName(data.Company.CompanyName);

            if (DifficultyManager.Instance != null)
                DifficultyManager.Instance.SetDifficulty((DifficultyType)data.Company.DifficultyType);

            // 3. Money
            if (MoneyManager.Instance != null)
                MoneyManager.Instance.SetMoney(data.Company.Money);

            // 4. Time
            if (Core.GameTimeManager.Instance != null)
                Core.GameTimeManager.Instance.LoadState(data.GameTime.Day, data.GameTime.Hour,
                                                        data.GameTime.Minute, data.GameTime.SpeedMultiplier);

            // 5. Office Level (physical floor & grid resize: Office → OfficeGrid)
            if (OfficeManager.Instance != null)
                OfficeManager.Instance.LoadLevelIndex(data.Office.CurrentLevelIndex);

            // 6. Furniture (placed on grid before employees spawn)
            if (FurniturePlacer.Instance != null)
                FurniturePlacer.Instance.LoadFurniture(data.Furniture);

            // 7. Employees & Workstation Assignments
            if (EmployeeManager.Instance != null)
                EmployeeManager.Instance.LoadEmployees(data.Employees);

            // 8. Product Development (restored after employees and workstations are initialized)
            if (Products.ProductDevelopmentManager.Instance != null)
                Products.ProductDevelopmentManager.Instance.LoadState(data.ProductDevelopment);

            // 9. AI Core Stats
            if (AICore.Instance != null)
                AICore.Instance.LoadStats(data.AICore);

            // 10. Completed Technologies
            if (TechnologyDatabase.Instance != null)
                TechnologyDatabase.Instance.LoadStates(data.Technology);

            // 11. Active Research
            if (TechnologyResearchManager.Instance != null)
                TechnologyResearchManager.Instance.LoadActiveResearch(data.Technology.ActiveTechId, data.Technology.ActiveProgress);

            // 12. Gameplay State Resume
            GameStateManager.Instance?.GoGameplay();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static string GetPath(int slot) =>
            Path.Combine(Application.persistentDataPath, $"{SaveFilePrefix}{slot}{SaveFileExt}");
    }
}
