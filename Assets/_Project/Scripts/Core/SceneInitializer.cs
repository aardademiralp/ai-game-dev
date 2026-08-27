using UnityEngine;
using GameDevStudio.Camera;
using GameDevStudio.Office;

namespace GameDevStudio.Core
{
    /// <summary>
    /// Bootstraps the game scene at runtime.
    /// Attach to any GameObject in MainScene and hit Play.
    ///
    /// Responsibilities (Aşama 1 scope):
    ///   1. Build a simple office floor from primitives
    ///   2. Attach IsometricCameraController to Main Camera if missing
    ///   3. Centre the camera focus on the floor
    /// </summary>
    public class SceneInitializer : MonoBehaviour
    {
        [Header("Floor")]
        [SerializeField] private int   floorSizeX  = 10;   // units
        [SerializeField] private int   floorSizeZ  = 10;   // units
        [SerializeField] private Color floorColor  = new Color(0.86f, 0.83f, 0.76f);
        [SerializeField] private Color wallColor   = new Color(0.60f, 0.60f, 0.60f);
        [SerializeField] private float wallHeight  = 0.15f;

        [Header("Camera")]
        [SerializeField] private UnityEngine.Camera mainCamera; // auto-found if null

        // ─────────────────────────────────────────────────────
        private void Awake()
        {
            if (mainCamera == null)
                mainCamera = UnityEngine.Camera.main;

            BuildOfficeFloor();
            SetupCamera();
            SetupGrid();         // Aşama 2 — grid + input
        }

        // ── Floor ─────────────────────────────────────────────
        private void BuildOfficeFloor()
        {
            GameObject root = new GameObject("Office_Floor");

            // Plane (Unity Plane = 10×10 units at scale 1, so divide by 10)
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.SetParent(root.transform);
            floor.transform.localPosition = Vector3.zero;
            floor.transform.localScale    = new Vector3(floorSizeX * 0.1f, 1f, floorSizeZ * 0.1f);
            SetColor(floor, floorColor);

            // Four boundary walls (thin cubes)
            float hx = floorSizeX * 0.5f;
            float hz = floorSizeZ * 0.5f;

            CreateWall(root, "Wall_North", new Vector3(0f,            wallHeight * 0.5f, hz),
                                           new Vector3(floorSizeX,    wallHeight,        0.1f));
            CreateWall(root, "Wall_South", new Vector3(0f,            wallHeight * 0.5f, -hz),
                                           new Vector3(floorSizeX,    wallHeight,        0.1f));
            CreateWall(root, "Wall_East",  new Vector3(hx,            wallHeight * 0.5f, 0f),
                                           new Vector3(0.1f,          wallHeight,        floorSizeZ));
            CreateWall(root, "Wall_West",  new Vector3(-hx,           wallHeight * 0.5f, 0f),
                                           new Vector3(0.1f,          wallHeight,        floorSizeZ));
        }

        private void CreateWall(GameObject parent, string wallName, Vector3 position, Vector3 scale)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = wallName;
            wall.transform.SetParent(parent.transform);
            wall.transform.localPosition = position;
            wall.transform.localScale    = scale;
            SetColor(wall, wallColor);
        }

        // ── Camera ────────────────────────────────────────────
        private void SetupCamera()
        {
            if (mainCamera == null)
            {
                Debug.LogWarning("[SceneInitializer] No Main Camera found in scene.");
                return;
            }

            IsometricCameraController controller =
                mainCamera.GetComponent<IsometricCameraController>() ??
                mainCamera.gameObject.AddComponent<IsometricCameraController>();

            controller.SetFocusPoint(Vector3.zero); // centre on floor
        }

        // ── Grid System ────────────────────────────────────
        private void SetupGrid()
        {
            OfficeGrid existingGrid = FindFirstObjectByType<OfficeGrid>();

            if (existingGrid != null)
            {
                // OfficeGrid is in scene — make sure GridInputHandler is also attached
                GridInputHandler handler = existingGrid.GetComponent<GridInputHandler>();
                if (handler == null)
                {
                    handler = existingGrid.gameObject.AddComponent<GridInputHandler>();
                    Debug.Log("[SceneInitializer] GridInputHandler was missing — added to existing GridSystem.");
                }

                // Also ensure FurniturePlacer is attached
                if (existingGrid.GetComponent<FurniturePlacer>() == null)
                    existingGrid.gameObject.AddComponent<FurniturePlacer>();
                else
                {
                    Debug.Log("[SceneInitializer] GridSystem fully present (OfficeGrid + GridInputHandler).");
                }
                return;
            }

            // Nothing in scene — create from scratch
            GameObject go = new GameObject("GridSystem");
            go.AddComponent<OfficeGrid>();
            go.AddComponent<GridInputHandler>();
            go.AddComponent<FurniturePlacer>();
            Debug.Log("[SceneInitializer] GridSystem created from scratch.");
        }


        // ── Helpers ───────────────────────────────────────────
        private static void SetColor(GameObject go, Color color)
        {
            Renderer rend = go.GetComponent<Renderer>();
            if (rend == null) return;

            // Instantiate a new material so each object has its own colour
            Material mat = new Material(rend.sharedMaterial);
            mat.color = color;
            rend.material = mat;
        }
    }
}
