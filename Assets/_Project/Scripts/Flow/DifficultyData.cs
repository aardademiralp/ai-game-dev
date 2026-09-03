using System;

namespace GameDevStudio.Flow
{
    public enum DifficultyType
    {
        SizBizeBirakin  = 0,   // Easy
        BizHallederiz   = 1,   // Normal
        UykuHaram       = 2,   // Hard
        YatirimciyiArama = 3   // Extreme
    }

    /// <summary>
    /// All gameplay multipliers that difficulty provides.
    /// Future systems query these — nothing is hard-coded in individual managers.
    /// </summary>
    [Serializable]
    public class DifficultyData
    {
        public DifficultyType Type;

        // Economy
        public int   StartingMoney;
        public float OperatingCostMultiplier;
        public float TaxMultiplier;

        // Research
        public float ResearchSpeedMultiplier;

        // Employees
        public float EmployeeEfficiencyMultiplier;

        // Products (future use)
        public float ProductDevelopmentSpeedMultiplier;
        public float ReputationPenaltyMultiplier;

        // ── Preset constructors ───────────────────────────────────────────────
        public static DifficultyData ForType(DifficultyType type)
        {
            switch (type)
            {
                case DifficultyType.SizBizeBirakin:
                    return new DifficultyData
                    {
                        Type = type,
                        StartingMoney                       = 25000,
                        ResearchSpeedMultiplier             = 1.4f,
                        EmployeeEfficiencyMultiplier        = 1.2f,
                        OperatingCostMultiplier             = 0.7f,
                        TaxMultiplier                       = 0.5f,
                        ProductDevelopmentSpeedMultiplier   = 1.3f,
                        ReputationPenaltyMultiplier         = 0.5f
                    };

                case DifficultyType.BizHallederiz:
                    return new DifficultyData
                    {
                        Type = type,
                        StartingMoney                       = 10000,
                        ResearchSpeedMultiplier             = 1.0f,
                        EmployeeEfficiencyMultiplier        = 1.0f,
                        OperatingCostMultiplier             = 1.0f,
                        TaxMultiplier                       = 1.0f,
                        ProductDevelopmentSpeedMultiplier   = 1.0f,
                        ReputationPenaltyMultiplier         = 1.0f
                    };

                case DifficultyType.UykuHaram:
                    return new DifficultyData
                    {
                        Type = type,
                        StartingMoney                       = 5000,
                        ResearchSpeedMultiplier             = 0.75f,
                        EmployeeEfficiencyMultiplier        = 0.85f,
                        OperatingCostMultiplier             = 1.4f,
                        TaxMultiplier                       = 1.6f,
                        ProductDevelopmentSpeedMultiplier   = 0.8f,
                        ReputationPenaltyMultiplier         = 1.5f
                    };

                case DifficultyType.YatirimciyiArama:
                    return new DifficultyData
                    {
                        Type = type,
                        StartingMoney                       = 2000,
                        ResearchSpeedMultiplier             = 0.55f,
                        EmployeeEfficiencyMultiplier        = 0.7f,
                        OperatingCostMultiplier             = 1.9f,
                        TaxMultiplier                       = 2.2f,
                        ProductDevelopmentSpeedMultiplier   = 0.6f,
                        ReputationPenaltyMultiplier         = 2.0f
                    };

                default:
                    return ForType(DifficultyType.BizHallederiz);
            }
        }
    }
}
