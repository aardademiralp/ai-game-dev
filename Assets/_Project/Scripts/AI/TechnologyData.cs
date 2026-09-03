using System;
using System.Collections.Generic;

namespace GameDevStudio.AI
{
    // ── Enums ─────────────────────────────────────────────────────────────────

    public enum TechCategory
    {
        CoreTechnology,
        Intelligence,
        MachineLearning,
        GenerativeAI,
        Software,
        GameDevelopment,
        Infrastructure,
        AISafety
    }

    public enum TechState
    {
        Locked,       // Prerequisites not met
        Available,    // Can be researched
        Researching,  // Currently active research
        Completed     // Research finished
    }

    // ── Effect applied when a technology is completed ─────────────────────────

    [Serializable]
    public class TechnologyEffect
    {
        public string capabilityId;   // e.g. "TEXT_AI", "IMAGE_AI", "GAME_DEV"
        public AIStatType? statBonus; // Optional stat to improve
        public int statBonusAmount;   // Amount to add to stat
        public int computeCapacityBonus;
        public int energyCapacityBonus;

        public static TechnologyEffect Capability(string id) =>
            new TechnologyEffect { capabilityId = id };

        public static TechnologyEffect StatBoost(AIStatType stat, int amount) =>
            new TechnologyEffect { statBonus = stat, statBonusAmount = amount };

        public static TechnologyEffect ComputeBoost(int amount) =>
            new TechnologyEffect { computeCapacityBonus = amount };
    }

    // ── Core technology definition ─────────────────────────────────────────────

    public class TechnologyData
    {
        // Identity
        public string       Id          { get; }
        public string       Name        { get; }
        public string       Description { get; }
        public TechCategory Category    { get; }

        // Costs & Requirements
        public float ResearchTimeDays   { get; }  // in game-days
        public int   MoneyCost          { get; }
        public int   ComputeRequired    { get; }
        public int   EnergyRequired     { get; }
        public int   MinEmployees       { get; }

        public IReadOnlyList<string>          PrerequisiteIds { get; }
        public IReadOnlyList<TechnologyEffect> Effects        { get; }

        // Runtime State (mutable)
        public TechState State           { get; set; } = TechState.Locked;
        public float     ResearchProgress { get; set; } = 0f; // 0.0 – 1.0

        public bool IsCompleted   => State == TechState.Completed;
        public bool IsResearching => State == TechState.Researching;
        public bool IsAvailable   => State == TechState.Available;
        public bool IsLocked      => State == TechState.Locked;

        public TechnologyData(
            string id, string name, string description,
            TechCategory category,
            float researchTimeDays, int moneyCost,
            int computeRequired, int energyRequired, int minEmployees,
            string[] prerequisiteIds,
            TechnologyEffect[] effects)
        {
            Id               = id;
            Name             = name;
            Description      = description;
            Category         = category;
            ResearchTimeDays = researchTimeDays;
            MoneyCost        = moneyCost;
            ComputeRequired  = computeRequired;
            EnergyRequired   = energyRequired;
            MinEmployees     = minEmployees;
            PrerequisiteIds  = prerequisiteIds  ?? Array.Empty<string>();
            Effects          = effects          ?? Array.Empty<TechnologyEffect>();
        }
    }
}
