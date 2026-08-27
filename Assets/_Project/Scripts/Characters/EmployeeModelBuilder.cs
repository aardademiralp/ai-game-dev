using UnityEngine;

namespace GameDevStudio.Characters
{
    public enum EmployeeAppearanceStyle
    {
        JuniorDev,
        SeniorDev,
        Designer,
        Artist,
        Manager
    }

    /// <summary>
    /// Procedurally builds low-poly character models with 5 distinct visual styles.
    /// Exposes SetSittingPose() for realistic chair-sitting positions.
    /// Model hierarchy (direct children of visual root):
    ///   Torso, Head, Hair, LeftArm, RightArm, LeftLeg, RightLeg
    /// Additional accessory children may exist per style.
    /// </summary>
    public static class EmployeeModelBuilder
    {
        // ── Skin tones ────────────────────────────────────────────
        private static readonly Color SkinLight = new Color(0.96f, 0.78f, 0.65f);
        private static readonly Color SkinTan   = new Color(0.82f, 0.60f, 0.44f);
        private static readonly Color SkinDark  = new Color(0.55f, 0.38f, 0.28f);

        // ── Shirts ────────────────────────────────────────────────
        private static readonly Color ShirtTeal   = new Color(0.18f, 0.52f, 0.48f);
        private static readonly Color ShirtNavy   = new Color(0.15f, 0.25f, 0.48f);
        private static readonly Color ShirtPurple = new Color(0.48f, 0.18f, 0.55f);
        private static readonly Color ShirtOrange = new Color(0.82f, 0.38f, 0.15f);
        private static readonly Color ShirtSuit   = new Color(0.14f, 0.14f, 0.17f);
        private static readonly Color ShirtWhite  = new Color(0.92f, 0.94f, 0.95f);

        // ── Pants ─────────────────────────────────────────────────
        private static readonly Color PantsGrey  = new Color(0.20f, 0.22f, 0.28f);
        private static readonly Color PantsKhaki = new Color(0.66f, 0.60f, 0.46f);
        private static readonly Color PantsBlack = new Color(0.09f, 0.09f, 0.11f);
        private static readonly Color PantsDenim = new Color(0.22f, 0.35f, 0.55f);

        // ── Hair ──────────────────────────────────────────────────
        private static readonly Color HairBrown  = new Color(0.30f, 0.18f, 0.12f);
        private static readonly Color HairDark   = new Color(0.10f, 0.09f, 0.11f);
        private static readonly Color HairBlonde = new Color(0.88f, 0.75f, 0.36f);
        private static readonly Color HairRed    = new Color(0.65f, 0.22f, 0.12f);

        // ── Accents ───────────────────────────────────────────────
        private static readonly Color TieRed     = new Color(0.75f, 0.15f, 0.15f);
        private static readonly Color GlassesDk  = new Color(0.10f, 0.09f, 0.11f);

        // ─────────────────────────────────────────────────────────
        public static GameObject CreateEmployeeModel(
            EmployeeAppearanceStyle style = EmployeeAppearanceStyle.JuniorDev)
        {
            Color shirt, pants, hair, skin;
            skin = SkinLight;

            switch (style)
            {
                case EmployeeAppearanceStyle.SeniorDev:
                    shirt = ShirtNavy;   pants = PantsKhaki; hair = HairDark;   skin = SkinTan;  break;
                case EmployeeAppearanceStyle.Designer:
                    shirt = ShirtPurple; pants = PantsBlack; hair = HairBlonde;                  break;
                case EmployeeAppearanceStyle.Artist:
                    shirt = ShirtOrange; pants = PantsDenim;  hair = HairRed;                     break;
                case EmployeeAppearanceStyle.Manager:
                    shirt = ShirtSuit;   pants = PantsBlack; hair = HairDark;   skin = SkinDark; break;
                default: // JuniorDev
                    shirt = ShirtTeal;   pants = PantsGrey;  hair = HairBrown;                   break;
            }

            var mSkin  = Mat(skin,   "Skin");
            var mShirt = Mat(shirt,  "Shirt");
            var mPants = Mat(pants,  "Pants");
            var mHair  = Mat(hair,   "Hair");

            var root = new GameObject("Employee_Visual");

            // Torso
            Prim(root, "Torso",    new Vector3(0f,    0.70f, 0f),  new Vector3(0.40f, 0.48f, 0.24f), mShirt);

            // Head
            Prim(root, "Head",     new Vector3(0f,    1.09f, 0f),  new Vector3(0.28f, 0.28f, 0.28f), mSkin);

            // Hair — long for Designer, short cap for others
            Vector3 hairScale = style == EmployeeAppearanceStyle.Designer
                ? new Vector3(0.32f, 0.26f, 0.32f)
                : new Vector3(0.30f, 0.08f, 0.30f);
            Prim(root, "Hair", new Vector3(0f, style == EmployeeAppearanceStyle.Designer ? 1.27f : 1.24f, 0f),
                 hairScale, mHair);

            // Style-specific accessories
            if (style == EmployeeAppearanceStyle.Manager)
            {
                // White collar strip
                Prim(root, "Collar",
                    new Vector3(0f, 0.82f, -0.108f),
                    new Vector3(0.13f, 0.22f, 0.04f),
                    Mat(ShirtWhite, "Collar"));
                // Red tie
                Prim(root, "Tie",
                    new Vector3(0f, 0.70f, -0.125f),
                    new Vector3(0.06f, 0.23f, 0.02f),
                    Mat(TieRed, "Tie"));
            }

            if (style == EmployeeAppearanceStyle.SeniorDev)
            {
                // Simple glasses bar
                Prim(root, "Glasses",
                    new Vector3(0f, 1.09f, -0.136f),
                    new Vector3(0.27f, 0.05f, 0.03f),
                    Mat(GlassesDk, "Glasses"));
            }

            // Arms
            Prim(root, "LeftArm",  new Vector3(-0.26f, 0.70f, 0f), new Vector3(0.10f, 0.44f, 0.10f), mShirt);
            Prim(root, "RightArm", new Vector3( 0.26f, 0.70f, 0f), new Vector3(0.10f, 0.44f, 0.10f), mShirt);

            // Legs — two-segment hierarchy (LeftLeg/RightLeg parent = walk anim pivot; UpperLeg+LowerLeg = visuals)
            // LeftLeg parent pivot is at hip joint = Y 0.44 (top of old single-piece leg)
            BuildLeg(root, "LeftLeg",  -0.11f, mPants);
            BuildLeg(root, "RightLeg",  0.11f, mPants);

            // Remove auto-added colliders
            foreach (var col in root.GetComponentsInChildren<Collider>())
            {
                if (Application.isPlaying) Object.Destroy(col);
                else Object.DestroyImmediate(col);
            }

            return root;
        }

        // ─────────────────────────────────────────────────────────
        // SITTING POSE  — call when employee reaches a chair.
        // Root is already snapped to chair.SeatPosition (XZ only, Y = navmesh ground).
        // VisualModel.localPos = 0; hips land at world Y ~0.44 ≈ seat surface 0.455.
        // ─────────────────────────────────────────────────────────
        public static void SetSittingPose(GameObject visualRoot)
        {
            if (visualRoot == null) return;

            Transform lLegPivot = visualRoot.transform.Find("LeftLeg");
            Transform rLegPivot = visualRoot.transform.Find("RightLeg");
            Transform lArm      = visualRoot.transform.Find("LeftArm");
            Transform rArm      = visualRoot.transform.Find("RightArm");
            Transform lLower    = lLegPivot != null ? lLegPivot.Find("LowerLeg") : null;
            Transform rLower    = rLegPivot != null ? rLegPivot.Find("LowerLeg") : null;

            // VisualModel: no XYZ offset — character root is already at seat centre.
            visualRoot.transform.localPosition = Vector3.zero;
            visualRoot.transform.localRotation = Quaternion.identity;

            // Thighs: pivot at hip height, rotate −90° X → UpperLeg extends forward (+Z = toward computer)
            if (lLegPivot != null) { lLegPivot.localPosition = new Vector3(-0.11f, 0.44f, 0f); lLegPivot.localRotation = Quaternion.Euler(-90f, 0f, 0f); }
            if (rLegPivot != null) { rLegPivot.localPosition = new Vector3( 0.11f, 0.44f, 0f); rLegPivot.localRotation = Quaternion.Euler(-90f, 0f, 0f); }

            // Lower legs: +90° relative to thigh pivot → hangs downward from knee
            if (lLower != null) { lLower.localPosition = new Vector3(0f,  0.22f, 0.11f); lLower.localRotation = Quaternion.Euler(90f, 0f, 0f); }
            if (rLower != null) { rLower.localPosition = new Vector3(0f,  0.22f, 0.11f); rLower.localRotation = Quaternion.Euler(90f, 0f, 0f); }

            // Arms: tilt forward −40° → reaching toward keyboard
            if (lArm != null) { lArm.localPosition = new Vector3(-0.26f, 0.70f, 0f); lArm.localRotation = Quaternion.Euler(-40f, 0f, 0f); }
            if (rArm != null) { rArm.localPosition = new Vector3( 0.26f, 0.70f, 0f); rArm.localRotation = Quaternion.Euler(-40f, 0f, 0f); }
        }

        // ─────────────────────────────────────────────────────────
        // STANDING POSE  — explicit reset; called for WorkingStanding.
        // Every body part is set deterministically so no sitting state leaks.
        // ─────────────────────────────────────────────────────────
        public static void SetStandingPose(GameObject visualRoot)
        {
            if (visualRoot == null) return;

            Transform lLegPivot = visualRoot.transform.Find("LeftLeg");
            Transform rLegPivot = visualRoot.transform.Find("RightLeg");
            Transform lArm      = visualRoot.transform.Find("LeftArm");
            Transform rArm      = visualRoot.transform.Find("RightArm");
            Transform lLower    = lLegPivot != null ? lLegPivot.Find("LowerLeg") : null;
            Transform rLower    = rLegPivot != null ? rLegPivot.Find("LowerLeg") : null;

            // VisualModel fully upright, no offset
            visualRoot.transform.localPosition = Vector3.zero;
            visualRoot.transform.localRotation = Quaternion.identity;

            // Hip pivots straight down
            if (lLegPivot != null) { lLegPivot.localPosition = new Vector3(-0.11f, 0.44f, 0f); lLegPivot.localRotation = Quaternion.identity; }
            if (rLegPivot != null) { rLegPivot.localPosition = new Vector3( 0.11f, 0.44f, 0f); rLegPivot.localRotation = Quaternion.identity; }

            // Lower legs hang straight (no knee bend)
            if (lLower != null) { lLower.localPosition = new Vector3(0f, -0.33f, 0f); lLower.localRotation = Quaternion.identity; }
            if (rLower != null) { rLower.localPosition = new Vector3(0f, -0.33f, 0f); rLower.localRotation = Quaternion.identity; }

            // Arms at sides
            if (lArm != null) { lArm.localPosition = new Vector3(-0.26f, 0.70f, 0f); lArm.localRotation = Quaternion.identity; }
            if (rArm != null) { rArm.localPosition = new Vector3( 0.26f, 0.70f, 0f); rArm.localRotation = Quaternion.identity; }
        }

        /// Backwards-compatible shim so any old call site compiles.
        public static void SetSittingPose(GameObject visualRoot, bool isSitting)
        {
            if (isSitting) SetSittingPose(visualRoot);
            else           SetStandingPose(visualRoot);
        }

        // ─────────────────────────────────────────────────────────
        // WALK ANIMATION (call every frame when WalkingToWork)
        // Uses a simple sine wave to swing legs and arms alternately.
        // ─────────────────────────────────────────────────────────
        public static void TickWalkAnimation(GameObject visualRoot, float time, float speed)
        {
            if (visualRoot == null) return;

            Transform lLeg = visualRoot.transform.Find("LeftLeg");
            Transform rLeg = visualRoot.transform.Find("RightLeg");
            Transform lArm = visualRoot.transform.Find("LeftArm");
            Transform rArm = visualRoot.transform.Find("RightArm");

            float swingAmt  = 28f;                          // max degrees of swing
            float frequency = 4.5f * Mathf.Clamp(speed / 2.5f, 0.5f, 2f);
            float phase     = time * frequency;

            float legSwing  = Mathf.Sin(phase)  * swingAmt;
            float armSwing  = Mathf.Sin(phase)  * (swingAmt * 0.6f);

            if (lLeg != null) { lLeg.localPosition = new Vector3(-0.11f, 0.44f, 0f); lLeg.localRotation = Quaternion.Euler( legSwing, 0f, 0f); }
            if (rLeg != null) { rLeg.localPosition = new Vector3( 0.11f, 0.44f, 0f); rLeg.localRotation = Quaternion.Euler(-legSwing, 0f, 0f); }
            if (lArm != null) { lArm.localPosition = new Vector3(-0.26f, 0.70f, 0f); lArm.localRotation = Quaternion.Euler(-armSwing, 0f, 0f); }
            if (rArm != null) { rArm.localPosition = new Vector3( 0.26f, 0.70f, 0f); rArm.localRotation = Quaternion.Euler( armSwing, 0f, 0f); }
        }

        // ─────────────────────────────────────────────────────────
        // IDLE SWAY (subtle breathing/idle movement)
        // ─────────────────────────────────────────────────────────
        public static void TickIdleAnimation(GameObject visualRoot, float time)
        {
            if (visualRoot == null) return;
            Transform head = visualRoot.transform.Find("Head");
            Transform hair = visualRoot.transform.Find("Hair");

            float sway = Mathf.Sin(time * 1.2f) * 0.008f;
            if (head != null) head.localPosition = new Vector3(0f, 1.09f + sway, 0f);
            if (hair != null) hair.localPosition = new Vector3(0f, hair.localPosition.y, 0f);
        }

        // ─────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────
        // ── Two-segment leg builder ────────────────────────────────
        // Creates an empty "LegName" pivot at hip height, with UpperLeg (thigh) and
        // LowerLeg (shin) cubes as children.  Walk animation rotates the pivot;
        // SetSittingPose individually controls each segment.
        private static void BuildLeg(GameObject root, string legName, float xOffset, Material mat)
        {
            // Pivot lives at the hip joint (top of old single-piece leg)
            var pivot = new GameObject(legName);
            pivot.transform.SetParent(root.transform);
            pivot.transform.localPosition = new Vector3(xOffset, 0.44f, 0f);
            pivot.transform.localRotation = Quaternion.identity;

            // Upper leg (thigh): 0.22 long, hangs DOWN from pivot (pivot is at hip)
            var upper = GameObject.CreatePrimitive(PrimitiveType.Cube);
            upper.name = "UpperLeg";
            upper.transform.SetParent(pivot.transform);
            upper.transform.localPosition = new Vector3(0f, -0.11f, 0f);  // center of thigh below pivot
            upper.transform.localScale    = new Vector3(0.13f, 0.22f, 0.13f);
            var r1 = upper.GetComponent<Renderer>(); if (r1) r1.material = mat;

            // Lower leg (shin): 0.20 long, hangs from the knee
            var lower = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lower.name = "LowerLeg";
            lower.transform.SetParent(pivot.transform);
            lower.transform.localPosition = new Vector3(0f, -0.33f, 0f);  // -0.22 knee + -0.11 half-shin
            lower.transform.localScale    = new Vector3(0.11f, 0.22f, 0.11f);
            var r2 = lower.GetComponent<Renderer>(); if (r2) r2.material = mat;
        }

        private static void Prim(GameObject parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent.transform);
            go.transform.localPosition = pos;
            go.transform.localScale    = scale;
            var rend = go.GetComponent<Renderer>();
            if (rend != null) rend.material = mat;
        }

        private static Material Mat(Color color, string name)
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit")
                    ?? Shader.Find("Standard")
                    ?? Shader.Find("Unlit/Color");
            var m = new Material(s) { name = "Mat_" + name };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Color"))     m.SetColor("_Color",     color);
            return m;
        }
    }
}
