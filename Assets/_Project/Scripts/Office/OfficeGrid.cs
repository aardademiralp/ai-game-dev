using UnityEngine;
using UnityEngine.Rendering;

namespace GameDevStudio.Office
{
    /// <summary>
    /// Owns GridData, builds grid lines via LineRenderers (URP-compatible),
    /// and manages the hover-highlight tile.
    /// </summary>
    public class OfficeGrid : MonoBehaviour
    {
        [Header("Grid Size")]
        [SerializeField] private int gridWidth  = 10;
        [SerializeField] private int gridHeight = 10;

        [Header("World Origin (bottom-left corner)")]
        [SerializeField] private Vector3 gridOrigin = new Vector3(-5f, 0.02f, -5f);

        [Header("Visuals")]
        [SerializeField] private Color lineColor      = new Color(0.25f, 0.25f, 0.25f, 1f);
        [SerializeField] private Color highlightColor = new Color(0.2f,  0.85f, 0.3f,  1f);
        [SerializeField] private float lineWidth      = 0.03f;

        // ── Public API ───────────────────────────────────────────
        public GridData Data   { get; private set; }
        public Vector3  Origin => gridOrigin;

        // ── Private ──────────────────────────────────────────────
        private GameObject _highlight;

        // ────────────────────────────────────────────────────────
        private void Awake()
        {
            Data = new GridData(gridWidth, gridHeight);
            BuildGridLines();
            CreateHighlight();
        }

        // ── Grid Line Rendering (LineRenderer — works in URP) ────
        private void BuildGridLines()
        {
            var parent = new GameObject("Grid_Lines");
            parent.transform.SetParent(transform);

            // URP Unlit material – no lighting, pure colour
            Material mat = CreateUnlitMaterial(lineColor);

            // Vertical lines (run along Z axis)
            for (int x = 0; x <= gridWidth; x++)
            {
                Vector3 from = new Vector3(gridOrigin.x + x, gridOrigin.y, gridOrigin.z);
                Vector3 to   = new Vector3(gridOrigin.x + x, gridOrigin.y, gridOrigin.z + gridHeight);
                AddLine(parent, mat, from, to);
            }

            // Horizontal lines (run along X axis)
            for (int z = 0; z <= gridHeight; z++)
            {
                Vector3 from = new Vector3(gridOrigin.x,             gridOrigin.y, gridOrigin.z + z);
                Vector3 to   = new Vector3(gridOrigin.x + gridWidth, gridOrigin.y, gridOrigin.z + z);
                AddLine(parent, mat, from, to);
            }
        }

        private void AddLine(GameObject parent, Material mat, Vector3 from, Vector3 to)
        {
            var go = new GameObject("Line");
            go.transform.SetParent(parent.transform);

            var lr               = go.AddComponent<LineRenderer>();
            lr.sharedMaterial    = mat;
            lr.useWorldSpace     = true;
            lr.positionCount     = 2;
            lr.startWidth        = lineWidth;
            lr.endWidth          = lineWidth;
            lr.shadowCastingMode = ShadowCastingMode.Off;
            lr.receiveShadows    = false;
            lr.SetPosition(0, from);
            lr.SetPosition(1, to);
        }

        // ── Hover Highlight ──────────────────────────────────────
        private void CreateHighlight()
        {
            _highlight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _highlight.name = "Grid_Highlight";
            _highlight.transform.SetParent(transform);
            _highlight.transform.localScale = new Vector3(0.95f, 0.02f, 0.95f);
            _highlight.SetActive(false);

            // Highlight must never block raycasts
            Destroy(_highlight.GetComponent<BoxCollider>());

            var rend = _highlight.GetComponent<Renderer>();
            var mat  = new Material(rend.sharedMaterial);
            // URP Lit uses _BaseColor, not _Color
            SetUrpColor(mat, highlightColor);
            rend.material = mat;
        }

        public void ShowHighlight(int x, int z)
        {
            if (!Data.IsValid(x, z)) { ClearHighlight(); return; }

            Vector3 pos = Data.CellToWorld(x, z, gridOrigin);
            pos.y = gridOrigin.y + 0.01f;   // sit just above grid lines

            _highlight.transform.position = pos;
            _highlight.SetActive(true);
        }

        public void ClearHighlight() => _highlight.SetActive(false);

        // ── Material Helpers ─────────────────────────────────────
        /// Creates an Unlit material. Works in URP and Built-in RP.
        private static Material CreateUnlitMaterial(Color color)
        {
            // Try URP Unlit first, fall back to built-in Unlit/Color
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Color");

            var mat = new Material(shader);
            SetUrpColor(mat, color);
            return mat;
        }

        /// Sets colour on both _BaseColor (URP) and _Color (Built-in) if they exist.
        private static void SetUrpColor(Material mat, Color color)
        {
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))     mat.SetColor("_Color",     color);
        }
    }
}
