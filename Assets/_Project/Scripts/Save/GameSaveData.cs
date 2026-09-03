using System;
using System.Collections.Generic;

namespace GameDevStudio.Save
{
    // ── Per-system data containers ────────────────────────────────────────────

    [Serializable]
    public class CompanySaveData
    {
        public string CompanyName   = "My AI Studio";
        public int    DifficultyType = 1;  // cast to Flow.DifficultyType
        public int    Money         = 10000;
    }

    [Serializable]
    public class GameTimeSaveData
    {
        public int   Day    = 1;
        public int   Hour   = 8;
        public int   Minute = 0;
        public float SpeedMultiplier = 1f;
    }

    [Serializable]
    public class OfficeSaveData
    {
        public int CurrentLevelIndex = 0;
    }

    [Serializable]
    public class EmployeeSaveEntry
    {
        public string EmployeeName;
        public string RoleString;
        public int    AppearanceStyle; // cast to EmployeeAppearanceStyle
        public int    Reasoning;
        public int    Engineering;
        public int    Creativity;
        public int    Leadership;
        public int    SalaryPerDay;
        public int    TraitFlags;      // cast to EmployeeTrait flags
        public float  MoveSpeed;
    }

    [Serializable]
    public class AICoreSaveData
    {
        public int Quality          = 20;
        public int Speed            = 15;
        public int Reasoning        = 10;
        public int Creativity       = 12;
        public int Reliability      = 25;
        public int Learning         = 5;
        public int ComputeCapacity  = 10;
        public int EnergyCapacity   = 20;
    }

    [Serializable]
    public class TechnologyStateSaveEntry
    {
        public string TechId;
        public int    State; // cast to AI.TechState
    }

    [Serializable]
    public class TechnologySaveData
    {
        public List<TechnologyStateSaveEntry> TechStates = new List<TechnologyStateSaveEntry>();
        public string ActiveTechId   = null;
        public float  ActiveProgress = 0f;
    }

    // ── Root save container ───────────────────────────────────────────────────

    [Serializable]
    public class GameSaveData
    {
        public int    SaveVersion = 1;          // For future migration
        public string SaveDate    = "";         // ISO-8601 string
        public int    PlaytimeSeconds = 0;      // Future: total playtime

        public CompanySaveData    Company    = new CompanySaveData();
        public GameTimeSaveData   GameTime   = new GameTimeSaveData();
        public OfficeSaveData     Office     = new OfficeSaveData();
        public List<EmployeeSaveEntry> Employees = new List<EmployeeSaveEntry>();
        public AICoreSaveData     AICore     = new AICoreSaveData();
        public TechnologySaveData Technology = new TechnologySaveData();

        // ── Slot metadata (for Load screen display) ───────────────────────────
        public string DisplayCompanyName => Company?.CompanyName ?? "Unknown";
        public string DisplayDifficulty  => ((Flow.DifficultyType)(Company?.DifficultyType ?? 1)).ToString();
        public int    DisplayDay         => GameTime?.Day ?? 1;
        public int    DisplayMoney       => Company?.Money ?? 0;
        public int    DisplayEmployees   => Employees?.Count ?? 0;
    }
}
