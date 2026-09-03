using UnityEngine;
using System.Collections.Generic;
using GameDevStudio.Employees;
using GameDevStudio.Office;

namespace GameDevStudio.Characters
{
    /// <summary>
    /// Central manager for employee lifecycle: hiring, spawning, workstation registry.
    /// Employees can ONLY be hired through the Recruitment UI / RecruitmentManager.
    /// Direct H-key spawning has been completely removed.
    /// </summary>
    public class EmployeeManager : MonoBehaviour
    {
        public static EmployeeManager Instance { get; private set; }

        public List<EmployeeController> Employees   { get; private set; } = new List<EmployeeController>();
        public List<Workstation>        Workstations { get; private set; } = new List<Workstation>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public EmployeeData GenerateCandidate(EmployeeAppearanceStyle style)
            => EmployeeData.GenerateCandidate(style);

        public EmployeeController HireEmployee(EmployeeData data)
        {
            if (data == null) return null;

            Debug.Log($"[Employee] Hired → {data.employeeName} ({data.role})");

            var empGo = new GameObject($"Employee_{Employees.Count + 1}_{data.employeeName}");
            empGo.transform.position = new Vector3(0.5f, 0f, 0.5f); // entrance

            // Physics collider
            var col    = empGo.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.70f, 0f);
            col.height = 1.40f;
            col.radius = 0.25f;

            // Visual
            var visual = EmployeeModelBuilder.CreateEmployeeModel(data.appearanceStyle);
            visual.transform.SetParent(empGo.transform);
            visual.transform.localPosition = Vector3.zero;

            // NavMeshAgent
            var agent          = empGo.AddComponent<UnityEngine.AI.NavMeshAgent>();
            agent.speed        = data.moveSpeed;
            agent.radius       = 0.25f;
            agent.height       = 1.40f;
            agent.angularSpeed = 240f;

            // Controller
            var ctrl         = empGo.AddComponent<EmployeeController>();
            ctrl.VisualModel = visual;
            ctrl.Initialize(data);

            Employees.Add(ctrl);

            // Immediately assign to a free workstation if one exists
            var freeWs = FindUnassignedWorkstation();
            if (freeWs != null) ctrl.AssignWorkstation(freeWs);

            return ctrl;
        }

        public void FireEmployee(EmployeeController employee)
        {
            if (employee == null) return;
            if (employee.AssignedWorkstation != null) employee.AssignedWorkstation.Unassign();
            if (employee.TargetChair        != null) employee.TargetChair.IsOccupied = false;
            Employees.Remove(employee);
            Object.Destroy(employee.gameObject);
            Debug.Log("[Employee] Fired → employee removed.");
        }

        // ── Workstation registry ──────────────────────────────────
        public void RegisterWorkstation(Workstation ws)
        {
            if (ws == null || Workstations.Contains(ws)) return;
            Workstations.Add(ws);

            foreach (var emp in Employees)
            {
                if (emp != null && emp.CurrentState == EmployeeState.Idle)
                {
                    if (ws.TryAssign(emp)) { emp.AssignWorkstation(ws); break; }
                }
            }
        }

        public void UnregisterWorkstation(Workstation ws)
        {
            if (ws != null) Workstations.Remove(ws);
        }

        public Workstation FindUnassignedWorkstation()
        {
            foreach (var ws in Workstations)
                if (ws != null && !ws.IsAssigned) return ws;
            return null;
        }

        /// <summary>Called by SaveManager when loading a save. Destroys current employees and respawns from data.</summary>
        public void LoadEmployees(System.Collections.Generic.List<Save.EmployeeSaveEntry> entries)
        {
            // Destroy existing employees
            for (int i = Employees.Count - 1; i >= 0; i--)
            {
                if (Employees[i] != null) Destroy(Employees[i].gameObject);
            }
            Employees.Clear();

            if (entries == null) return;

            foreach (var entry in entries)
            {
                // Build EmployeeData from saved entry
                var empData = ScriptableObject.CreateInstance<Employees.EmployeeData>();
                empData.employeeName    = entry.EmployeeName;
                empData.role            = entry.RoleString;
                empData.appearanceStyle = (EmployeeAppearanceStyle)entry.AppearanceStyle;
                empData.reasoningSkill  = entry.Reasoning;
                empData.engineeringSkill = entry.Engineering;
                empData.creativitySkill = entry.Creativity;
                empData.dataSkill       = entry.Leadership;
                empData.salaryPerDay    = entry.SalaryPerDay;
                empData.traits          = (Employees.EmployeeTrait)entry.TraitFlags;
                empData.moveSpeed       = entry.MoveSpeed;

                HireEmployee(empData);
            }

            Debug.Log($"[EmployeeManager] Loaded {entries.Count} employees from save.");
        }
    }
}
