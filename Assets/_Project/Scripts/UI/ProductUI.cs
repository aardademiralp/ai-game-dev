using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using GameDevStudio.Products;
using GameDevStudio.AI;
using GameDevStudio.Economy;

namespace GameDevStudio.UI
{
    /// <summary>
    /// Product Development UI — toggled with P key.
    /// Shows product catalog, unlock statuses, product details, and active development progress.
    /// </summary>
    public class ProductUI : MonoBehaviour
    {
        public static ProductUI Instance { get; private set; }
        public bool IsOpen => _isOpen;

        // ── UI References ───────────────────────────────────────────────────
        private GameObject _panel;
        private Text       _aiStatsText;
        private Text       _activeDevText;
        private Text       _statusMessageText;
        private GameObject _productListContainer;
        private GameObject _detailPanel;
        private Font       _uiFont;

        private ProductType?             _selectedCategory = null; // null = ALL
        private ProductData              _selectedProduct;
        private readonly List<GameObject> _productCards = new List<GameObject>();
        private readonly List<Button>     _tabButtons   = new List<Button>();

        // ── Input ────────────────────────────────────────────────────────────
        private InputAction _keyP;
        private InputAction _keyEsc;
        private bool        _isOpen;

        // ── Handlers ─────────────────────────────────────────────────────────
        private System.Action<ProductData, float> _onProgressHandler;
        private System.Action<ProductHistoryEntry> _onCompletedHandler;
        private System.Action<string>              _onFailedHandler;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                   ?? Resources.GetBuiltinResource<Font>("Arial.ttf")
                   ?? Font.CreateDynamicFontFromOSFont("Arial", 24);

            _keyP   = new InputAction("ProductUI_P",   InputActionType.Button, "<Keyboard>/p");
            _keyEsc = new InputAction("ProductUI_Esc", InputActionType.Button, "<Keyboard>/escape");
            _keyP.Enable();
            _keyEsc.Enable();

            _onProgressHandler  = (prod, prog) => { if (_isOpen) RefreshActiveDevPanel(); };
            _onCompletedHandler = (entry)      => { if (_isOpen) { RefreshAll(); ShowStatus($"✓ {entry.ProductName} COMPLETED!"); } };
            _onFailedHandler    = (msg)        => { if (_isOpen) ShowStatus(msg); };

            BuildUI();
        }

        private void Start()
        {
            if (ProductDevelopmentManager.Instance != null)
            {
                ProductDevelopmentManager.Instance.OnProductProgressUpdated += _onProgressHandler;
                ProductDevelopmentManager.Instance.OnProductCompleted       += _onCompletedHandler;
                ProductDevelopmentManager.Instance.OnProductFailed          += _onFailedHandler;
            }
        }

        private void OnDestroy()
        {
            _keyP?.Dispose();
            _keyEsc?.Dispose();
            if (ProductDevelopmentManager.Instance != null)
            {
                ProductDevelopmentManager.Instance.OnProductProgressUpdated -= _onProgressHandler;
                ProductDevelopmentManager.Instance.OnProductCompleted       -= _onCompletedHandler;
                ProductDevelopmentManager.Instance.OnProductFailed          -= _onFailedHandler;
            }
        }

        private void Update()
        {
            if (!Flow.GameStateManager.IsGameplayActive)
            {
                if (_isOpen) CloseWindow();
                return;
            }

            if (_keyP != null && _keyP.WasPressedThisFrame())
            {
                if (!Office.FurniturePlacer.IsPlacing)
                {
                    if (RecruitmentUI.Instance != null && RecruitmentUI.Instance.IsOpen)
                        RecruitmentUI.Instance.CloseWindow();
                    if (OfficeExpansionUI.Instance != null && OfficeExpansionUI.Instance.IsOpen)
                        OfficeExpansionUI.Instance.CloseWindow();
                    if (ResearchUI.Instance != null && ResearchUI.Instance.IsOpen)
                        ResearchUI.Instance.CloseWindow();

                    ToggleWindow();
                }
            }
            if (_isOpen && _keyEsc != null && _keyEsc.WasPressedThisFrame())
                CloseWindow();

            if (_isOpen && ProductDevelopmentManager.Instance?.ActiveProduct != null)
                RefreshActiveDevPanel();
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

        // ── UI Construction ──────────────────────────────────────────────────
        private void BuildUI()
        {
            Transform old = transform.Find("ProductCanvas");
            if (old != null) DestroyImmediate(old.gameObject);

            var canvasGo = new GameObject("ProductCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 210;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            _panel = CreateGo("ProductWindow", canvasGo.transform);
            var bg = _panel.AddComponent<Image>();
            bg.color = new Color(0.02f, 0.05f, 0.08f, 0.94f);
            FullStretch(_panel.GetComponent<RectTransform>());

            var box = CreateGo("Box", _panel.transform);
            box.AddComponent<Image>().color = new Color(0.06f, 0.09f, 0.14f, 0.99f);
            var boxRt = box.GetComponent<RectTransform>();
            boxRt.anchorMin = new Vector2(0.03f, 0.02f);
            boxRt.anchorMax = new Vector2(0.97f, 0.98f);
            boxRt.offsetMin = boxRt.offsetMax = Vector2.zero;

            // Title
            var titleTxt = MakeText("Title", box.transform, "PRODUCT DEVELOPMENT", 26, FontStyle.Bold,
                new Color(0f, 0.85f, 1f), TextAnchor.MiddleCenter);
            SetAnchors(titleTxt.GetComponent<RectTransform>(), 0f, 0.93f, 1f, 1f, 0, -5f, 0, -5f);

            // AI Stats bar
            _aiStatsText = MakeText("AIStats", box.transform, "", 15, FontStyle.Normal,
                new Color(1f, 0.85f, 0.3f), TextAnchor.MiddleCenter);
            SetAnchors(_aiStatsText.GetComponent<RectTransform>(), 0f, 0.86f, 1f, 0.93f, 5, 0, -5, 0);

            // Tabs
            BuildCategoryTabs(box.transform);

            // Left List area
            var listBg = CreateGo("ListBg", box.transform);
            listBg.AddComponent<Image>().color = new Color(0.04f, 0.06f, 0.10f, 0.7f);
            SetAnchors(listBg.GetComponent<RectTransform>(), 0f, 0.18f, 0.6f, 0.84f, 5, 5, -5, -5);

            _productListContainer = CreateGo("ListContent", listBg.transform);
            FullStretch(_productListContainer.GetComponent<RectTransform>() ?? _productListContainer.AddComponent<RectTransform>());
            var vlg = _productListContainer.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6; vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childControlWidth = true; vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;

            // Right Detail Panel
            _detailPanel = CreateGo("DetailPanel", box.transform);
            _detailPanel.AddComponent<Image>().color = new Color(0.05f, 0.08f, 0.13f, 0.9f);
            SetAnchors(_detailPanel.GetComponent<RectTransform>(), 0.62f, 0.18f, 1f, 0.84f, 5, 5, -5, -5);

            // Bottom Active Dev Panel
            var activeBg = CreateGo("ActiveBg", box.transform);
            activeBg.AddComponent<Image>().color = new Color(0.03f, 0.08f, 0.06f, 0.95f);
            SetAnchors(activeBg.GetComponent<RectTransform>(), 0f, 0.05f, 1f, 0.18f, 5, 5, -5, -5);

            _activeDevText = MakeText("ActiveText", activeBg.transform, "No active product development.", 16,
                FontStyle.Normal, new Color(0.3f, 0.9f, 0.5f), TextAnchor.MiddleLeft);
            SetAnchors(_activeDevText.GetComponent<RectTransform>(), 0f, 0f, 1f, 1f, 10, 5, -10, -5);

            // Status msg
            _statusMessageText = MakeText("StatusMsg", box.transform, "", 15, FontStyle.Bold,
                new Color(1f, 0.5f, 0.3f), TextAnchor.MiddleCenter);
            SetAnchors(_statusMessageText.GetComponent<RectTransform>(), 0f, 0f, 1f, 0.05f, 5, 0, -5, 0);

            // Close button
            MakeButton("CLOSE (Esc/P)", box.transform,
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

            var categories = new (ProductType? type, string label)[]
            {
                (null,                   "ALL"),
                (ProductType.Game,        "GAMES"),
                (ProductType.Software,    "SOFTWARE"),
                (ProductType.Website,     "WEBSITES"),
                (ProductType.Application, "APPS"),
                (ProductType.AITool,      "AI TOOLS"),
            };

            foreach (var (type, label) in categories)
            {
                var typeCapture = type;
                var btnGo = CreateGo("Tab_" + label, tabRow.transform);
                var img = btnGo.AddComponent<Image>();
                img.color = new Color(0.12f, 0.18f, 0.28f);
                var btn = btnGo.AddComponent<Button>();
                btn.onClick.AddListener(() => SelectCategory(typeCapture));
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

        // ── Data Refresh ─────────────────────────────────────────────────────
        private void RefreshAll()
        {
            RefreshAIStats();
            SelectCategory(_selectedCategory);
            RefreshActiveDevPanel();
            ShowStatus("");
        }

        private void RefreshAIStats()
        {
            if (_aiStatsText == null || AICore.Instance == null) return;
            var c = AICore.Instance;
            _aiStatsText.text =
                $"Quality:{c.Quality}  Speed:{c.Speed}  Reasoning:{c.Reasoning}  " +
                $"Creativity:{c.Creativity}  Reliability:{c.Reliability}  Learning:{c.Learning}  " +
                $"| Compute Capacity:{c.CurrentComputeCapacity}  Energy Capacity:{c.CurrentEnergyCapacity}";
        }

        private void SelectCategory(ProductType? type)
        {
            _selectedCategory = type;
            _selectedProduct  = null;
            RefreshTabColors();
            RebuildProductList();
            ClearDetailPanel();
        }

        private void RefreshTabColors()
        {
            var categories = new ProductType?[] { null, ProductType.Game, ProductType.Software, ProductType.Website, ProductType.Application, ProductType.AITool };
            for (int i = 0; i < _tabButtons.Count && i < categories.Length; i++)
            {
                bool sel = categories[i] == _selectedCategory;
                var img = _tabButtons[i].GetComponent<Image>();
                if (img != null) img.color = sel ? new Color(0.0f, 0.55f, 0.75f) : new Color(0.12f, 0.18f, 0.28f);
            }
        }

        private void RebuildProductList()
        {
            if (_productListContainer == null) return;
            foreach (var c in _productCards) if (c != null) DestroyImmediate(c);
            _productCards.Clear();

            if (ProductDatabase.Instance == null) return;

            var products = _selectedCategory.HasValue
                ? ProductDatabase.Instance.GetByType(_selectedCategory.Value)
                : new List<ProductData>(ProductDatabase.Instance.AllProducts);

            foreach (var prod in products)
                _productCards.Add(BuildProductCard(prod));
        }

        private GameObject BuildProductCard(ProductData product)
        {
            var card = CreateGo("Card_" + product.Id, _productListContainer.transform);
            var img = card.AddComponent<Image>();

            bool unlocked = ProductDatabase.Instance != null && ProductDatabase.Instance.IsProductUnlocked(product);
            bool isDeveloping = ProductDevelopmentManager.Instance?.ActiveProduct?.Id == product.Id;

            Color cardColor = isDeveloping
                ? new Color(0.08f, 0.18f, 0.28f)
                : (unlocked ? new Color(0.10f, 0.14f, 0.22f) : new Color(0.06f, 0.07f, 0.11f));
            img.color = cardColor;

            var le = card.AddComponent<LayoutElement>();
            le.minHeight = 52; le.preferredHeight = 52;

            string badge = isDeveloping ? "[DEVELOPING]" : (unlocked ? "[AVAILABLE]" : "[LOCKED]");
            Color badgeColor = isDeveloping ? new Color(0.3f, 0.8f, 1f) : (unlocked ? new Color(0.3f, 1f, 0.5f) : new Color(0.5f, 0.5f, 0.5f));

            var nameTxt = MakeText("Name", card.transform, $"{badge}  {product.productName}", 14,
                unlocked ? FontStyle.Bold : FontStyle.Normal, badgeColor, TextAnchor.MiddleLeft);
            SetAnchors(nameTxt.GetComponent<RectTransform>(), 0f, 0.5f, 0.75f, 1f, 6, 2, -2, -2);

            var costTxt = MakeText("Cost", card.transform,
                $"Type: {product.productType}  ·  Cost: ${product.baseCost:N0}  ·  Time: {product.baseDevelopmentDays}d", 12,
                FontStyle.Normal, new Color(0.8f, 0.75f, 0.6f), TextAnchor.MiddleLeft);
            SetAnchors(costTxt.GetComponent<RectTransform>(), 0f, 0f, 0.75f, 0.5f, 6, 2, -2, -2);

            var btn = card.AddComponent<Button>();
            var prodCapture = product;
            btn.onClick.AddListener(() => SelectProduct(prodCapture));

            return card;
        }

        private void SelectProduct(ProductData product)
        {
            _selectedProduct = product;
            BuildDetailPanel(product);
        }

        private void BuildDetailPanel(ProductData product)
        {
            if (_detailPanel == null) return;
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

            AddLine(product.productName, new Color(0f, 0.85f, 1f), FontStyle.Bold, 16);
            y -= 4f;
            AddLine(product.description, Color.white, FontStyle.Normal, 13);
            y -= 6f;
            AddLine($"Type: {product.productType}", new Color(1f, 0.85f, 0.3f));
            AddLine($"Development Cost: ${product.baseCost:N0}", new Color(0.3f, 1f, 0.5f));
            AddLine($"Est. Dev Time: {product.baseDevelopmentDays} game-days", new Color(0.8f, 0.8f, 0.8f));
            AddLine($"Min Compute Required: {product.minComputeRequired} (Usage: {product.computeUsage})", new Color(0.5f, 0.8f, 1f));
            AddLine($"Min Energy Required: {product.minEnergyRequired} (Usage: {product.energyUsage})", new Color(1f, 0.6f, 0.2f));
            AddLine($"Min Employees Required: {product.minEmployeesRequired}", Color.white);

            bool unlocked = ProductDatabase.Instance != null && ProductDatabase.Instance.IsProductUnlocked(product);

            if (product.requiredTechIds != null && product.requiredTechIds.Length > 0)
            {
                y -= 4f;
                AddLine("Required Technologies:", new Color(0.9f, 0.7f, 0.4f), FontStyle.Bold);
                foreach (var techId in product.requiredTechIds)
                {
                    var tech = TechnologyDatabase.Instance?.GetById(techId);
                    string tName = tech?.Name ?? techId;
                    bool done = tech?.IsCompleted ?? false;
                    AddLine($"  • {tName}", done ? new Color(0.4f, 0.9f, 0.4f) : new Color(0.9f, 0.4f, 0.4f));
                }
            }

            var pdm = ProductDevelopmentManager.Instance;

            if (pdm != null && pdm.ActiveProduct?.Id == product.Id)
            {
                AddLine("[CURRENTLY DEVELOPING]", new Color(0.3f, 0.8f, 1f), FontStyle.Bold);
            }
            else if (unlocked && (pdm == null || pdm.DevelopmentState != ProductDevelopmentState.Developing))
            {
                var prodCapture = product;
                MakeButton("START DEVELOPMENT", _detailPanel.transform,
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, 30f), new Vector2(200f, 40f),
                    new Color(0.1f, 0.5f, 0.25f),
                    () =>
                    {
                        if (ProductDevelopmentManager.Instance != null)
                        {
                            bool ok = ProductDevelopmentManager.Instance.TryStartDevelopment(prodCapture);
                            if (ok) RefreshAll();
                        }
                    });
            }
            else if (!unlocked)
            {
                AddLine("[LOCKED - Research prerequisites first]", new Color(0.9f, 0.4f, 0.4f), FontStyle.Bold);
            }
        }

        private void ClearDetailPanel()
        {
            if (_detailPanel == null) return;
            for (int i = _detailPanel.transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(_detailPanel.transform.GetChild(i).gameObject);
        }

        private void RefreshActiveDevPanel()
        {
            if (_activeDevText == null) return;
            var pdm = ProductDevelopmentManager.Instance;
            if (pdm == null || pdm.ActiveProduct == null)
            {
                _activeDevText.text = "ACTIVE PRODUCT DEVELOPMENT: None";
                return;
            }

            var p = pdm.ActiveProduct;
            int pct = Mathf.RoundToInt(pdm.ActiveProgress * 100f);
            int workers = pdm.GetWorkingEmployeeCount();
            float estDays = pdm.GetEstimatedRemainingDays();
            string bar = ProgressBar(pdm.ActiveProgress, 20);

            _activeDevText.text =
                $"DEVELOPING: {p.productName} [{p.productType}]   {bar} {pct}%   " +
                $"Team: {workers} workers   Compute: {p.computeUsage}   Energy: {p.energyUsage}   Est: {estDays:F1} days left";
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

        // ── Helpers ──────────────────────────────────────────────────────────
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
