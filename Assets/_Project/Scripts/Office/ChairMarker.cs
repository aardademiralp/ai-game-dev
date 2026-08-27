using UnityEngine;
using System.Collections.Generic;

namespace GameDevStudio.Office
{
    /// <summary>
    /// Attached to placed Chair furniture.
    /// Manages an explicit child transform "SeatPoint" located at the exact cushion surface center.
    /// Tracks occupancy and provides direction/position properties for seated employees.
    /// </summary>
    public class ChairMarker : MonoBehaviour
    {
        public static List<ChairMarker> AllChairs { get; private set; } = new List<ChairMarker>();

        public bool IsOccupied { get; set; }

        /// <summary>
        /// Explicit child transform representing the seat cushion center.
        /// Created automatically on Awake/Enable if not present.
        /// </summary>
        public Transform SeatPoint { get; private set; }

        /// <summary>
        /// World position of the seat cushion center.
        /// </summary>
        public Vector3 SeatPosition => SeatPoint != null ? SeatPoint.position : transform.TransformPoint(new Vector3(0f, 0.455f, -0.04f));

        /// <summary>
        /// Direction the seated employee should face (chair -> computer).
        /// Assigned dynamically when assigned to a workstation.
        /// </summary>
        public Vector3 WorkDirection { get; set; } = Vector3.forward;

        private void Awake()
        {
            EnsureSeatPoint();
        }

        private void OnEnable()
        {
            EnsureSeatPoint();
            if (!AllChairs.Contains(this)) AllChairs.Add(this);
        }

        private void OnDisable()
        {
            AllChairs.Remove(this);
            IsOccupied = false;
        }

        /// <summary>
        /// Guarantees that a child GameObject named "SeatPoint" exists at the exact seat cushion center.
        /// Cushion geometry: localPos (0, 0.42, -0.04), cushion top Y = 0.455f.
        /// </summary>
        public void EnsureSeatPoint()
        {
            if (SeatPoint != null) return;

            Transform existing = transform.Find("SeatPoint");
            if (existing != null)
            {
                SeatPoint = existing;
            }
            else
            {
                GameObject spGo = new GameObject("SeatPoint");
                spGo.transform.SetParent(transform, false);
                // Position at seat cushion center top surface (0, 0.455, -0.04)
                spGo.transform.localPosition = new Vector3(0f, 0.455f, -0.04f);
                spGo.transform.localRotation = Quaternion.identity;
                SeatPoint = spGo.transform;
            }
        }

        /// <summary>
        /// Finds the nearest free chair within maxDistance of computerPos.
        /// </summary>
        public static ChairMarker FindNearestFreeChair(Vector3 computerPos, float maxDistance = 2.0f)
        {
            ChairMarker best    = null;
            float       minDist = maxDistance;

            for (int i = 0; i < AllChairs.Count; i++)
            {
                var chair = AllChairs[i];
                if (chair == null || chair.IsOccupied) continue;

                float dist = Vector3.Distance(computerPos, chair.SeatPosition);
                if (dist < minDist)
                {
                    minDist = dist;
                    best    = chair;
                }
            }
            return best;
        }

        public static ChairMarker FindBestChairForComputer(
            Vector3 computerPos, Vector3 computerForward, float maxDistance = 2.0f)
            => FindNearestFreeChair(computerPos, maxDistance);

        /// <summary>
        /// Scene View visual debugging: draws a sphere at SeatPoint and an arrow in WorkDirection.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            EnsureSeatPoint();
            Vector3 pos = SeatPosition;

            Gizmos.color = IsOccupied ? Color.red : Color.green;
            Gizmos.DrawSphere(pos, 0.12f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(pos, WorkDirection.normalized * 0.5f);
        }
    }
}
