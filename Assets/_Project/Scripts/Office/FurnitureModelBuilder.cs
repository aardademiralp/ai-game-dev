using UnityEngine;

namespace GameDevStudio.Office
{
    /// <summary>
    /// Constructs low-poly / stylized 3D models for furniture using Unity primitives.
    /// Shared visual palette across Desk, Chair, and Computer.
    /// </summary>
    public static class FurnitureModelBuilder
    {
        // ── Shared Color Palette ──────────────────────────────────────────
        private static readonly Color ColorWood        = new Color(0.75f, 0.52f, 0.32f); // Warm Oak
        private static readonly Color ColorWoodAccent  = new Color(0.35f, 0.24f, 0.18f); // Dark Wood Trim
        private static readonly Color ColorDarkMetal   = new Color(0.20f, 0.22f, 0.25f); // Anthracite Metal
        private static readonly Color ColorChairFabric = new Color(0.20f, 0.48f, 0.85f); // Office Blue
        private static readonly Color ColorCharcoal    = new Color(0.15f, 0.16f, 0.18f); // Dark Casing
        private static readonly Color ColorScreen      = new Color(0.20f, 0.75f, 0.95f); // Stylized Display Cyan
        private static readonly Color ColorLightGrey   = new Color(0.85f, 0.87f, 0.90f); // Keycaps

        public static GameObject CreateModel(FurnitureType type)
        {
            switch (type)
            {
                case FurnitureType.Desk:
                    return CreateDesk();
                case FurnitureType.Chair:
                    return CreateChair();
                case FurnitureType.Computer:
                    return CreateComputer();
                default:
                    return CreateFallbackCube();
            }
        }

        public static void RemoveColliders(GameObject root)
        {
            if (root == null) return;
            Collider[] colliders = root.GetComponentsInChildren<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                if (Application.isPlaying)
                    Object.Destroy(colliders[i]);
                else
                    Object.DestroyImmediate(colliders[i]);
            }
        }

        // ── 1. DESK (Footprint 2x1) ───────────────────────────────────────
        private static GameObject CreateDesk()
        {
            GameObject root = new GameObject("Desk_Model");

            Material matWood       = CreateMaterial(ColorWood,       "Mat_DeskWood");
            Material matWoodAccent = CreateMaterial(ColorWoodAccent, "Mat_DeskAccent");
            Material matLegs       = CreateMaterial(ColorDarkMetal,  "Mat_DeskLegs");

            // Tabletop (width X=1.85, depth Z=0.85, height=0.08)
            GameObject top = GameObject.CreatePrimitive(PrimitiveType.Cube);
            top.name = "Tabletop";
            top.transform.SetParent(root.transform);
            top.transform.localPosition = new Vector3(0f, 0.62f, 0f);
            top.transform.localScale    = new Vector3(1.85f, 0.08f, 0.85f);
            ApplyMaterial(top, matWood);

            // 4 Legs
            Vector3 legScale = new Vector3(0.08f, 0.58f, 0.08f);
            Vector3[] legPositions = new[]
            {
                new Vector3(-0.80f, 0.29f, -0.34f),
                new Vector3( 0.80f, 0.29f, -0.34f),
                new Vector3(-0.80f, 0.29f,  0.34f),
                new Vector3( 0.80f, 0.29f,  0.34f),
            };

            for (int i = 0; i < legPositions.Length; i++)
            {
                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leg.name = $"Leg_{i+1}";
                leg.transform.SetParent(root.transform);
                leg.transform.localPosition = legPositions[i];
                leg.transform.localScale    = legScale;
                ApplyMaterial(leg, matLegs);
            }

            // Back modesty panel
            GameObject modesty = GameObject.CreatePrimitive(PrimitiveType.Cube);
            modesty.name = "ModestyPanel";
            modesty.transform.SetParent(root.transform);
            modesty.transform.localPosition = new Vector3(0f, 0.45f, 0.34f);
            modesty.transform.localScale    = new Vector3(1.52f, 0.22f, 0.03f);
            ApplyMaterial(modesty, matWoodAccent);

            return root;
        }

        // ── 2. CHAIR (Footprint 1x1) ──────────────────────────────────────
        private static GameObject CreateChair()
        {
            GameObject root = new GameObject("Chair_Model");

            Material matFabric = CreateMaterial(ColorChairFabric, "Mat_ChairFabric");
            Material matLegs   = CreateMaterial(ColorDarkMetal,   "Mat_ChairLegs");

            // Seat cushion
            GameObject seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seat.name = "Seat";
            seat.transform.SetParent(root.transform);
            seat.transform.localPosition = new Vector3(0f, 0.42f, -0.04f);
            seat.transform.localScale    = new Vector3(0.55f, 0.07f, 0.55f);
            ApplyMaterial(seat, matFabric);

            // Backrest
            GameObject back = GameObject.CreatePrimitive(PrimitiveType.Cube);
            back.name = "Backrest";
            back.transform.SetParent(root.transform);
            back.transform.localPosition = new Vector3(0f, 0.72f, 0.20f);
            back.transform.localScale    = new Vector3(0.52f, 0.50f, 0.06f);
            ApplyMaterial(back, matFabric);

            // Backrest connectors (2 vertical bars)
            Vector3[] connPositions = new[]
            {
                new Vector3(-0.18f, 0.43f, 0.20f),
                new Vector3( 0.18f, 0.43f, 0.20f),
            };
            for (int i = 0; i < connPositions.Length; i++)
            {
                GameObject conn = GameObject.CreatePrimitive(PrimitiveType.Cube);
                conn.name = $"BackSupport_{i+1}";
                conn.transform.SetParent(root.transform);
                conn.transform.localPosition = connPositions[i];
                conn.transform.localScale    = new Vector3(0.04f, 0.10f, 0.04f);
                ApplyMaterial(conn, matLegs);
            }

            // 4 Legs
            Vector3 legScale = new Vector3(0.05f, 0.385f, 0.05f);
            Vector3[] legPositions = new[]
            {
                new Vector3(-0.21f, 0.1925f, -0.23f),
                new Vector3( 0.21f, 0.1925f, -0.23f),
                new Vector3(-0.21f, 0.1925f,  0.15f),
                new Vector3( 0.21f, 0.1925f,  0.15f),
            };

            for (int i = 0; i < legPositions.Length; i++)
            {
                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leg.name = $"Leg_{i+1}";
                leg.transform.SetParent(root.transform);
                leg.transform.localPosition = legPositions[i];
                leg.transform.localScale    = legScale;
                ApplyMaterial(leg, matLegs);
            }

            return root;
        }

        // ── 3. COMPUTER (Footprint 1x1) ───────────────────────────────────
        private static GameObject CreateComputer()
        {
            GameObject root = new GameObject("Computer_Model");

            Material matWood      = CreateMaterial(ColorWood,      "Mat_CompDesk");
            Material matLegs      = CreateMaterial(ColorDarkMetal, "Mat_CompLegs");
            Material matBody      = CreateMaterial(ColorCharcoal,  "Mat_CompBody");
            Material matScreen    = CreateMaterial(ColorScreen,    "Mat_CompScreen");
            Material matLightKeys = CreateMaterial(ColorLightGrey, "Mat_CompKeys");

            // Compact Desk Surface (fits inside 1x1)
            GameObject table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = "DeskSurface";
            table.transform.SetParent(root.transform);
            table.transform.localPosition = new Vector3(0f, 0.58f, 0f);
            table.transform.localScale    = new Vector3(0.85f, 0.06f, 0.75f);
            ApplyMaterial(table, matWood);

            // 4 Compact Desk Legs
            Vector3 legScale = new Vector3(0.06f, 0.55f, 0.06f);
            Vector3[] legPositions = new[]
            {
                new Vector3(-0.36f, 0.275f, -0.31f),
                new Vector3( 0.36f, 0.275f, -0.31f),
                new Vector3(-0.36f, 0.275f,  0.31f),
                new Vector3( 0.36f, 0.275f,  0.31f),
            };

            for (int i = 0; i < legPositions.Length; i++)
            {
                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leg.name = $"Leg_{i+1}";
                leg.transform.SetParent(root.transform);
                leg.transform.localPosition = legPositions[i];
                leg.transform.localScale    = legScale;
                ApplyMaterial(leg, matLegs);
            }

            // Monitor Base
            GameObject monBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            monBase.name = "MonitorBase";
            monBase.transform.SetParent(root.transform);
            monBase.transform.localPosition = new Vector3(0f, 0.62f, 0.12f);
            monBase.transform.localScale    = new Vector3(0.22f, 0.02f, 0.16f);
            ApplyMaterial(monBase, matLegs);

            // Monitor Pole / Neck
            GameObject monPole = GameObject.CreatePrimitive(PrimitiveType.Cube);
            monPole.name = "MonitorStand";
            monPole.transform.SetParent(root.transform);
            monPole.transform.localPosition = new Vector3(0f, 0.74f, 0.14f);
            monPole.transform.localScale    = new Vector3(0.05f, 0.22f, 0.05f);
            ApplyMaterial(monPole, matLegs);

            // Monitor Outer Body Frame
            GameObject monBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            monBody.name = "MonitorBody";
            monBody.transform.SetParent(root.transform);
            monBody.transform.localPosition = new Vector3(0f, 0.92f, 0.12f);
            monBody.transform.localScale    = new Vector3(0.65f, 0.40f, 0.06f);
            ApplyMaterial(monBody, matBody);

            // Screen Surface Display (Front panel facing -Z)
            GameObject monScreen = GameObject.CreatePrimitive(PrimitiveType.Cube);
            monScreen.name = "ScreenSurface";
            monScreen.transform.SetParent(root.transform);
            monScreen.transform.localPosition = new Vector3(0f, 0.92f, 0.088f);
            monScreen.transform.localScale    = new Vector3(0.59f, 0.34f, 0.01f);
            ApplyMaterial(monScreen, matScreen);

            // Keyboard Base
            GameObject kbBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            kbBase.name = "KeyboardBase";
            kbBase.transform.SetParent(root.transform);
            kbBase.transform.localPosition = new Vector3(0f, 0.625f, -0.12f);
            kbBase.transform.localScale    = new Vector3(0.42f, 0.03f, 0.16f);
            ApplyMaterial(kbBase, matBody);

            // Keyboard Key Panel
            GameObject kbKeys = GameObject.CreatePrimitive(PrimitiveType.Cube);
            kbKeys.name = "KeyboardKeys";
            kbKeys.transform.SetParent(root.transform);
            kbKeys.transform.localPosition = new Vector3(0f, 0.642f, -0.12f);
            kbKeys.transform.localScale    = new Vector3(0.38f, 0.01f, 0.13f);
            ApplyMaterial(kbKeys, matLightKeys);

            // Mouse
            GameObject mouse = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mouse.name = "Mouse";
            mouse.transform.SetParent(root.transform);
            mouse.transform.localPosition = new Vector3(0.26f, 0.625f, -0.12f);
            mouse.transform.localScale    = new Vector3(0.07f, 0.03f, 0.10f);
            ApplyMaterial(mouse, matBody);

            return root;
        }

        private static GameObject CreateFallbackCube()
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Fallback_Cube";
            return cube;
        }

        // ── Helpers ───────────────────────────────────────────────────────
        private static Material CreateMaterial(Color color, string name)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") 
                         ?? Shader.Find("Standard") 
                         ?? Shader.Find("Unlit/Color");

            Material mat = new Material(shader);
            mat.name = name;

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))     mat.SetColor("_Color",     color);

            return mat;
        }

        private static void ApplyMaterial(GameObject go, Material mat)
        {
            Renderer rend = go.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material = mat;
            }
        }
    }
}
