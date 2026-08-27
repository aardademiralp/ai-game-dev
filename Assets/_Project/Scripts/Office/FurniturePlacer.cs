using UnityEngine;
using UnityEngine.InputSystem;

namespace GameDevStudio.Office
{
    /// <summary>
    /// Handles furniture placement on the office grid.
    /// Created by SceneInitializer alongside OfficeGrid.
    ///
    /// Keys:
    ///   1 / 2 / 3  → Select Desk / Chair / Computer
    ///   R          → Rotate 90°
    ///   Escape / RMB → Cancel placement
    ///   LMB        → Place furniture (when in placement mode)
    /// </summary>
    public class FurniturePlacer : MonoBehaviour
    {
        // ── Static flag so GridInputHandler can yield during placement ────
        public static bool IsPlacing { get; private set; }

        // ── References ────────────────────────────────────────────────────
        private OfficeGrid             _grid;
        private UnityEngine.Camera     _cam;

        // ── State ─────────────────────────────────────────────────────────
        private FurnitureData[] _catalog;
        private FurnitureData   _selected;
        private int             _rotation;          // 0-3 (×90°)

        // ── Preview ───────────────────────────────────────────────────────
        private GameObject  _previewGo;
        private Renderer[]  _previewRenderers;
        private Material[]  _previewMaterials;

        private static readonly Color ColorValid   = new Color(0.1f, 0.9f, 0.1f, 0.7f);
        private static readonly Color ColorInvalid = new Color(0.9f, 0.1f, 0.1f, 0.7f);

        // ── Input ─────────────────────────────────────────────────────────
        private InputAction _mousePos, _lmb, _rmb;
        private InputAction _key1, _key2, _key3, _keyR, _keyEsc, _keyB;

        // ── Ground plane (same Y=0 as GridInputHandler) ───────────────────
        private const float GroundY = 0f;

        // ─────────────────────────────────────────────────────────────────
        private void Awake()
        {
            _catalog = BuildDefaultCatalog();

            _mousePos = new InputAction("FP_Pos",  InputActionType.Value,  "<Mouse>/position");
            _lmb      = new InputAction("FP_LMB",  InputActionType.Button, "<Mouse>/leftButton");
            _rmb      = new InputAction("FP_RMB",  InputActionType.Button, "<Mouse>/rightButton");
            _key1     = new InputAction("FP_Key1", InputActionType.Button, "<Keyboard>/1");
            _key2     = new InputAction("FP_Key2", InputActionType.Button, "<Keyboard>/2");
            _key3     = new InputAction("FP_Key3", InputActionType.Button, "<Keyboard>/3");
            _keyR     = new InputAction("FP_KeyR", InputActionType.Button, "<Keyboard>/r");
            _keyEsc   = new InputAction("FP_Esc",  InputActionType.Button, "<Keyboard>/escape");
            _keyB     = new InputAction("FP_KeyB", InputActionType.Button, "<Keyboard>/b");

            _mousePos.Enable(); _lmb.Enable();  _rmb.Enable();
            _key1.Enable();     _key2.Enable(); _key3.Enable();
            _keyR.Enable();     _keyEsc.Enable(); _keyB.Enable();
        }

        private void Start()
        {
            _grid = FindFirstObjectByType<OfficeGrid>();
            _cam  = UnityEngine.Camera.main;
        }

        private void OnDestroy()
        {
            _mousePos?.Dispose(); _lmb?.Dispose(); _rmb?.Dispose();
            _key1?.Dispose();     _key2?.Dispose(); _key3?.Dispose();
            _keyR?.Dispose();     _keyEsc?.Dispose(); _keyB?.Dispose();

            if (_previewGo != null) Destroy(_previewGo);
            IsPlacing = false;
        }

        // ── Update ────────────────────────────────────────────────────────
        private void Update()
        {
            HandleSelectionKeys();

            if (!IsPlacing) return;

            if (_keyEsc.WasPressedThisFrame() || _rmb.WasPressedThisFrame())
            {
                CancelPlacement();
                return;
            }

            if (_keyR.WasPressedThisFrame()) Rotate();

            Vector2 screen = _mousePos.ReadValue<Vector2>();
            UpdatePreview(screen);

            if (_lmb.WasPressedThisFrame()) TryPlace(screen);
        }

        // ── Selection ─────────────────────────────────────────────────────
        private void HandleSelectionKeys()
        {
            if (_keyB != null && _keyB.WasPressedThisFrame())
            {
                if (IsPlacing) CancelPlacement();
                else BeginPlacement(0);
                return;
            }

            bool shiftPressed = Keyboard.current != null && Keyboard.current.shiftKey.isPressed;
            if (IsPlacing || shiftPressed)
            {
                if (_key1.WasPressedThisFrame()) BeginPlacement(0);
                else if (_key2.WasPressedThisFrame()) BeginPlacement(1);
                else if (_key3.WasPressedThisFrame()) BeginPlacement(2);
            }
        }

        private void BeginPlacement(int catalogIndex)
        {
            if (_grid == null || _cam == null) return;
            
            FurnitureData newlySelected = _catalog[catalogIndex];
            if (_selected != newlySelected && _previewGo != null)
            {
                Destroy(_previewGo);
                _previewGo = null;
            }

            _selected = newlySelected;
            _rotation = 0;
            IsPlacing = true;

            if (_previewGo == null) CreatePreviewObject();
            _previewGo.SetActive(true);

            Debug.Log($"[FurniturePlacer] Selected: {_selected.furnitureName} " +
                      $"({_selected.sizeX}x{_selected.sizeZ}) — Price: ${_selected.price}");
        }

        private void CancelPlacement()
        {
            IsPlacing = false;
            _selected = null;
            if (_previewGo != null) _previewGo.SetActive(false);
            _grid?.ClearHighlight();
            Debug.Log("[FurniturePlacer] Placement cancelled.");
        }

        private void Rotate()
        {
            _rotation = (_rotation + 1) % 4;
            Debug.Log($"[FurniturePlacer] Rotation: {_rotation * 90}°");
        }

        // ── Preview ───────────────────────────────────────────────────────
        private void CreatePreviewObject()
        {
            if (_previewGo != null) Destroy(_previewGo);

            if (_selected != null && _selected.prefab != null)
            {
                _previewGo = Instantiate(_selected.prefab);
            }
            else if (_selected != null)
            {
                _previewGo = FurnitureModelBuilder.CreateModel(_selected.furnitureType);
            }
            else
            {
                _previewGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            }

            _previewGo.name = "FurniturePreview";
            FurnitureModelBuilder.RemoveColliders(_previewGo);

            _previewRenderers = _previewGo.GetComponentsInChildren<Renderer>();
            _previewMaterials = new Material[_previewRenderers.Length];
            for (int i = 0; i < _previewRenderers.Length; i++)
            {
                _previewMaterials[i] = new Material(_previewRenderers[i].sharedMaterial);
                _previewRenderers[i].material = _previewMaterials[i];
            }
        }

        private void UpdatePreview(Vector2 screenPos)
        {
            if (_previewGo == null) return;

            if (!ScreenToGrid(screenPos, out int gx, out int gz))
            {
                _previewGo.SetActive(false);
                return;
            }

            GetFootprint(out int fw, out int fh);
            bool valid = CanPlace(gx, gz, fw, fh);

            Vector3 pos = new Vector3(
                _grid.Origin.x + gx + fw * 0.5f,
                _grid.Origin.y,
                _grid.Origin.z + gz + fh * 0.5f);

            _previewGo.transform.position = pos;
            _previewGo.transform.rotation = Quaternion.Euler(0f, _rotation * 90f, 0f);
            _previewGo.SetActive(true);

            ApplyColor(valid ? ColorValid : ColorInvalid);
        }

        private void ApplyColor(Color c)
        {
            if (_previewMaterials == null) return;
            for (int i = 0; i < _previewMaterials.Length; i++)
            {
                Material mat = _previewMaterials[i];
                if (mat == null) continue;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                if (mat.HasProperty("_Color"))     mat.SetColor("_Color",     c);
            }
        }

        // ── Placement ─────────────────────────────────────────────────────
        private void TryPlace(Vector2 screenPos)
        {
            if (!ScreenToGrid(screenPos, out int gx, out int gz)) return;

            GetFootprint(out int fw, out int fh);
            if (!CanPlace(gx, gz, fw, fh))
            {
                Debug.Log("[FurniturePlacer] Cannot place — cells occupied, out of bounds, or insufficient funds.");
                return;
            }

            // Deduct funds via MoneyManager
            if (GameDevStudio.Economy.MoneyManager.Instance != null)
            {
                if (!GameDevStudio.Economy.MoneyManager.Instance.TrySpendMoney(_selected.price))
                {
                    Debug.Log($"[FurniturePlacer] Cannot place — insufficient funds for {_selected.furnitureName} (${_selected.price}).");
                    return;
                }
            }

            // Mark cells occupied
            for (int dx = 0; dx < fw; dx++)
            for (int dz = 0; dz < fh; dz++)
                _grid.Data.GetCell(gx + dx, gz + dz).State = CellState.Occupied;

            // Spawn permanent visual
            SpawnFurnitureVisual(gx, gz, fw, fh);

            Debug.Log($"[FurniturePlacer] Placed '{_selected.furnitureName}' at grid ({gx},{gz}).");
        }

        private void SpawnFurnitureVisual(int gx, int gz, int fw, int fh)
        {
            GameObject go;
            if (_selected.prefab != null)
            {
                go = Instantiate(_selected.prefab);
            }
            else
            {
                go = FurnitureModelBuilder.CreateModel(_selected.furnitureType);
            }

            go.name = _selected.furnitureName;

            go.transform.position = new Vector3(
                _grid.Origin.x + gx + fw * 0.5f,
                _grid.Origin.y,
                _grid.Origin.z + gz + fh * 0.5f);

            go.transform.rotation = Quaternion.Euler(0f, _rotation * 90f, 0f);

            FurnitureModelBuilder.RemoveColliders(go);
        }

        // ── Helpers ───────────────────────────────────────────────────────
        /// Returns footprint dimensions after applying current rotation.
        private void GetFootprint(out int fw, out int fh)
        {
            bool swapped = _rotation % 2 != 0;
            fw = swapped ? _selected.sizeZ : _selected.sizeX;
            fh = swapped ? _selected.sizeX : _selected.sizeZ;
        }

        private bool CanPlace(int gx, int gz, int fw, int fh)
        {
            if (_selected != null && GameDevStudio.Economy.MoneyManager.Instance != null)
            {
                if (!GameDevStudio.Economy.MoneyManager.Instance.HasEnoughMoney(_selected.price)) return false;
            }

            for (int dx = 0; dx < fw; dx++)
            for (int dz = 0; dz < fh; dz++)
            {
                var cell = _grid.Data.GetCell(gx + dx, gz + dz);
                if (cell == null || cell.State != CellState.Empty) return false;
            }
            return true;
        }

        private bool ScreenToGrid(Vector2 screenPos, out int x, out int z)
        {
            x = z = -1;
            if (screenPos == Vector2.zero || _cam == null) return false;

            Ray ray   = _cam.ScreenPointToRay(new Vector3(screenPos.x, screenPos.y, 0f));
            var plane = new Plane(Vector3.up, new Vector3(0f, GroundY, 0f));
            if (!plane.Raycast(ray, out float dist)) return false;

            return _grid.Data.WorldToCell(ray.GetPoint(dist), _grid.Origin, out x, out z);
        }

        // ── Default Catalog ───────────────────────────────────────────────
        private static FurnitureData[] BuildDefaultCatalog()
        {
            return new[]
            {
                CreateItem("Desk",     FurnitureType.Desk,     2, 1, 500,  new Color(0.6f, 0.4f, 0.2f), 0.7f),
                CreateItem("Chair",    FurnitureType.Chair,    1, 1, 150,  new Color(0.2f, 0.4f, 0.8f), 0.8f),
                CreateItem("Computer", FurnitureType.Computer, 1, 1, 1000, new Color(0.1f, 0.1f, 0.1f), 0.5f),
            };
        }

        private static FurnitureData CreateItem(string name, FurnitureType type,
                                                int sx, int sz, int price,
                                                Color color, float height)
        {
            var d = ScriptableObject.CreateInstance<FurnitureData>();
            d.furnitureName  = name;
            d.furnitureType  = type;
            d.sizeX          = sx;
            d.sizeZ          = sz;
            d.price          = price;
            d.color          = color;
            d.visualHeight   = height;
            return d;
        }
    }
}
