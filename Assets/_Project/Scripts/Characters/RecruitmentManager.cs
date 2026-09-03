using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using GameDevStudio.Employees;
using GameDevStudio.Economy;
using GameDevStudio.Core;
using GameDevStudio.Office;

namespace GameDevStudio.Characters
{
    /// <summary>
    /// Manages candidate pools for recruitment, hiring validation against staff capacity,
    /// and daily employee payroll.
    /// </summary>
    public class RecruitmentManager : MonoBehaviour
    {
        public static RecruitmentManager Instance { get; private set; }

        // Safety ceiling — actual capacity is always enforced by OfficeManager.
        // This value is intentionally high so it never interferes with office-level caps.
        [SerializeField] private int defaultMaxStaffCapacity = 99;

        public int MaxStaffCapacity => defaultMaxStaffCapacity;
        public List<EmployeeData> CandidatePool { get; private set; } = new List<EmployeeData>();

        public event System.Action OnCandidatePoolRefreshed;
        public event System.Action<EmployeeData> OnEmployeeHired;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            RefreshCandidates();
        }

        private void Start()
        {
            RefreshCandidates();

            // Subscribe to daily payroll
            if (GameTimeManager.Instance != null)
            {
                GameTimeManager.Instance.OnDayChanged += ProcessDailyPayroll;
            }
        }

        private void OnDestroy()
        {
            if (GameTimeManager.Instance != null)
            {
                GameTimeManager.Instance.OnDayChanged -= ProcessDailyPayroll;
            }
        }

        public void RefreshCandidates(int count = 3)
        {
            Debug.Log($"[Recruitment TRACE] RefreshCandidates({count}) ENTER. Previous count = {CandidatePool.Count}");
            CandidatePool.Clear();
            for (int i = 0; i < count; i++)
            {
                CandidatePool.Add(EmployeeData.GenerateAICandidate());
            }
            Debug.Log($"[Recruitment TRACE] Candidate pool now contains {CandidatePool.Count} candidates. Invoking OnCandidatePoolRefreshed...");
            OnCandidatePoolRefreshed?.Invoke();
            Debug.Log($"[Recruitment] Refreshed candidate pool with {CandidatePool.Count} AI researchers.");
        }

        public bool CanHire()
        {
            // OfficeManager is the single source of truth for staff capacity.
            if (OfficeManager.Instance != null)
                return OfficeManager.Instance.CanHireMoreEmployees();

            // Fallback if OfficeManager is somehow not present.
            int currentEmp = EmployeeManager.Instance != null ? EmployeeManager.Instance.Employees.Count : 0;
            return currentEmp < MaxStaffCapacity;
        }

        public bool HireCandidate(EmployeeData candidate)
        {
            if (candidate == null) return false;

            if (!CanHire())
            {
                int officeCap = OfficeManager.Instance != null ? OfficeManager.Instance.CurrentCapacity : MaxStaffCapacity;
                Debug.LogWarning($"[Recruitment] Cannot hire {candidate.employeeName} — Office at capacity ({officeCap} employees max).");
                return false;
            }

            if (EmployeeManager.Instance == null) return false;

            var ctrl = EmployeeManager.Instance.HireEmployee(candidate);
            if (ctrl != null)
            {
                CandidatePool.Remove(candidate);
                // Assign a default research task (Improve Reasoning)
                if (GameDevStudio.AI.ResearchManager.Instance != null)
                {
                    GameDevStudio.AI.ResearchManager.Instance.AssignTask(ctrl, GameDevStudio.AI.AIStatType.Reasoning);
                }

                OnEmployeeHired?.Invoke(candidate);
                Debug.Log($"[Recruitment] Successfully hired {candidate.employeeName} (${candidate.salaryPerDay}/day).");
                return true;
            }

            return false;
        }

        private void ProcessDailyPayroll(int day)
        {
            if (EmployeeManager.Instance == null) return;

            int totalSalary = 0;
            foreach (var emp in EmployeeManager.Instance.Employees)
            {
                if (emp != null && emp.Data != null)
                    totalSalary += emp.Data.salaryPerDay;
            }

            if (totalSalary > 0 && MoneyManager.Instance != null)
            {
                MoneyManager.Instance.TrySpendMoney(totalSalary);
                Debug.Log($"[Economy] Daily Payroll for Day {day} → -${totalSalary} for {EmployeeManager.Instance.Employees.Count} employees.");
            }
        }
    }
}
