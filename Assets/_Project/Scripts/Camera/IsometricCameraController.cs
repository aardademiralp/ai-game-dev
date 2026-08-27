using UnityEngine;
using UnityEngine.InputSystem;

namespace GameDevStudio.Camera
{
    /// <summary>
    /// Controls an isometric-style camera.
    ///   - WASD / Arrow Keys : pan
    ///   - Mouse Scroll Wheel : zoom (distance from focus point)
    ///   - Call Rotate(delta) externally for Q/E yaw rotation (future)
    /// </summary>
    public class IsometricCameraController : MonoBehaviour
    {
        // ── Pan ──────────────────────────────────────────────
        [Header("Pan")]
        [SerializeField] private float panSpeed = 8f;

        // ── Zoom ─────────────────────────────────────────────
        [Header("Zoom")]
        [SerializeField] private float zoomSpeed    = 40f;
        [SerializeField] private float minDistance  = 5f;
        [SerializeField] private float maxDistance  = 30f;

        // ── Isometric Setup ───────────────────────────────────
        [Header("Isometric Setup")]
        [SerializeField] private float pitchAngle      = 45f;   // X-axis tilt
        [SerializeField] private float initialYaw      = 45f;   // Y-axis starting angle
        [SerializeField] private float initialDistance = 15f;   // Distance from focus point

        // ── Runtime State ─────────────────────────────────────
        private Vector3 _focusPoint;
        private float   _currentYaw;
        private float   _currentDistance;

        // ─────────────────────────────────────────────────────
        private void Start()
        {
            _focusPoint      = Vector3.zero;
            _currentYaw      = initialYaw;
            _currentDistance = initialDistance;

            ApplyTransform();
        }

        private void LateUpdate()
        {
            HandlePan();
            HandleZoom();
            ApplyTransform();
        }

        // ─────────────────────────────────────────────────────
        private void HandlePan()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            float h = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f)
                    - (kb.aKey.isPressed || kb.leftArrowKey.isPressed  ? 1f : 0f);

            float v = (kb.wKey.isPressed || kb.upArrowKey.isPressed    ? 1f : 0f)
                    - (kb.sKey.isPressed || kb.downArrowKey.isPressed   ? 1f : 0f);

            if (h == 0f && v == 0f) return;

            // Move relative to camera's current yaw so WASD always feel natural
            Quaternion yaw = Quaternion.Euler(0f, _currentYaw, 0f);
            Vector3 dir = yaw * new Vector3(h, 0f, v).normalized;
            _focusPoint += dir * (panSpeed * Time.deltaTime);
        }

        private void HandleZoom()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            float scroll = mouse.scroll.ReadValue().y;
            if (scroll == 0f) return;

            // scroll.y is in pixels; normalise to a small delta
            _currentDistance -= scroll * zoomSpeed * Time.deltaTime * 0.01f;
            _currentDistance  = Mathf.Clamp(_currentDistance, minDistance, maxDistance);
        }

        /// <summary>
        /// Rotates camera yaw around the focus point.
        /// Call with positive delta to rotate right, negative to rotate left.
        /// Ready for Q/E key support – wire up from outside or add here later.
        /// </summary>
        public void Rotate(float deltaDegrees) => _currentYaw += deltaDegrees;

        /// <summary>
        /// Instantly moves the camera focus point (e.g. to centre on spawned objects).
        /// </summary>
        public void SetFocusPoint(Vector3 worldPoint) => _focusPoint = worldPoint;

        public Vector3 FocusPoint => _focusPoint;

        // ─────────────────────────────────────────────────────
        /// Recalculates camera position & rotation every frame.
        private void ApplyTransform()
        {
            Quaternion rotation = Quaternion.Euler(pitchAngle, _currentYaw, 0f);
            Vector3    offset   = rotation * (Vector3.back * _currentDistance);

            transform.SetPositionAndRotation(_focusPoint + offset, rotation);
        }
    }
}
