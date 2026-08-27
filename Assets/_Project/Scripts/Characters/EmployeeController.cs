using UnityEngine;
using UnityEngine.AI;
using GameDevStudio.Employees;
using GameDevStudio.Office;

namespace GameDevStudio.Characters
{
    public enum EmployeeState
    {
        Idle,
        WalkingToWork,
        WorkingSeated,
        WorkingStanding
    }

    /// <summary>
    /// Drives an employee's NavMesh movement, chair-sitting, state transitions, and walk animation.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class EmployeeController : MonoBehaviour
    {
        // ── Public state ──────────────────────────────────────────
        public EmployeeData   Data               { get; private set; }
        public EmployeeState  CurrentState       { get; private set; } = EmployeeState.Idle;
        public Workstation    AssignedWorkstation { get; private set; }
        public ChairMarker    TargetChair        { get; private set; }
        public GameObject     VisualModel        { get; set; }

        private NavMeshAgent _agent;
        private float        _animTime;

        // ─────────────────────────────────────────────────────────
        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.speed           = 2.5f;
            _agent.stoppingDistance = 0.35f;
            _agent.radius          = 0.25f;
            _agent.height          = 1.4f;
            _agent.angularSpeed    = 240f;
            _agent.acceleration    = 8f;
        }

        public void Initialize(EmployeeData data)
        {
            Data = data;
            if (Data != null && _agent != null)
                _agent.speed = Data.moveSpeed;
        }

        // ─────────────────────────────────────────────────────────
        private void Update()
        {
            _animTime += Time.deltaTime;

            switch (CurrentState)
            {
                case EmployeeState.Idle:
                    EmployeeModelBuilder.TickIdleAnimation(VisualModel, _animTime);
                    TryFindAndGoToWorkstation();
                    break;

                case EmployeeState.WalkingToWork:
                    // Rotate model toward movement direction smoothly
                    if (_agent.velocity.sqrMagnitude > 0.01f)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(_agent.velocity.normalized);
                        transform.rotation = Quaternion.Slerp(
                            transform.rotation, targetRot, Time.deltaTime * 8f);
                    }
                    EmployeeModelBuilder.TickWalkAnimation(
                        VisualModel, _animTime, Data != null ? Data.moveSpeed : 2.5f);
                    CheckArrival();
                    break;

                case EmployeeState.WorkingSeated:
                case EmployeeState.WorkingStanding:
                    // Future: task progress ticking goes here
                    break;
            }
        }

        // ─────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────
        public void AssignWorkstation(Workstation ws)
        {
            if (ws == null || !ws.TryAssign(this)) return;

            AssignedWorkstation = ws;

            // Re-enable NavMeshAgent if it was previously disabled while seated
            if (_agent != null && !_agent.enabled)
                _agent.enabled = true;

            // Find the nearest free chair near this computer
            ChairMarker chair = ChairMarker.FindNearestFreeChair(ws.transform.position, 2.0f);

            if (chair != null && !chair.IsOccupied)
            {
                TargetChair = chair;
                TargetChair.IsOccupied = true;
                TargetChair.WorkDirection =
                    (ws.transform.position - chair.SeatPosition).normalized;
            }
            else
            {
                TargetChair = null;
            }

            CurrentState = EmployeeState.WalkingToWork;

            if (TargetChair != null)
            {
                _agent.stoppingDistance = 0.15f;
                Vector3 dest = TargetChair.SeatPosition;
                if (_agent.isOnNavMesh)
                {
                    _agent.isStopped = false;
                    _agent.SetDestination(dest);
                }
                Debug.Log($"[Employee] WalkingToWork → {Data?.employeeName} heading to Chair (Seated)");
            }
            else
            {
                _agent.stoppingDistance = 0.45f;
                Vector3 dest = AssignedWorkstation.transform.position;
                if (_agent.isOnNavMesh)
                {
                    _agent.isStopped = false;
                    _agent.SetDestination(dest);
                }
                Debug.Log($"[Employee] WalkingToWork → {Data?.employeeName} heading to Computer (Standing)");
            }
        }

        // ─────────────────────────────────────────────────────────
        private void TryFindAndGoToWorkstation()
        {
            if (EmployeeManager.Instance == null) return;
            Workstation freeWs = EmployeeManager.Instance.FindUnassignedWorkstation();
            if (freeWs != null) AssignWorkstation(freeWs);
        }

        private void CheckArrival()
        {
            if (AssignedWorkstation == null) { CurrentState = EmployeeState.Idle; return; }
            if (_agent.enabled && _agent.pathPending) return;
            if (_agent.enabled && _agent.remainingDistance > _agent.stoppingDistance + 0.15f) return;

            // ── Step 1: Stop agent & reset path ──
            if (_agent.enabled)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
            }

            if (TargetChair != null)
            {
                // ── Step 2: Disable NavMeshAgent so NavMeshObstacle carving doesn't clamp position outside ──
                if (_agent.enabled)
                    _agent.enabled = false;

                // ── Step 3: Exact position snap to SeatPoint XZ ──
                Vector3 seatPos = TargetChair.SeatPosition;
                transform.position = new Vector3(seatPos.x, TargetChair.transform.position.y, seatPos.z);

                // ── Step 4: Exact rotation (Computer position - SeatPoint position) ──
                Vector3 toComputer = AssignedWorkstation.transform.position - transform.position;
                toComputer.y = 0f;
                if (toComputer.sqrMagnitude > 0.001f)
                {
                    TargetChair.WorkDirection = toComputer.normalized;
                    transform.rotation = Quaternion.LookRotation(TargetChair.WorkDirection, Vector3.up);
                }

                // ── Step 5 & 6: Stop walking animation & apply sitting pose ──
                EmployeeModelBuilder.SetSittingPose(VisualModel);

                CurrentState = EmployeeState.WorkingSeated;
                Debug.Log($"[Employee] WorkingSeated → {Data?.employeeName} seated precisely on Chair at {transform.position}");
            }
            else
            {
                // ── Standing: face computer, fully reset pose ──
                Vector3 toComp = AssignedWorkstation.transform.position - transform.position;
                toComp.y = 0f;
                if (toComp.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(toComp.normalized, Vector3.up);

                EmployeeModelBuilder.SetStandingPose(VisualModel);

                CurrentState = EmployeeState.WorkingStanding;
                Debug.Log($"[Employee] WorkingStanding → {Data?.employeeName}");
            }
        }
    }
}
