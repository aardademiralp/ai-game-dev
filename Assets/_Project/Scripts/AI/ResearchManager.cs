using UnityEngine;
using System.Collections.Generic;
using GameDevStudio.Characters;
using GameDevStudio.Core;

namespace GameDevStudio.AI
{
    public class ResearchAssignment
    {
        public EmployeeController employee;
        public AIStatType targetStat;
        public float progress; // 0 to 100
    }

    /// <summary>
    /// Manages active AI research tasks assigned to employees working at computer stations.
    /// </summary>
    public class ResearchManager : MonoBehaviour
    {
        public static ResearchManager Instance { get; private set; }

        private List<ResearchAssignment> _assignments = new List<ResearchAssignment>();

        public IReadOnlyList<ResearchAssignment> Assignments => _assignments;

        public event System.Action<ResearchAssignment> OnResearchProgressUpdated;
        public event System.Action<EmployeeController, AIStatType, int> OnResearchCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public ResearchAssignment GetAssignment(EmployeeController emp)
        {
            return _assignments.Find(a => a.employee == emp);
        }

        public void AssignTask(EmployeeController emp, AIStatType stat)
        {
            if (emp == null) return;

            var existing = GetAssignment(emp);
            if (existing != null)
            {
                existing.targetStat = stat;
                existing.progress = 0f;
                Debug.Log($"[AI Research] Reassigned {emp.Data?.employeeName} to Improve {stat}");
            }
            else
            {
                var newAssign = new ResearchAssignment
                {
                    employee = emp,
                    targetStat = stat,
                    progress = 0f
                };
                _assignments.Add(newAssign);
                Debug.Log($"[AI Research] Assigned {emp.Data?.employeeName} to Improve {stat}");
            }
        }

        private void Update()
        {
            if (GameTimeManager.Instance != null && GameTimeManager.Instance.IsPaused) return;

            float timeMult = GameTimeManager.Instance != null ? GameTimeManager.Instance.CurrentSpeedMultiplier : 1f;
            float dt = Time.deltaTime * timeMult;

            for (int i = _assignments.Count - 1; i >= 0; i--)
            {
                var assign = _assignments[i];
                if (assign.employee == null)
                {
                    _assignments.RemoveAt(i);
                    continue;
                }

                // Check if employee is working at a workstation
                bool isWorking = assign.employee.CurrentState == EmployeeState.WorkingSeated ||
                                 assign.employee.CurrentState == EmployeeState.WorkingStanding;

                if (isWorking && assign.employee.Data != null)
                {
                    int skill = assign.employee.Data.GetSkillFor(assign.targetStat);
                    float baseSpeed = 4.0f + (skill * 0.15f);

                    // Trait multipliers
                    if (assign.employee.Data.traits.HasFlag(Employees.EmployeeTrait.FastLearner)) baseSpeed *= 1.25f;
                    if (assign.employee.Data.traits.HasFlag(Employees.EmployeeTrait.HardWorker)) baseSpeed *= 1.20f;
                    if (assign.employee.Data.traits.HasFlag(Employees.EmployeeTrait.Lazy)) baseSpeed *= 0.80f;

                    assign.progress += baseSpeed * dt;
                    OnResearchProgressUpdated?.Invoke(assign);

                    // Note: stat improvements are now handled by TechnologyResearchManager
                    // when a technology is fully researched. This loop only drives progress
                    // tracking for UI display (e.g., per-employee work status).
                    if (assign.progress >= 100f)
                    {
                        assign.progress = 0f;
                        // Reset so the employee keeps "working" — actual outcomes come from TechResearch.
                    }
                }
            }
        }
    }
}
