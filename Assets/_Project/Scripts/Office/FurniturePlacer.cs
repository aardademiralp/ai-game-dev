using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;
using GameDevStudio.Save;

namespace GameDevStudio.Office
{
    /// <summary>
    /// Handles furniture placement on the office grid and manages furniture save/load persistence.
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
        public static FurniturePlacer Instance { get; private set; }

        // ── Static flag so GridInputHandler can yield during placement ────
        public static bool IsPlacing { get; private set; }

        // ── References ────────────────────────────────────────────────────
        private OfficeGrid             _grid;
        private UnityEngine.Camera     _cam;

        // ── State ─────────────────────────────────────────────────────────
        private FurnitureData[] _catalog;
        private FurnitureData   _selected;
        private int             _rotation;          // 0-3 (×90°)

        private readonly List<PlacedFurniture> _placedFurnitureList = new List<PlacedFurniture>();
        public IReadOnlyList<PlacedFurniture> PlacedFurnitureList => _placedFurnitureList;

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
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

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
            if (Instance == this) Instance = null;

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

            // Spawn permanent visual and attach persistence tracking
            SpawnFurnitureVisual(_selected, gx, gz, _rotation, 0);

            Debug.Log($"[FurniturePlacer] Placed '{_selected.furnitureName}' at grid ({gx},{gz}).");
        }

        /// <summary>
        /// Instantiates furniture visual mesh/prefab, configures NavMeshObstacle, Workstation/ChairMarker,
        /// and attaches PlacedFurniture component for runtime persistence.
        /// </summary>
        public GameObject SpawnFurnitureVisual(FurnitureData data, int gx, int gz, int rotation, int variantIndex = 0)
        {
            if (_grid == null) _grid = FindFirstObjectByType<OfficeGrid>();

            bool swapped = rotation % 2 != 0;
            int fw = swapped ? data.sizeZ : data.sizeX;
            int fh = swapped ? data.sizeX : data.sizeZ;

            GameObject go;
            if (data.prefab != null)
            {
                go = Instantiate(data.prefab);
            }
            else
            {
                go = FurnitureModelBuilder.CreateModel(data.furnitureType);
            }

            go.name = data.furnitureName;

            Vector3 origin = _grid != null ? _grid.Origin : Vector3.zero;
            go.transform.position = new Vector3(
                origin.x + gx + fw * 0.5f,
                origin.y,
                origin.z + gz + fh * 0.5f);

            go.transform.rotation = Quaternion.Euler(0f, rotation * 90f, 0f);

            FurnitureModelBuilder.RemoveColliders(go);

            // Add NavMeshObstacle so NavMeshAgents dynamically pathfind around placed furniture
            var obstacle = go.AddComponent<UnityEngine.AI.NavMeshObstacle>();
            obstacle.carving = true;
            obstacle.size = new Vector3(fw * 0.85f, 1f, fh * 0.85f);

            // Attach Workstation if Computer, or ChairMarker if Chair
            if (data.furnitureType == FurnitureType.Computer)
            {
                go.AddComponent<Workstation>();
            }
            else if (data.furnitureType == FurnitureType.Chair)
            {
                go.AddComponent<ChairMarker>();
            }

            // Attach PlacedFurniture persistence component
            var pf = go.AddComponent<PlacedFurniture>();
            pf.FurnitureId  = data.Id;
            pf.GridX        = gx;
            pf.GridZ        = gz;
            pf.Rotation     = rotation;
            pf.VariantIndex = variantIndex;
            pf.InstanceId   = $"FURN_{gx}_{gz}";
            pf.Data         = data;

            _placedFurnitureList.Add(pf);
            return go;
        }

        // ── Catalog Lookup ────────────────────────────────────────────────
        public FurnitureData GetFurnitureDataById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            if (_catalog != null)
            {
                foreach (var item in _catalog)
                {
                    if (item == null) continue;
                    if (string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(item.furnitureName, id, StringComparison.OrdinalIgnoreCase))
                    {
                        return item;
                    }
                }
            }
            return null;
        }

        // ── Save / Load System ───────────────────────────────────────────

        /// <summary>
        /// Gathers save data entries for all placed furniture on the office grid.
        /// </summary>
        public List<FurnitureSaveEntry> GatherSaveData()
        {
            _placedFurnitureList.RemoveAll(pf => pf == null);

            var list = new List<FurnitureSaveEntry>();
            foreach (var pf in _placedFurnitureList)
            {
                if (pf == null) continue;
                list.Add(new FurnitureSaveEntry
                {
                    FurnitureId  = pf.FurnitureId,
                    GridX        = pf.GridX,
                    GridZ        = pf.GridZ,
                    Rotation     = pf.Rotation,
                    VariantIndex = pf.VariantIndex,
                    InstanceId   = pf.InstanceId
                });
            }
            return list;
        }

        /// <summary>
        /// Clears existing furniture and recreates furniture from loaded save entries.
        /// </summary>
        public void LoadFurniture(List<FurnitureSaveEntry> entries)
        {
            ClearAllPlacedFurniture();

            if (entries == null || entries.Count == 0)
            {
                Debug.Log("[FurniturePlacer] No furniture entries to load.");
                return;
            }

            _grid = FindFirstObjectByType<OfficeGrid>();
            if (_grid == null || _grid.Data == null)
            {
                Debug.LogError("[FurniturePlacer] OfficeGrid missing during furniture load!");
                return;
            }

            int loadedCount = 0;
            HashSet<string> occupiedCellKeys = new HashSet<string>();

            foreach (var entry in entries)
            {
                if (entry == null) continue;

                // Lookup furniture data by ID safely; skip invalid / missing IDs without breaking save file
                FurnitureData data = GetFurnitureDataById(entry.FurnitureId);
                if (data == null)
                {
                    Debug.LogWarning($"[FurniturePlacer] Invalid or missing furniture ID '{entry.FurnitureId}'. Skipping entry gracefully.");
                    continue;
                }

                int gx  = entry.GridX;
                int gz  = entry.GridZ;
                int rot = entry.Rotation % 4;

                bool swapped = rot % 2 != 0;
                int fw = swapped ? data.sizeZ : data.sizeX;
                int fh = swapped ? data.sizeX : data.sizeZ;

                // Validate bounds
                bool outOfBounds = false;
                for (int dx = 0; dx < fw; dx++)
                {
                    for (int dz = 0; dz < fh; dz++)
                    {
                        if (!_grid.Data.IsValid(gx + dx, gz + dz))
                        {
                            outOfBounds = true;
                            break;
                        }
                    }
                    if (outOfBounds) break;
                }

                if (outOfBounds)
                {
                    Debug.LogWarning($"[FurniturePlacer] Furniture '{entry.FurnitureId}' at ({gx},{gz}) is out of grid bounds. Skipping.");
                    continue;
                }

                // Check for duplicate placement overlap during load
                bool cellOverlap = false;
                for (int dx = 0; dx < fw; dx++)
                {
                    for (int dz = 0; dz < fh; dz++)
                    {
                        string cellKey = $"{gx + dx}_{gz + dz}";
                        if (occupiedCellKeys.Contains(cellKey))
                        {
                            cellOverlap = true;
                            break;
                        }
                    }
                    if (cellOverlap) break;
                }

                if (cellOverlap)
                {
                    Debug.LogWarning($"[FurniturePlacer] Furniture '{entry.FurnitureId}' at ({gx},{gz}) overlaps another loaded furniture. Skipping duplicate.");
                    continue;
                }

                // Mark grid cells Occupied and store cell keys
                for (int dx = 0; dx < fw; dx++)
                {
                    for (int dz = 0; dz < fh; dz++)
                    {
                        int cx = gx + dx;
                        int cz = gz + dz;
                        _grid.Data.GetCell(cx, cz).State = CellState.Occupied;
                        occupiedCellKeys.Add($"{cx}_{cz}");
                    }
                }

                // Spawn visual object & components
                SpawnFurnitureVisual(data, gx, gz, rot, entry.VariantIndex);
                loadedCount++;
            }

            Debug.Log($"[FurniturePlacer] Loaded {loadedCount}/{entries.Count} furniture items.");
        }

        /// <summary>
        /// Destroys all currently placed furniture GameObjects and resets internal registry.
        /// </summary>
        public void ClearAllPlacedFurniture()
        {
            foreach (var pf in _placedFurnitureList)
            {
                if (pf != null && pf.gameObject != null)
                {
                    Destroy(pf.gameObject);
                }
            }
            _placedFurnitureList.Clear();

            // Safety check for any stray PlacedFurniture components in scene
            PlacedFurniture[] stray = FindObjectsByType<PlacedFurniture>(FindObjectsSortMode.None);
            foreach (var pf in stray)
            {
                if (pf != null && pf.gameObject != null)
                {
                    Destroy(pf.gameObject);
                }
            }
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
            d.id             = name;
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
