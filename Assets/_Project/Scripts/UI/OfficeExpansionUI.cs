using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using GameDevStudio.Office;
using GameDevStudio.Economy;

namespace GameDevStudio.UI
{
    /// <summary>
    /// Office Expansion UI (toggled with E key).
    /// Displays current office level, capacity, next upgrade info, current money, and EXPAND button.
    /// </summary>
    public class OfficeExpansionUI : MonoBehaviour
    {
        public static OfficeExpansionUI Instance { get; private set; }

        public bool IsOpen => _isOpen;

        private GameObject _panel;
        private Text       _currentInfoText;
        private Text       _nextInfoText;
        private Text       _moneyText;
        private Button     _expandButton;
        private Text       _expandBtnText;
        private Font       _uiFont;

        private InputAction _keyE;
        private InputAction _keyEsc;
        private bool        _isOpen = false;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ??
                      Resources.GetBuiltinResource<Font>("Arial.ttf") ??
                      Font.CreateDynamicFontFromOSFont("Arial", 24);

            _keyE   = new InputAction("Expansion_E",   InputActionType.Button, "<Keyboard>/e");
            _keyEsc = new InputAction("Expansion_Esc", InputActionType.Button, "<Keyboard>/escape");
            _keyE.Enable();
            _keyEsc.Enable();

            BuildUI();
        }

        private void Start()
        {
            if (OfficeManager.Instance != null)
                OfficeManager.Instance.OnOfficeExpanded += RefreshUI;

            if (MoneyManager.Instance != null)
                MoneyManager.Instance.OnMoneyChanged += OnMoneyChanged;
        }

        // Named handler so it can be safely unsubscribed in OnDestroy.
        private void OnMoneyChanged(int _) => RefreshUI();

        private void OnDestroy()
        {
            _keyE?.Dispose();
            _keyEsc?.Dispose();

            if (OfficeManager.Instance != null)
                OfficeManager.Instance.OnOfficeExpanded -= RefreshUI;

            if (MoneyManager.Instance != null)
                MoneyManager.Instance.OnMoneyChanged -= OnMoneyChanged;
        }

        private void Update()
        {
            // All gameplay hotkeys blocked outside an active session
            if (!Flow.GameStateManager.IsGameplayActive)
            {
                if (_isOpen) CloseWindow();
                return;
            }

            if (_keyE != null && _keyE.WasPressedThisFrame())
            {
                if (!FurniturePlacer.IsPlacing)
                {
                    // Close other mutually-exclusive panels before toggling this one.
                    if (RecruitmentUI.Instance != null && RecruitmentUI.Instance.IsOpen)
                        RecruitmentUI.Instance.CloseWindow();

                    if (ResearchUI.Instance != null && ResearchUI.Instance.IsOpen)
                        ResearchUI.Instance.CloseWindow();

                    ToggleWindow();
                }
            }

            if (_isOpen && _keyEsc != null && _keyEsc.WasPressedThisFrame())
                CloseWindow();
        }

        public void ToggleWindow()
        {
            if (_isOpen) CloseWindow();
            else OpenWindow();
        }

        public void OpenWindow()
        {
            if (_panel == null) BuildUI();
            _isOpen = true;
            if (_panel != null)
            {
                _panel.SetActive(true);
                RefreshUI();
            }
        }

        public void CloseWindow()
        {
            _isOpen = false;
            if (_panel != null) _panel.SetActive(false);
        }

        // ── UI Construction ─────────────────────────────────────────────
        private void BuildUI()
        {
            Transform existingCanvas = transform.Find("ExpansionCanvas");
            if (existingCanvas != null) DestroyImmediate(existingCanvas.gameObject);

            // Root canvas
            GameObject canvasGo = new GameObject("ExpansionCanvas");
            canvasGo.transform.SetParent(transform, false);

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 210;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode    = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // Full-screen dim panel
            _panel = new GameObject("ExpansionWindow");
            _panel.transform.SetParent(canvasGo.transform, false);

            Image bgImg = _panel.AddComponent<Image>();
            bgImg.color         = new Color(0.04f, 0.05f, 0.08f, 0.88f);
            bgImg.raycastTarget = true;

            RectTransform panelRt = _panel.GetComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            // Centred content box
            GameObject box = new GameObject("ContentBox");
            box.transform.SetParent(_panel.transform, false);
            box.AddComponent<Image>().color = new Color(0.08f, 0.10f, 0.16f, 0.98f);

            RectTransform boxRt = box.GetComponent<RectTransform>();
            boxRt.anchorMin = new Vector2(0.5f, 0.5f);
            boxRt.anchorMax = new Vector2(0.5f, 0.5f);
            boxRt.pivot     = new Vector2(0.5f, 0.5f);
            boxRt.sizeDelta = new Vector2(560f, 430f);

            // ── Title ────────────────────────────────────────────────────
            {
                GameObject go = new GameObject("Title");
                go.transform.SetParent(box.transform, false);
                Text t = go.AddComponent<Text>();
                t.font      = _uiFont;
                t.fontSize  = 26;
                t.fontStyle = FontStyle.Bold;
                t.color     = new Color(0.0f, 0.88f, 1.0f);
                t.text      = "OFFICE EXPANSION";
                t.alignment = TextAnchor.MiddleCenter;

                RectTransform rt = go.GetComponent<RectTransform>();
                rt.anchorMin        = new Vector2(0f, 1f);
                rt.anchorMax        = new Vector2(1f, 1f);
                rt.pivot            = new Vector2(0.5f, 1f);
                rt.sizeDelta        = new Vector2(0f, 45f);
                rt.anchoredPosition = new Vector2(0f, -15f);
            }

            // ── Current Office Info ───────────────────────────────────────
            {
                GameObject go = new GameObject("CurrentInfo");
                go.transform.SetParent(box.transform, false);
                _currentInfoText           = go.AddComponent<Text>();
                _currentInfoText.font      = _uiFont;
                _currentInfoText.fontSize  = 18;
                _currentInfoText.color     = Color.white;
                _currentInfoText.alignment = TextAnchor.UpperCenter;

                RectTransform rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.05f, 0.62f);
                rt.anchorMax = new Vector2(0.95f, 0.87f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            // ── Next Upgrade Info ─────────────────────────────────────────
            {
                GameObject go = new GameObject("NextInfo");
                go.transform.SetParent(box.transform, false);
                _nextInfoText           = go.AddComponent<Text>();
                _nextInfoText.font      = _uiFont;
                _nextInfoText.fontSize  = 18;
                _nextInfoText.color     = new Color(0.9f, 0.85f, 0.4f);
                _nextInfoText.alignment = TextAnchor.UpperCenter;

                RectTransform rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.05f, 0.35f);
                rt.anchorMax = new Vector2(0.95f, 0.62f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            // ── Current Money Display ─────────────────────────────────────
            {
                GameObject go = new GameObject("MoneyDisplay");
                go.transform.SetParent(box.transform, false);
                _moneyText           = go.AddComponent<Text>();
                _moneyText.font      = _uiFont;
                _moneyText.fontSize  = 17;
                _moneyText.color     = new Color(0.26f, 0.93f, 0.44f);
                _moneyText.alignment = TextAnchor.MiddleCenter;

                RectTransform rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.05f, 0.19f);
                rt.anchorMax = new Vector2(0.95f, 0.34f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            // ── Expand Button ─────────────────────────────────────────────
            {
                GameObject btnGo = new GameObject("Btn_Expand");
                btnGo.transform.SetParent(box.transform, false);
                btnGo.AddComponent<Image>().color = new Color(0.15f, 0.55f, 0.35f);

                _expandButton = btnGo.AddComponent<Button>();
                _expandButton.onClick.AddListener(OnExpandClicked);

                RectTransform rt = btnGo.GetComponent<RectTransform>();
                rt.anchorMin        = new Vector2(0.5f, 0f);
                rt.anchorMax        = new Vector2(0.5f, 0f);
                rt.pivot            = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(-80f, 42f);
                rt.sizeDelta        = new Vector2(210f, 44f);

                GameObject txtGo = new GameObject("Text");
                txtGo.transform.SetParent(btnGo.transform, false);
                _expandBtnText           = txtGo.AddComponent<Text>();
                _expandBtnText.font      = _uiFont;
                _expandBtnText.fontSize  = 16;
                _expandBtnText.fontStyle = FontStyle.Bold;
                _expandBtnText.color     = Color.white;
                _expandBtnText.text      = "EXPAND";
                _expandBtnText.alignment = TextAnchor.MiddleCenter;

                RectTransform txtRt = txtGo.GetComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero;
                txtRt.anchorMax = Vector2.one;
                txtRt.offsetMin = Vector2.zero;
                txtRt.offsetMax = Vector2.zero;
            }

            // ── Close Button ──────────────────────────────────────────────
            {
                GameObject closeGo = new GameObject("Btn_Close");
                closeGo.transform.SetParent(box.transform, false);
                closeGo.AddComponent<Image>().color = new Color(0.35f, 0.15f, 0.15f);

                Button closeBtn = closeGo.AddComponent<Button>();
                closeBtn.onClick.AddListener(CloseWindow);

                RectTransform rt = closeGo.GetComponent<RectTransform>();
                rt.anchorMin        = new Vector2(0.5f, 0f);
                rt.anchorMax        = new Vector2(0.5f, 0f);
                rt.pivot            = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(130f, 42f);
                rt.sizeDelta        = new Vector2(150f, 44f);

                GameObject txtGo = new GameObject("Text");
                txtGo.transform.SetParent(closeGo.transform, false);
                Text closeTxt = txtGo.AddComponent<Text>();
                closeTxt.font      = _uiFont;
                closeTxt.fontSize  = 16;
                closeTxt.fontStyle = FontStyle.Bold;
                closeTxt.color     = Color.white;
                closeTxt.text      = "CLOSE (Esc/E)";
                closeTxt.alignment = TextAnchor.MiddleCenter;

                RectTransform txtRt = txtGo.GetComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero;
                txtRt.anchorMax = Vector2.one;
                txtRt.offsetMin = Vector2.zero;
                txtRt.offsetMax = Vector2.zero;
            }

            _panel.SetActive(false);
        }

        // ── Data Refresh ─────────────────────────────────────────────────
        public void RefreshUI()
        {
            if (OfficeManager.Instance == null) return;

            var mgr = OfficeManager.Instance;

            // Current office section
            if (_currentInfoText != null)
            {
                _currentInfoText.text =
                    $"CURRENT OFFICE: <b>{mgr.CurrentOfficeName}</b> (Level {mgr.CurrentLevelNumber})\n" +
                    $"Staff Capacity: <b>{mgr.CurrentCapacity} Employees</b>";
            }

            // Next upgrade section
            if (_nextInfoText != null)
            {
                if (mgr.HasNextLevel)
                {
                    _nextInfoText.text =
                        $"NEXT UPGRADE: <b>{mgr.NextUpgradeName}</b>\n" +
                        $"New Capacity: <b>{mgr.NextUpgradeCapacity} Employees</b>\n" +
                        $"Cost: <b>${mgr.NextUpgradeCost:N0}</b>";
                }
                else
                {
                    _nextInfoText.text = "<b>MAXIMUM OFFICE LEVEL REACHED!</b>";
                }
            }

            // Current money section
            if (_moneyText != null)
            {
                int money = MoneyManager.Instance != null ? MoneyManager.Instance.CurrentMoney : 0;
                _moneyText.text = $"Your Balance: <b>${money:N0}</b>";
            }

            // Expand button state
            if (_expandButton != null)
            {
                bool canAfford = mgr.CanExpandOffice();
                _expandButton.interactable = mgr.HasNextLevel && canAfford;

                if (_expandBtnText != null)
                {
                    if (!mgr.HasNextLevel)
                        _expandBtnText.text = "MAX LEVEL";
                    else if (!canAfford)
                        _expandBtnText.text = "NEED FUNDS";
                    else
                        _expandBtnText.text = "EXPAND";
                }
            }
        }

        private void OnExpandClicked()
        {
            if (OfficeManager.Instance == null) return;

            bool success = OfficeManager.Instance.TryExpandOffice();
            if (success)
            {
                Debug.Log($"[OfficeExpansionUI] Expansion successful! Now: {OfficeManager.Instance.CurrentOfficeName} (Cap: {OfficeManager.Instance.CurrentCapacity})");
                RefreshUI();
            }
            else
            {
                Debug.LogWarning("[OfficeExpansionUI] Expansion failed — not enough money or already at max level.");
            }
        }
    }
}
