using UnityEngine;
using UnityEngine.InputSystem;

namespace GameDevStudio.Office
{
    /// <summary>
    /// Mouse hover + click on the office grid.
    /// Created programmatically by SceneInitializer — no manual scene setup needed.
    /// </summary>
    public class GridInputHandler : MonoBehaviour
    {
        // Set by SceneInitializer or auto-found in Start
        public OfficeGrid             Grid;
        public UnityEngine.Camera     Cam;

        private const float GroundY = 0f;
        private int _hoverX = -1, _hoverZ = -1;

        private InputAction _posAction;
        private InputAction _clickAction;

        // ─────────────────────────────────────────────────────
        private void Awake()
        {
            _posAction   = new InputAction("GridPos",   InputActionType.Value,  "<Mouse>/position");
            _clickAction = new InputAction("GridClick", InputActionType.Button, "<Mouse>/leftButton");
            _posAction.Enable();
            _clickAction.Enable();

            Debug.Log("[GridInputHandler] Awake — InputActions created & enabled.");
        }

        private void Start()
        {
            if (Grid == null) Grid = FindFirstObjectByType<OfficeGrid>();
            if (Cam  == null) Cam  = UnityEngine.Camera.main;

            Debug.Log($"[GridInputHandler] Start — Grid={(Grid != null ? Grid.name : "NULL")}, " +
                      $"Cam={(Cam != null ? Cam.name : "NULL")}, " +
                      $"posEnabled={_posAction.enabled}, clickEnabled={_clickAction.enabled}");
        }

        private void OnDestroy()
        {
            _posAction?.Dispose();
            _clickAction?.Dispose();
        }

        // ─────────────────────────────────────────────────────
        private void Update()
        {
            if (Grid == null || Cam == null) return;

            // Yield to FurniturePlacer when placement mode is active or when UI overlay is open
            if (FurniturePlacer.IsPlacing || IsUIBlocking())
            {
                if (_hoverX >= 0) { Grid.ClearHighlight(); _hoverX = _hoverZ = -1; }
                return;
            }

            Vector2 pos     = _posAction.ReadValue<Vector2>();
            bool    clicked = _clickAction.WasPressedThisFrame();

            UpdateHover(pos);
            if (clicked) HandleClick(pos);
        }

        private bool IsUIBlocking()
        {
            if (GameDevStudio.UI.RecruitmentUI.Instance != null && GameDevStudio.UI.RecruitmentUI.Instance.IsOpen) return true;
            if (GameDevStudio.UI.ResearchUI.Instance != null && GameDevStudio.UI.ResearchUI.Instance.IsOpen) return true;
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return true;
            return false;
        }

        // ── Hover ─────────────────────────────────────────────
        private void UpdateHover(Vector2 screenPos)
        {
            if (!ScreenToGrid(screenPos, out int x, out int z))
            {
                if (_hoverX >= 0) { Grid.ClearHighlight(); _hoverX = _hoverZ = -1; }
                return;
            }
            if (x == _hoverX && z == _hoverZ) return;
            _hoverX = x; _hoverZ = z;
            Grid.ShowHighlight(x, z);
        }

        // ── Click ─────────────────────────────────────────────
        private void HandleClick(Vector2 screenPos)
        {
            if (!ScreenToGrid(screenPos, out int x, out int z)) return;
            var cell = Grid.Data.GetCell(x, z);
            Debug.Log($"[Grid] Clicked → X: {x}, Z: {z}  |  State: {cell.State}");
        }

        // ── Screen → Grid ─────────────────────────────────────
        private bool ScreenToGrid(Vector2 screenPos, out int x, out int z)
        {
            x = z = -1;
            if (screenPos == Vector2.zero) return false;

            Ray ray   = Cam.ScreenPointToRay(new Vector3(screenPos.x, screenPos.y, 0f));
            var plane = new Plane(Vector3.up, new Vector3(0f, GroundY, 0f));
            if (!plane.Raycast(ray, out float dist)) return false;

            Vector3 wp = ray.GetPoint(dist);
            return Grid.Data.WorldToCell(wp, Grid.Origin, out x, out z);
        }
    }
}
