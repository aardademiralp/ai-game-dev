using UnityEngine;
using GameDevStudio.Characters;

namespace GameDevStudio.Office
{
    /// <summary>
    /// Component attached to Computer furniture items to mark them as usable workstations for employees.
    /// </summary>
    public class Workstation : MonoBehaviour
    {
        public EmployeeController AssignedEmployee { get; private set; }

        public bool IsAssigned => AssignedEmployee != null;

        private void Start()
        {
            if (EmployeeManager.Instance != null)
            {
                EmployeeManager.Instance.RegisterWorkstation(this);
            }
        }

        private void OnDestroy()
        {
            if (EmployeeManager.Instance != null)
            {
                EmployeeManager.Instance.UnregisterWorkstation(this);
            }
        }

        public bool TryAssign(EmployeeController employee)
        {
            if (IsAssigned && AssignedEmployee != employee) return false;
            AssignedEmployee = employee;
            return true;
        }

        public void Unassign()
        {
            AssignedEmployee = null;
        }
    }
}
