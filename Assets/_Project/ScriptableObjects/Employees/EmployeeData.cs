using UnityEngine;
using System.Collections.Generic;
using GameDevStudio.Characters;

namespace GameDevStudio.Employees
{
    // ─────────────────────────────────────────────────────────────
    // Trait system — bitmask-friendly enum for future expansion
    // ─────────────────────────────────────────────────────────────
    [System.Flags]
    public enum EmployeeTrait
    {
        None        = 0,
        FastLearner = 1 << 0,
        HardWorker  = 1 << 1,
        Lazy        = 1 << 2,
        Creative    = 1 << 3,
        Experienced = 1 << 4,
        TeamPlayer  = 1 << 5,
    }

    /// <summary>
    /// ScriptableObject defining employee identity, stats, skills and traits.
    /// Extended in Aşama 5.2 with skill values, motivation, and trait flags.
    /// </summary>
    [CreateAssetMenu(fileName = "EmployeeData_", menuName = "GameDevStudio/Employee Data")]
    public class EmployeeData : ScriptableObject
    {
        [Header("Identity")]
        public string employeeId   = "emp_01";
        public string employeeName = "Junior Developer";
        public string role         = "Developer";
        public EmployeeAppearanceStyle appearanceStyle = EmployeeAppearanceStyle.JuniorDev;

        [Header("Economics & Work")]
        public int   salaryPerDay = 100;
        public float workSpeed    = 1.0f;
        public float moveSpeed    = 2.5f;

        [Header("AI Research Skills (0–100)")]
        [Range(0, 100)] public int researchSkill    = 50;
        [Range(0, 100)] public int engineeringSkill = 50;
        [Range(0, 100)] public int reasoningSkill   = 50;
        [Range(0, 100)] public int dataSkill        = 50;
        [Range(0, 100)] public int safetySkill      = 50;
        [Range(0, 100)] public int creativitySkill  = 50;

        [Header("General Stats (0–100)")]
        [Range(0, 100)] public int experience  = 20;
        [Range(0, 100)] public int motivation  = 80;

        [Header("Traits")]
        public EmployeeTrait traits = EmployeeTrait.None;

        public int GetSkillFor(GameDevStudio.AI.AIStatType statType)
        {
            switch (statType)
            {
                case GameDevStudio.AI.AIStatType.Quality:     return Mathf.Max(researchSkill, dataSkill);
                case GameDevStudio.AI.AIStatType.Speed:       return Mathf.Max(engineeringSkill, researchSkill);
                case GameDevStudio.AI.AIStatType.Reasoning:   return reasoningSkill;
                case GameDevStudio.AI.AIStatType.Creativity:  return creativitySkill;
                case GameDevStudio.AI.AIStatType.Reliability: return safetySkill;
                default: return 50;
            }
        }

        // ── Name Pool ────────────────────────────────────────────
        private static readonly string[] _namePool =
        {
            "Alex Morgan", "Sam Altman", "Mia Carter", "Jordan Lee", "Taylor Swift",
            "Casey Vance", "Jamie Chen", "Chris Bishop", "Riley Vance", "Drew Houston",
            "Blake Ross", "Quinn Fabray", "Avery Brooks", "Skyler Page", "Parker Wright"
        };

        private static int _nameIndex = 0;

        private static string NextName()
        {
            string name = _namePool[_nameIndex % _namePool.Length];
            _nameIndex++;
            return name;
        }

        public static EmployeeData GenerateAICandidate(string forceRole = null)
        {
            var d = ScriptableObject.CreateInstance<EmployeeData>();
            string[] roles = new[] { "AI Researcher", "AI Engineer", "AI Data Scientist", "AI Safety Researcher", "AI Systems Engineer" };
            d.role = string.IsNullOrEmpty(forceRole) ? roles[Random.Range(0, roles.Length)] : forceRole;
            d.employeeName = NextName();
            d.employeeId   = $"emp_{Random.Range(1000, 9999)}";

            d.appearanceStyle = (EmployeeAppearanceStyle)Random.Range(0, 5);

            switch (d.role)
            {
                case "AI Researcher":
                    d.researchSkill    = Random.Range(65, 95);
                    d.reasoningSkill   = Random.Range(60, 90);
                    d.engineeringSkill = Random.Range(40, 75);
                    d.creativitySkill  = Random.Range(50, 85);
                    d.safetySkill      = Random.Range(40, 70);
                    d.dataSkill        = Random.Range(50, 80);
                    d.salaryPerDay     = Random.Range(180, 280);
                    d.traits           = EmployeeTrait.FastLearner;
                    break;

                case "AI Engineer":
                    d.researchSkill    = Random.Range(45, 75);
                    d.reasoningSkill   = Random.Range(50, 80);
                    d.engineeringSkill = Random.Range(70, 98);
                    d.creativitySkill  = Random.Range(40, 70);
                    d.safetySkill      = Random.Range(50, 80);
                    d.dataSkill        = Random.Range(50, 75);
                    d.salaryPerDay     = Random.Range(160, 250);
                    d.traits           = EmployeeTrait.HardWorker;
                    break;

                case "AI Data Scientist":
                    d.researchSkill    = Random.Range(60, 85);
                    d.reasoningSkill   = Random.Range(55, 80);
                    d.engineeringSkill = Random.Range(50, 75);
                    d.creativitySkill  = Random.Range(45, 75);
                    d.safetySkill      = Random.Range(45, 70);
                    d.dataSkill        = Random.Range(75, 98);
                    d.salaryPerDay     = Random.Range(170, 260);
                    d.traits           = EmployeeTrait.Experienced;
                    break;

                case "AI Safety Researcher":
                    d.researchSkill    = Random.Range(60, 90);
                    d.reasoningSkill   = Random.Range(65, 92);
                    d.engineeringSkill = Random.Range(40, 70);
                    d.creativitySkill  = Random.Range(40, 70);
                    d.safetySkill      = Random.Range(75, 98);
                    d.dataSkill        = Random.Range(50, 75);
                    d.salaryPerDay     = Random.Range(190, 290);
                    d.traits           = EmployeeTrait.TeamPlayer;
                    break;

                default: // AI Systems Engineer
                    d.researchSkill    = Random.Range(40, 70);
                    d.reasoningSkill   = Random.Range(50, 80);
                    d.engineeringSkill = Random.Range(75, 96);
                    d.creativitySkill  = Random.Range(40, 75);
                    d.safetySkill      = Random.Range(60, 85);
                    d.dataSkill        = Random.Range(60, 85);
                    d.salaryPerDay     = Random.Range(200, 300);
                    d.traits           = EmployeeTrait.HardWorker | EmployeeTrait.Experienced;
                    break;
            }

            d.experience = Random.Range(20, 85);
            d.motivation = Random.Range(70, 100);
            d.moveSpeed  = 2.5f;

            return d;
        }

        // ── Role templates ────────────────────────────────────────
        public static EmployeeData CreateDefaultJuniorDev(
            EmployeeAppearanceStyle style = EmployeeAppearanceStyle.JuniorDev)
        {
            return GenerateAICandidate();
        }

        public static EmployeeData GenerateCandidate(EmployeeAppearanceStyle style)
        {
            return GenerateAICandidate();
        }
    }
}
