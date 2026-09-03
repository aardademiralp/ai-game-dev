using UnityEngine;
using Unity.AI.Navigation;
using System.Collections.Generic;
using GameDevStudio.Economy;
using GameDevStudio.Employees;
using GameDevStudio.Characters;

namespace GameDevStudio.Office
{
    [System.Serializable]
    public class OfficeLevelConfig
    {
        public int levelNumber;
        public string officeName;
        public int staffCapacity;
        public int upgradeCost;
        public int gridWidth;
        public int gridHeight;

        public OfficeLevelConfig(int levelNumber, string officeName, int staffCapacity, int upgradeCost, int gridWidth, int gridHeight)
        {
            this.levelNumber = levelNumber;
            this.officeName = officeName;
            this.staffCapacity = staffCapacity;
            this.upgradeCost = upgradeCost;
            this.gridWidth = gridWidth;
            this.gridHeight = gridHeight;
        }
    }

    /// <summary>
    /// Central manager for office progression, physical floor expansion, and staff capacity limits.
    /// </summary>
    public class OfficeManager : MonoBehaviour
    {
        public static OfficeManager Instance { get; private set; }

        [Header("Office Levels")]
        private readonly List<OfficeLevelConfig> _levels = new List<OfficeLevelConfig>
        {
            new OfficeLevelConfig(1, "Starter Office", 2,  0,      6,  6),
            new OfficeLevelConfig(2, "Small Office",   4,  5000,   10, 10),
            new OfficeLevelConfig(3, "Medium Office",  6,  12000,  14, 14),
            new OfficeLevelConfig(4, "Large Office",   10, 25000,  18, 18)
        };

        public int CurrentLevelIndex { get; private set; } = 0; // 0-indexed (0 = Level 1)

        public OfficeLevelConfig CurrentConfig => _levels[Mathf.Clamp(CurrentLevelIndex, 0, _levels.Count - 1)];
        public bool HasNextLevel => CurrentLevelIndex + 1 < _levels.Count;
        public OfficeLevelConfig NextConfig => HasNextLevel ? _levels[CurrentLevelIndex + 1] : null;

        public int CurrentCapacity => CurrentConfig.staffCapacity;
        public string CurrentOfficeName => CurrentConfig.officeName;
        public int CurrentLevelNumber => CurrentConfig.levelNumber;

        public string NextUpgradeName => HasNextLevel ? NextConfig.officeName : "MAX LEVEL";
        public int NextUpgradeCost => HasNextLevel ? NextConfig.upgradeCost : 0;
        public int NextUpgradeCapacity => HasNextLevel ? NextConfig.staffCapacity : CurrentCapacity;
        public int NextOfficeCapacity => NextUpgradeCapacity;

        public event System.Action OnOfficeExpanded;

        [Header("Colors & Visuals")]
        [SerializeField] private Color floorColor = new Color(0.86f, 0.83f, 0.76f);
        [SerializeField] private Color wallColor  = new Color(0.60f, 0.60f, 0.60f);
        [SerializeField] private float wallHeight = 0.15f;

        private GameObject _floorRoot;
        private GameObject _floorPlane;
        private NavMeshSurface _navSurface;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            InitializeOfficeFloor();
        }

        public bool CanHireMoreEmployees()
        {
            int currentEmpCount = EmployeeManager.Instance != null ? EmployeeManager.Instance.Employees.Count : 0;
            return currentEmpCount < CurrentCapacity;
        }

        public bool CanExpandOffice()
        {
            if (!HasNextLevel) return false;
            int cost = NextConfig.upgradeCost;
            return MoneyManager.Instance != null && MoneyManager.Instance.HasEnoughMoney(cost);
        }

        public bool TryExpandOffice()
        {
            if (!HasNextLevel)
            {
                Debug.LogWarning("[OfficeManager] Already at max office level!");
                return false;
            }

            int cost = NextConfig.upgradeCost;
            if (MoneyManager.Instance != null && MoneyManager.Instance.TrySpendMoney(cost))
            {
                CurrentLevelIndex++;
                Debug.Log($"[OfficeManager] Office expanded to Level {CurrentLevelNumber}: {CurrentOfficeName} (Capacity: {CurrentCapacity})");
                
                ApplyPhysicalExpansion();
                OnOfficeExpanded?.Invoke();
                return true;
            }

            Debug.LogWarning($"[OfficeManager] Failed to expand office — Insufficient funds! Needed: ${cost}");
            return false;
        }

        public void InitializeOfficeFloor()
        {
            ApplyPhysicalExpansion();
        }

        private void ApplyPhysicalExpansion()
        {
            var config = CurrentConfig;
            int sizeX = config.gridWidth;
            int sizeZ = config.gridHeight;

            // 1. Create or Find Floor Root
            if (_floorRoot == null)
            {
                _floorRoot = GameObject.Find("Office_Floor");
                if (_floorRoot == null)
                {
                    _floorRoot = new GameObject("Office_Floor");
                }
            }

            // 2. Setup Floor Mesh Primitive
            Transform floorTransform = _floorRoot.transform.Find("Floor");
            if (floorTransform == null)
            {
                _floorPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
                _floorPlane.name = "Floor";
                _floorPlane.transform.SetParent(_floorRoot.transform, false);
            }
            else
            {
                _floorPlane = floorTransform.gameObject;
            }

            _floorPlane.transform.localPosition = Vector3.zero;
            _floorPlane.transform.localScale = new Vector3(sizeX * 0.1f, 1f, sizeZ * 0.1f);
            SetColor(_floorPlane, floorColor);

            // 3. Setup/Rebuild NavMeshSurface
            if (_navSurface == null)
            {
                _navSurface = _floorPlane.GetComponent<NavMeshSurface>() ?? _floorPlane.AddComponent<NavMeshSurface>();
            }
            _navSurface.BuildNavMesh();

            // 4. Update Boundary Walls
            float hx = sizeX * 0.5f;
            float hz = sizeZ * 0.5f;

            UpdateOrCreateWall("Wall_North", new Vector3(0f, wallHeight * 0.5f, hz),  new Vector3(sizeX, wallHeight, 0.1f));
            UpdateOrCreateWall("Wall_South", new Vector3(0f, wallHeight * 0.5f, -hz), new Vector3(sizeX, wallHeight, 0.1f));
            UpdateOrCreateWall("Wall_East",  new Vector3(hx, wallHeight * 0.5f, 0f),  new Vector3(0.1f, wallHeight, sizeZ));
            UpdateOrCreateWall("Wall_West",  new Vector3(-hx, wallHeight * 0.5f, 0f), new Vector3(0.1f, wallHeight, sizeZ));

            // 5. Update OfficeGrid GridSystem
            OfficeGrid officeGrid = FindFirstObjectByType<OfficeGrid>();
            if (officeGrid != null)
            {
                Vector3 newOrigin = new Vector3(-hx, 0.02f, -hz);
                officeGrid.ResizeGrid(sizeX, sizeZ, newOrigin);
                Debug.Log($"[OfficeManager] OfficeGrid resized to {sizeX}x{sizeZ} with origin {newOrigin}");
            }
        }

        private void UpdateOrCreateWall(string wallName, Vector3 localPos, Vector3 localScale)
        {
            Transform wallTransform = _floorRoot.transform.Find(wallName);
            GameObject wallGo;

            if (wallTransform == null)
            {
                wallGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wallGo.name = wallName;
                wallGo.transform.SetParent(_floorRoot.transform, false);
            }
            else
            {
                wallGo = wallTransform.gameObject;
            }

            wallGo.transform.localPosition = localPos;
            wallGo.transform.localScale = localScale;
            SetColor(wallGo, wallColor);
        }

        private static void SetColor(GameObject go, Color color)
        {
            Renderer rend = go.GetComponent<Renderer>();
            if (rend == null) return;

            Material mat = new Material(rend.sharedMaterial);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))     mat.SetColor("_Color",     color);
            rend.material = mat;
        }
    }
}
