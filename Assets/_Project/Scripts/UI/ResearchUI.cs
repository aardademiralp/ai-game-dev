using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using GameDevStudio.AI;
using GameDevStudio.Economy;

namespace GameDevStudio.UI
{
    /// <summary>
    /// Technology Tree UI — toggled with F key.
    /// Shows AI Core stats, category tabs, technology cards, and active research panel.
    /// Replaces the old employee-assignment research panel.
    /// </summary>
    public class ResearchUI : MonoBehaviour
    {
        public static ResearchUI Instance { get; private set; }
        public bool IsOpen => _isOpen;

        // ── UI Refs ──────────────────────────────────────────────────────────
        private GameObject _panel;
        private Text       _aiStatsText;
        private Text       _activeResearchText;
        private Text       _statusMessageText;
        private GameObject _techListContainer;
        private GameObject _detailPanel;
        private Font       _uiFont;

        private TechCategory         _selectedCategory = TechCategory.CoreTechnology;
        private TechnologyData       _selectedTech;
        private readonly List<GameObject> _techCards = new List<GameObject>();
        private readonly List<Button>     _tabButtons = new List<Button>();

        // ── Input ────────────────────────────────────────────────────────────
        private InputAction _keyF;
        private InputAction _keyEsc;
        private bool        _isOpen;

        // ── Handlers (named for safe unsubscription) ─────────────────────────
        private System.Action<TechnologyData, float> _onProgressHandler;
        private System.Action<TechnologyData>        _onCompletedHandler;
        private System.Action<string>                _onFailedHandler;

        // ────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                   ?? Resources.GetBuiltinResource<Font>("Arial.ttf")
                   ?? Font.CreateDynamicFontFromOSFont("Arial", 24);

            _keyF   = new InputAction("TechTree_F",   InputActionType.Button, "<Keyboard>/f");
            _keyEsc = new InputAction("TechTree_Esc", InputActionType.Button, "<Keyboard>/escape");
            _keyF.Enable();
            _keyEsc.Enable();

            _onProgressHandler  = (t, p)  => { if (_isOpen) RefreshActiveResearch(); };
            _onCompletedHandler = (t)      => { if (_isOpen) { RefreshAll(); ShowStatus($"✓ {t.Name} completed!"); } };
            _onFailedHandler    = (msg)    => { if (_isOpen) ShowStatus(msg); };

            BuildUI();
        }

        private void Start()
        {
            if (TechnologyResearchManager.Instance != null)
            {
                TechnologyResearchManager.Instance.OnResearchProgress  += _onProgressHandler;
                TechnologyResearchManager.Instance.OnResearchCompleted += _onCompletedHandler;
                TechnologyResearchManager.Instance.OnResearchFailed    += _onFailedHandler;
            }
        }

        private void OnDestroy()
        {
            _keyF?.Dispose();
            _keyEsc?.Dispose();
            if (TechnologyResearchManager.Instance != null)
            {
                TechnologyResearchManager.Instance.OnResearchProgress  -= _onProgressHandler;
                TechnologyResearchManager.Instance.OnResearchCompleted -= _onCompletedHandler;
                TechnologyResearchManager.Instance.OnResearchFailed    -= _onFailedHandler;
            }
        }

        private void Update()
        {
            // All gameplay hotkeys blocked outside an active session
            if (!Flow.GameStateManager.IsGameplayActive)
            {
                if (_isOpen) CloseWindow();
                return;
            }

            if (_keyF != null && _keyF.WasPressedThisFrame())
            {
                if (!Office.FurniturePlacer.IsPlacing)
                {
                    if (RecruitmentUI.Instance != null && RecruitmentUI.Instance.IsOpen)
                        RecruitmentUI.Instance.CloseWindow();
                    if (OfficeExpansionUI.Instance != null && OfficeExpansionUI.Instance.IsOpen)
                        OfficeExpansionUI.Instance.CloseWindow();
                    ToggleWindow();
                }
            }
            if (_isOpen && _keyEsc != null && _keyEsc.WasPressedThisFrame())
                CloseWindow();

            // Live-update active research progress bar each frame while open
            if (_isOpen && TechnologyResearchManager.Instance?.ActiveResearch != null)
                RefreshActiveResearch();
        }

        public void ToggleWindow() { if (_isOpen) CloseWindow(); else OpenWindow(); }

        public void OpenWindow()
        {
            if (_panel == null) BuildUI();
            _isOpen = true;
            _panel.SetActive(true);
            RefreshAll();
        }

        public void CloseWindow()
        {
            _isOpen = false;
            if (_panel != null) _panel.SetActive(false);
        }

        // ── Build static UI structure ────────────────────────────────────────
        private void BuildUI()
        {
            Transform old = transform.Find("TechTreeCanvas");
            if (old != null) DestroyImmediate(old.gameObject);

            var canvasGo = new GameObject("TechTreeCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // Background
            _panel = CreateGo("TechWindow", canvasGo.transform);
            var bg = _panel.AddComponent<Image>();
            bg.color = new Color(0.03f, 0.04f, 0.07f, 0.93f);
            FullStretch(_panel.GetComponent<RectTransform>());

            // Main box
            var box = CreateGo("Box", _panel.transform);
            box.AddComponent<Image>().color = new Color(0.07f, 0.09f, 0.13f, 0.99f);
            var boxRt = box.GetComponent<RectTransform>();
            boxRt.anchorMin = new Vector2(0.03f, 0.02f);
            boxRt.anchorMax = new Vector2(0.97f, 0.98f);
            boxRt.offsetMin = boxRt.offsetMax = Vector2.zero;

            // Title
            var titleTxt = MakeText("Title", box.transform, "AI CORE TECHNOLOGY", 26, FontStyle.Bold,
                new Color(0f, 0.9f, 1f), TextAnchor.MiddleCenter);
            SetAnchors(titleTxt.GetComponent<RectTransform>(), 0f, 0.93f, 1f, 1f, 0, -5f, 0, -5f);

            // AI Stats bar
            _aiStatsText = MakeText("AIStats", box.transform, "", 15, FontStyle.Normal,
                new Color(1f, 0.85f, 0.3f), TextAnchor.MiddleCenter);
            SetAnchors(_aiStatsText.GetComponent<RectTransform>(), 0f, 0.86f, 1f, 0.93f, 5, 0, -5, 0);

            // Category tab strip
            BuildCategoryTabs(box.transform);

            // Tech list scroll area (left 60%)
            var listBg = CreateGo("ListBg", box.transform);
            listBg.AddComponent<Image>().color = new Color(0.05f, 0.06f, 0.10f, 0.7f);
            SetAnchors(listBg.GetComponent<RectTransform>(), 0f, 0.18f, 0.6f, 0.84f, 5, 5, -5, -5);

            var listContent = CreateGo("ListContent", listBg.transform);
            _techListContainer = listContent;
            FullStretch(listContent.GetComponent<RectTransform>() ?? listContent.AddComponent<RectTransform>());
            var vlg = listContent.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6; vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childControlWidth = true; vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;

            // Detail panel (right 38%)
            _detailPanel = CreateGo("DetailPanel", box.transform);
            _detailPanel.AddComponent<Image>().color = new Color(0.06f, 0.08f, 0.13f, 0.9f);
            SetAnchors(_detailPanel.GetComponent<RectTransform>(), 0.62f, 0.18f, 1f, 0.84f, 5, 5, -5, -5);

            // Active research panel (bottom strip)
            var activeBg = CreateGo("ActiveBg", box.transform);
            activeBg.AddComponent<Image>().color = new Color(0.04f, 0.08f, 0.05f, 0.9f);
            SetAnchors(activeBg.GetComponent<RectTransform>(), 0f, 0.05f, 1f, 0.18f, 5, 5, -5, -5);

            _activeResearchText = MakeText("ActiveText", activeBg.transform, "No active research.", 16,
                FontStyle.Normal, new Color(0.26f, 0.93f, 0.44f), TextAnchor.MiddleLeft);
            SetAnchors(_activeResearchText.GetComponent<RectTransform>(), 0f, 0f, 1f, 1f, 10, 5, -10, -5);

            // Status message
            _statusMessageText = MakeText("StatusMsg", box.transform, "", 15, FontStyle.Bold,
                new Color(1f, 0.5f, 0.3f), TextAnchor.MiddleCenter);
            SetAnchors(_statusMessageText.GetComponent<RectTransform>(), 0f, 0f, 1f, 0.05f, 5, 0, -5, 0);

            // Close button
            MakeButton("CLOSE (Esc/F)", box.transform,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0.5f),
                new Vector2(-10f, 10f), new Vector2(160f, 36f),
                new Color(0.3f, 0.12f, 0.12f), CloseWindow);

            _panel.SetActive(false);
        }

        private void BuildCategoryTabs(Transform parent)
        {
            _tabButtons.Clear();
            var tabRow = CreateGo("TabRow", parent);
            SetAnchors(tabRow.GetComponent<RectTransform>() ?? tabRow.AddComponent<RectTransform>(),
                       0f, 0.84f, 1f, 0.92f, 5, 0, -5, 0);
            var hlg = tabRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4; hlg.padding = new RectOffset(4, 4, 4, 4);
            hlg.childControlWidth = true; hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;

            var categories = new (TechCategory cat, string label)[]
            {
                (TechCategory.CoreTechnology,  "CORE"),
                (TechCategory.Intelligence,    "INTELLIGENCE"),
                (TechCategory.MachineLearning, "ML"),
                (TechCategory.GenerativeAI,    "GEN AI"),
                (TechCategory.Software,        "SOFTWARE"),
                (TechCategory.GameDevelopment, "GAME DEV"),
                (TechCategory.Infrastructure,  "INFRA"),
                (TechCategory.AISafety,        "AI SAFETY"),
            };

            foreach (var (cat, label) in categories)
            {
                var catCapture = cat;
                var btnGo = CreateGo("Tab_" + cat, tabRow.transform);
                var img = btnGo.AddComponent<Image>();
                img.color = new Color(0.15f, 0.20f, 0.30f);
                var btn = btnGo.AddComponent<Button>();
                btn.onClick.AddListener(() => SelectCategory(catCapture));
                _tabButtons.Add(btn);

                var le = btnGo.AddComponent<LayoutElement>();
                le.minHeight = 30;

                var txtGo = CreateGo("T", btnGo.transform);
                var txt = txtGo.AddComponent<Text>();
                txt.font = _uiFont; txt.fontSize = 12; txt.fontStyle = FontStyle.Bold;
                txt.color = Color.white; txt.text = label;
                txt.alignment = TextAnchor.MiddleCenter;
                FullStretch(txtGo.GetComponent<RectTransform>() ?? txtGo.AddComponent<RectTransform>());
            }
        }

        // ── Data refresh ─────────────────────────────────────────────────────
        private void RefreshAll()
        {
            RefreshAIStats();
            SelectCategory(_selectedCategory);
            RefreshActiveResearch();
            ShowStatus("");
        }

        private void RefreshAIStats()
        {
            if (_aiStatsText == null || AICore.Instance == null) return;
            var c = AICore.Instance;
            _aiStatsText.text =
                $"Quality:{c.Quality}  Speed:{c.Speed}  Reasoning:{c.Reasoning}  " +
                $"Creativity:{c.Creativity}  Reliability:{c.Reliability}  Learning:{c.Learning}  " +
                $"| Compute:{c.CurrentComputeCapacity}  Energy:{c.CurrentEnergyCapacity}";
        }

        private void SelectCategory(TechCategory cat)
        {
            _selectedCategory = cat;
            _selectedTech = null;
            RefreshTabColors();
            RebuildTechList();
            ClearDetailPanel();
        }

        private void RefreshTabColors()
        {
            var cats = (TechCategory[])System.Enum.GetValues(typeof(TechCategory));
            for (int i = 0; i < _tabButtons.Count && i < cats.Length; i++)
            {
                bool sel = cats[i] == _selectedCategory;
                var img = _tabButtons[i].GetComponent<Image>();
                if (img != null) img.color = sel
                    ? new Color(0.0f, 0.55f, 0.75f)
                    : new Color(0.15f, 0.20f, 0.30f);
            }
        }

        private void RebuildTechList()
        {
            if (_techListContainer == null) return;
            foreach (var c in _techCards) if (c != null) DestroyImmediate(c);
            _techCards.Clear();

            if (TechnologyDatabase.Instance == null) return;
            var techs = TechnologyDatabase.Instance.GetByCategory(_selectedCategory);
            foreach (var tech in techs)
                _techCards.Add(BuildTechCard(tech));
        }

        private GameObject BuildTechCard(TechnologyData tech)
        {
            var card = CreateGo("Card_" + tech.Id, _techListContainer.transform);
            var img = card.AddComponent<Image>();
            Color cardColor = tech.State switch
            {
                TechState.Completed   => new Color(0.08f, 0.22f, 0.10f),
                TechState.Researching => new Color(0.08f, 0.16f, 0.25f),
                TechState.Available   => new Color(0.12f, 0.14f, 0.22f),
                _                    => new Color(0.07f, 0.08f, 0.12f),
            };
            img.color = cardColor;

            var le = card.AddComponent<LayoutElement>();
            le.minHeight = 52; le.preferredHeight = 52;

            // State badge
            string badge = tech.State switch
            {
                TechState.Completed   => "[DONE]",
                TechState.Researching => "[RESEARCHING]",
                TechState.Available   => "[AVAILABLE]",
                _                    => "[LOCKED]",
            };
            Color badgeColor = tech.State switch
            {
                TechState.Completed   => new Color(0.3f, 1f, 0.5f),
                TechState.Researching => new Color(0.3f, 0.8f, 1f),
                TechState.Available   => new Color(1f, 0.85f, 0.3f),
                _                    => new Color(0.5f, 0.5f, 0.5f),
            };

            var nameTxt = MakeText("Name", card.transform, $"{badge}  {tech.Name}", 14,
                tech.State == TechState.Available ? FontStyle.Bold : FontStyle.Normal,
                badgeColor, TextAnchor.MiddleLeft);
            SetAnchors(nameTxt.GetComponent<RectTransform>(), 0f, 0.5f, 0.75f, 1f, 6, 2, -2, -2);

            var costTxt = MakeText("Cost", card.transform,
                $"${tech.MoneyCost:N0}  ·  {tech.ResearchTimeDays}d", 12,
                FontStyle.Normal, new Color(0.8f, 0.75f, 0.6f), TextAnchor.MiddleLeft);
            SetAnchors(costTxt.GetComponent<RectTransform>(), 0f, 0f, 0.75f, 0.5f, 6, 2, -2, -2);

            // Click to select (only clickable if not locked/completed)
            if (tech.State == TechState.Available || tech.State == TechState.Researching)
            {
                var btn = card.AddComponent<Button>();
                var techCapture = tech;
                btn.onClick.AddListener(() => SelectTech(techCapture));
            }

            return card;
        }

        private void SelectTech(TechnologyData tech)
        {
            _selectedTech = tech;
            BuildDetailPanel(tech);
        }

        private void BuildDetailPanel(TechnologyData tech)
        {
            if (_detailPanel == null) return;
            // Clear old children
            for (int i = _detailPanel.transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(_detailPanel.transform.GetChild(i).gameObject);

            float y = -10f;
            float lineH = 22f;

            void AddLine(string t, Color c, FontStyle fs = FontStyle.Normal, int sz = 14)
            {
                var go = CreateGo("L", _detailPanel.transform);
                var txt = go.AddComponent<Text>();
                txt.font = _uiFont; txt.fontSize = sz; txt.fontStyle = fs;
                txt.color = c; txt.text = t; txt.alignment = TextAnchor.UpperLeft;
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(8f, y);
                rt.sizeDelta = new Vector2(-16f, lineH);
                y -= lineH + 2f;
            }

            AddLine(tech.Name, new Color(0f, 0.9f, 1f), FontStyle.Bold, 16);
            y -= 4f;
            AddLine(tech.Description, Color.white, FontStyle.Normal, 13);
            y -= 6f;
            AddLine($"Research: {tech.ResearchTimeDays} game-days", new Color(1f, 0.85f, 0.3f));
            AddLine($"Cost: ${tech.MoneyCost:N0}", new Color(0.3f, 1f, 0.5f));
            if (tech.ComputeRequired > 0)
                AddLine($"Compute: {tech.ComputeRequired}", new Color(0.5f, 0.8f, 1f));
            if (tech.EnergyRequired > 0)
                AddLine($"Energy: {tech.EnergyRequired}", new Color(1f, 0.6f, 0.2f));
            if (tech.MinEmployees > 0)
                AddLine($"Min Employees: {tech.MinEmployees}", Color.white);

            if (tech.PrerequisiteIds.Count > 0)
            {
                y -= 4f;
                AddLine("Requires:", new Color(0.9f, 0.7f, 0.4f), FontStyle.Bold);
                foreach (var pid in tech.PrerequisiteIds)
                {
                    var prereq = TechnologyDatabase.Instance?.GetById(pid);
                    string pname = prereq?.Name ?? pid;
                    bool done = prereq?.IsCompleted ?? false;
                    AddLine($"  • {pname}", done ? new Color(0.4f, 0.9f, 0.4f) : new Color(0.9f, 0.4f, 0.4f));
                }
            }

            // START RESEARCH button (only if available)
            if (tech.State == TechState.Available &&
                TechnologyResearchManager.Instance?.ActiveResearch == null)
            {
                var techCapture = tech;
                MakeButton("START RESEARCH", _detailPanel.transform,
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, 30f), new Vector2(180f, 40f),
                    new Color(0.1f, 0.5f, 0.25f),
                    () =>
                    {
                        if (TechnologyResearchManager.Instance != null)
                        {
                            bool ok = TechnologyResearchManager.Instance.TryStartResearch(techCapture);
                            if (ok) { RefreshAll(); }
                        }
                    });
            }
            else if (tech.State == TechState.Researching)
            {
                AddLine("[CURRENTLY RESEARCHING]", new Color(0.3f, 0.8f, 1f), FontStyle.Bold);
            }
            else if (tech.State == TechState.Completed)
            {
                AddLine("[RESEARCH COMPLETE]", new Color(0.3f, 1f, 0.5f), FontStyle.Bold);
            }
            else if (TechnologyResearchManager.Instance?.ActiveResearch != null)
            {
                AddLine("Another research is active.", new Color(1f, 0.5f, 0.3f));
            }
        }

        private void ClearDetailPanel()
        {
            if (_detailPanel == null) return;
            for (int i = _detailPanel.transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(_detailPanel.transform.GetChild(i).gameObject);
        }

        private void RefreshActiveResearch()
        {
            if (_activeResearchText == null) return;
            var rm = TechnologyResearchManager.Instance;
            if (rm == null || rm.ActiveResearch == null)
            {
                _activeResearchText.text = "ACTIVE RESEARCH:  None";
                return;
            }
            var t = rm.ActiveResearch;
            int pct = Mathf.RoundToInt(rm.ActiveProgress * 100f);
            int workers = rm.GetWorkingEmployeeCount();
            float estDays = rm.GetEstimatedRemainingDays();
            string bar = ProgressBar(rm.ActiveProgress, 20);
            _activeResearchText.text =
                $"RESEARCHING: {t.Name}   {bar} {pct}%   " +
                $"Team: {workers} employees   Est: {estDays:F1} days remaining";
        }

        private string ProgressBar(float f, int width)
        {
            int filled = Mathf.RoundToInt(f * width);
            return "[" + new string('█', filled) + new string('░', width - filled) + "]";
        }

        private void ShowStatus(string msg)
        {
            if (_statusMessageText != null) _statusMessageText.text = msg;
        }

        // ── UI Helpers ───────────────────────────────────────────────────────
        private Text MakeText(string name, Transform parent, string txt, int size, FontStyle style,
            Color color, TextAnchor anchor)
        {
            var go = CreateGo(name, parent);
            go.AddComponent<RectTransform>();
            var t = go.AddComponent<Text>();
            t.font = _uiFont; t.fontSize = size; t.fontStyle = style;
            t.color = color; t.text = txt; t.alignment = anchor;
            t.raycastTarget = false;
            return t;
        }

        private void MakeButton(string label, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 apos, Vector2 size, Color bgColor, UnityEngine.Events.UnityAction onClick)
        {
            var go = CreateGo("Btn_" + label, parent);
            go.AddComponent<Image>().color = bgColor;
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(onClick);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.pivot = pivot;
            rt.anchoredPosition = apos; rt.sizeDelta = size;

            var tgo = CreateGo("T", go.transform);
            tgo.AddComponent<RectTransform>();
            FullStretch(tgo.GetComponent<RectTransform>());
            var t = tgo.AddComponent<Text>();
            t.font = _uiFont; t.fontSize = 14; t.fontStyle = FontStyle.Bold;
            t.color = Color.white; t.text = label; t.alignment = TextAnchor.MiddleCenter;
        }

        private static GameObject CreateGo(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private static void FullStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private static void SetAnchors(RectTransform rt,
            float axMin, float ayMin, float axMax, float ayMax,
            float oxMin, float oyMin, float oxMax, float oyMax)
        {
            rt.anchorMin = new Vector2(axMin, ayMin); rt.anchorMax = new Vector2(axMax, ayMax);
            rt.offsetMin = new Vector2(oxMin, oyMin); rt.offsetMax = new Vector2(oxMax, oyMax);
        }
    }
}
