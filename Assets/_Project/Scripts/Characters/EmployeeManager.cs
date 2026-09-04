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

        /// <summary>Called by SaveManager when loading a save. Destroys current employees and respawns from data at saved grid coordinates & workstations.</summary>
        public void LoadEmployees(System.Collections.Generic.List<Save.EmployeeSaveEntry> entries)
        {
            // Destroy existing employees
            for (int i = Employees.Count - 1; i >= 0; i--)
            {
                if (Employees[i] != null) Destroy(Employees[i].gameObject);
            }
            Employees.Clear();

            // Clear existing workstation & chair assignments to avoid conflicts
            foreach (var ws in Workstations)
            {
                if (ws != null) ws.Unassign();
            }
            foreach (var chair in ChairMarker.AllChairs)
            {
                if (chair != null) chair.IsOccupied = false;
            }

            if (entries == null) return;

            OfficeGrid officeGrid = FindFirstObjectByType<OfficeGrid>();

            foreach (var entry in entries)
            {
                // Build EmployeeData from saved entry
                var empData = ScriptableObject.CreateInstance<Employees.EmployeeData>();
                empData.employeeName     = entry.EmployeeName;
                empData.role             = entry.RoleString;
                empData.appearanceStyle  = (EmployeeAppearanceStyle)entry.AppearanceStyle;
                empData.reasoningSkill   = entry.Reasoning;
                empData.engineeringSkill = entry.Engineering;
                empData.creativitySkill  = entry.Creativity;
                empData.dataSkill        = entry.Leadership;
                empData.salaryPerDay     = entry.SalaryPerDay;
                empData.traits           = (Employees.EmployeeTrait)entry.TraitFlags;
                empData.moveSpeed        = entry.MoveSpeed;

                // Instantiate Employee GameObject
                var empGo = new GameObject($"Employee_{Employees.Count + 1}_{empData.employeeName}");

                // Physics collider
                var col    = empGo.AddComponent<CapsuleCollider>();
                col.center = new Vector3(0f, 0.70f, 0f);
                col.height = 1.40f;
                col.radius = 0.25f;

                // Visual
                var visual = EmployeeModelBuilder.CreateEmployeeModel(empData.appearanceStyle);
                visual.transform.SetParent(empGo.transform);
                visual.transform.localPosition = Vector3.zero;

                // NavMeshAgent
                var agent          = empGo.AddComponent<UnityEngine.AI.NavMeshAgent>();
                agent.speed        = empData.moveSpeed;
                agent.radius       = 0.25f;
                agent.height       = 1.40f;
                agent.angularSpeed = 240f;

                // Controller
                var ctrl         = empGo.AddComponent<EmployeeController>();
                ctrl.VisualModel = visual;
                ctrl.Initialize(empData);

                Employees.Add(ctrl);

                // 1. Grid Position & Occupancy resolution
                int targetX = entry.GridX;
                int targetZ = entry.GridZ;

                bool positionAssigned = false;
                if (officeGrid != null && officeGrid.Data != null)
                {
                    // Validate saved position
                    if (officeGrid.Data.IsValid(targetX, targetZ))
                    {
                        var cell = officeGrid.Data.GetCell(targetX, targetZ);
                        if (cell != null && cell.State == CellState.Empty)
                        {
                            cell.State = CellState.Occupied;
                            positionAssigned = true;
                        }
                    }

                    // Safe fallback if saved position is invalid, occupied, or missing
                    if (!positionAssigned)
                    {
                        for (int x = 0; x < officeGrid.Data.Width; x++)
                        {
                            for (int z = 0; z < officeGrid.Data.Height; z++)
                            {
                                var cell = officeGrid.Data.GetCell(x, z);
                                if (cell != null && cell.State == CellState.Empty)
                                {
                                    targetX = x;
                                    targetZ = z;
                                    cell.State = CellState.Occupied;
                                    positionAssigned = true;
                                    break;
                                }
                            }
                            if (positionAssigned) break;
                        }
                    }

                    // Apply initial position
                    if (positionAssigned)
                    {
                        Vector3 worldPos = officeGrid.Data.CellToWorld(targetX, targetZ, officeGrid.Origin);
                        empGo.transform.position = worldPos;
                        if (agent.isOnNavMesh) agent.Warp(worldPos);
                    }
                    else
                    {
                        empGo.transform.position = new Vector3(0.5f, 0f, 0.5f);
                    }
                }
                else
                {
                    empGo.transform.position = new Vector3(0.5f, 0f, 0.5f);
                }

                // 2. Workstation & Sitting State Resolution
                Workstation targetWs = null;
                bool wsFound = false;

                if (entry.WorkstationGridX >= 0 && entry.WorkstationGridZ >= 0 && officeGrid != null && officeGrid.Data != null)
                {
                    foreach (var ws in Workstations)
                    {
                        if (ws == null || ws.IsAssigned) continue;
                        if (officeGrid.Data.WorldToCell(ws.transform.position, officeGrid.Origin, out int wx, out int wz))
                        {
                            if (wx == entry.WorkstationGridX && wz == entry.WorkstationGridZ)
                            {
                                targetWs = ws;
                                wsFound = true;
                                break;
                            }
                        }
                    }
                }

                // Fallback: If saved workstation is missing/taken/office shrunk, find any free workstation
                if (targetWs == null)
                {
                    targetWs = FindUnassignedWorkstation();
                    if (targetWs != null) wsFound = true;
                }

                EmployeeState targetState = (EmployeeState)entry.EmployeeWorkState;

                if (targetWs != null)
                {
                    ctrl.AssignWorkstation(targetWs);

                    // If saved state was working (seated/standing), restore sitting/standing pose directly
                    if (targetState == EmployeeState.WorkingSeated || targetState == EmployeeState.WorkingStanding)
                    {
                        ctrl.RestoreWorkState(targetState);
                    }
                }

                // Debug log as requested
                Debug.Log($"[Save/Load] Employee loaded: {empData.employeeName}\n" +
                          $"Saved Grid: {entry.GridX}, {entry.GridZ}\n" +
                          $"Saved Workstation: {entry.WorkstationId} (x:{entry.WorkstationGridX}, z:{entry.WorkstationGridZ})\n" +
                          $"Workstation found: {wsFound}\n" +
                          $"State restored: {ctrl.CurrentState}\n" +
                          $"Final position: {ctrl.transform.position}");
            }

            Debug.Log($"[EmployeeManager] Loaded {entries.Count} employees from save with grid positions & workstations.");
        }
    }
}
