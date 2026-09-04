using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using GameDevStudio.Employees;
using GameDevStudio.Characters;
using GameDevStudio.Office;

namespace GameDevStudio.UI
{
    /// <summary>
    /// Professional uGUI Recruitment overlay panel (toggled with R key or Esc).
    /// Displays 3 candidate cards, AI stats, traits, salary, and [ HIRE ] buttons.
    /// Explicit RectTransform positioning (-330, 0, 330) guarantees visible card rendering.
    /// </summary>
    public class RecruitmentUI : MonoBehaviour
    {
        public static RecruitmentUI Instance { get; private set; }

        public bool IsOpen => _isOpen;

        private GameObject   _panel;
        private Transform    _cardContainer;
        private Text         _capacityText;
        private Font         _uiFont;

        private InputAction  _keyR;
        private InputAction  _keyEsc;
        private bool         _isOpen = false;

        private System.Action               _onPoolRefreshedHandler;
        private System.Action<EmployeeData> _onEmployeeHiredHandler;

        private readonly List<GameObject> _spawnedCards = new List<GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ??
                      Resources.GetBuiltinResource<Font>("Arial.ttf") ??
                      Font.CreateDynamicFontFromOSFont("Arial", 24);

            _keyR   = new InputAction("Recruitment_R", InputActionType.Button, "<Keyboard>/r");
            _keyEsc = new InputAction("Recruitment_Esc", InputActionType.Button, "<Keyboard>/escape");
            _keyR.Enable();
            _keyEsc.Enable();

            _onPoolRefreshedHandler = () => {
                Debug.Log($"[Recruitment TRACE] _onPoolRefreshedHandler invoked. _isOpen = {_isOpen}");
                if (_isOpen) PopulateCandidates();
            };
            _onEmployeeHiredHandler = (data) => {
                Debug.Log($"[Recruitment TRACE] _onEmployeeHiredHandler invoked for {data?.employeeName}. _isOpen = {_isOpen}");
                if (_isOpen) PopulateCandidates();
            };

            Debug.Log("[Recruitment TRACE] Awake: Building UI...");
            BuildUI();
            Debug.Log("[Recruitment TRACE] Awake completed.");
        }

        private void OnDestroy()
        {
            _keyR?.Dispose();
            _keyEsc?.Dispose();

            if (RecruitmentManager.Instance != null)
            {
                if (_onPoolRefreshedHandler != null)
                    RecruitmentManager.Instance.OnCandidatePoolRefreshed -= _onPoolRefreshedHandler;
                if (_onEmployeeHiredHandler != null)
                    RecruitmentManager.Instance.OnEmployeeHired -= _onEmployeeHiredHandler;
            }
        }

        private void Start()
        {
            Debug.Log("[Recruitment TRACE] Start ENTER.");
            if (_cardContainer == null) BuildUI();

            if (RecruitmentManager.Instance != null)
            {
                RecruitmentManager.Instance.OnCandidatePoolRefreshed += _onPoolRefreshedHandler;
                RecruitmentManager.Instance.OnEmployeeHired += _onEmployeeHiredHandler;
                Debug.Log("[Recruitment TRACE] Subscribed to RecruitmentManager events.");
            }
            else
            {
                Debug.LogWarning("[Recruitment TRACE] Start: RecruitmentManager.Instance is NULL during subscription!");
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

            if (_keyR != null && _keyR.WasPressedThisFrame())
            {
                Debug.Log($"[Recruitment TRACE] R key pressed! IsPlacing = {Office.FurniturePlacer.IsPlacing}");
                if (!Office.FurniturePlacer.IsPlacing)
                {
                    if (ResearchUI.Instance != null && ResearchUI.Instance.IsOpen)
                        ResearchUI.Instance.CloseWindow();

                    ToggleWindow();
                }
            }

            if (_isOpen && _keyEsc != null && _keyEsc.WasPressedThisFrame())
            {
                Debug.Log("[Recruitment TRACE] ESC key pressed while recruitment is open. Closing window.");
                CloseWindow();
            }
        }

        public void ToggleWindow()
        {
            Debug.Log($"[Recruitment TRACE] ToggleWindow called. Current _isOpen = {_isOpen}");
            if (_isOpen) CloseWindow();
            else OpenWindow();
        }

        public void OpenWindow()
        {
            Debug.Log($"[Recruitment TRACE] OpenWindow ENTER. _isOpen set to true. _cardContainer null? {(_cardContainer == null)}");
            if (_cardContainer == null || _panel == null) BuildUI();

            _isOpen = true;
            if (_panel != null)
            {
                _panel.SetActive(true);
                Debug.Log($"[Recruitment TRACE] _panel set active: {_panel.activeSelf}. Calling PopulateCandidates()...");
                PopulateCandidates();
            }
            else
            {
                Debug.LogError("[Recruitment TRACE] OpenWindow: _panel is NULL! UI build failed!");
            }
        }

        public void CloseWindow()
        {
            Debug.Log($"[Recruitment TRACE] CloseWindow ENTER. _isOpen set to false. _panel is null? {(_panel == null)}");
            _isOpen = false;
            if (_panel != null)
            {
                _panel.SetActive(false);
            }
        }

        private void BuildUI()
        {
            Transform existingCanvas = transform.Find("RecruitmentCanvas");
            if (existingCanvas != null) DestroyImmediate(existingCanvas.gameObject);

            GameObject canvasGo = new GameObject("RecruitmentCanvas");
            canvasGo.transform.SetParent(transform, false);

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // Background Dim Panel
            _panel = new GameObject("RecruitmentWindow");
            _panel.transform.SetParent(canvasGo.transform, false);

            Image bgImg = _panel.AddComponent<Image>();
            bgImg.color = new Color(0.04f, 0.05f, 0.08f, 0.88f);
            bgImg.raycastTarget = true;

            RectTransform panelRt = _panel.GetComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            // Main Content Box
            GameObject box = new GameObject("ContentBox");
            box.transform.SetParent(_panel.transform, false);
            Image boxImg = box.AddComponent<Image>();
            boxImg.color = new Color(0.08f, 0.10f, 0.16f, 0.98f);
            boxImg.raycastTarget = true;

            RectTransform boxRt = box.GetComponent<RectTransform>();
            boxRt.anchorMin = new Vector2(0.5f, 0.5f);
            boxRt.anchorMax = new Vector2(0.5f, 0.5f);
            boxRt.pivot = new Vector2(0.5f, 0.5f);
            boxRt.sizeDelta = new Vector2(1100f, 680f);

            // Title Text
            GameObject titleGo = new GameObject("Title");
            titleGo.transform.SetParent(box.transform, false);
            Text titleTxt = titleGo.AddComponent<Text>();
            titleTxt.font = _uiFont;
            titleTxt.fontSize = 28;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.color = new Color(0.0f, 0.88f, 1.0f);
            titleTxt.text = "AI RESEARCH RECRUITMENT";
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.raycastTarget = false;

            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.sizeDelta = new Vector2(0f, 45f);
            titleRt.anchoredPosition = new Vector2(0f, -15f);

            // Staff Capacity Subtitle
            GameObject capGo = new GameObject("CapacityText");
            capGo.transform.SetParent(box.transform, false);
            _capacityText = capGo.AddComponent<Text>();
            _capacityText.font = _uiFont;
            _capacityText.fontSize = 18;
            _capacityText.color = new Color(0.80f, 0.85f, 0.90f);
            _capacityText.alignment = TextAnchor.MiddleCenter;
            _capacityText.raycastTarget = false;

            RectTransform capRt = capGo.GetComponent<RectTransform>();
            capRt.anchorMin = new Vector2(0f, 1f);
            capRt.anchorMax = new Vector2(1f, 1f);
            capRt.pivot = new Vector2(0.5f, 1f);
            capRt.sizeDelta = new Vector2(0f, 30f);
            capRt.anchoredPosition = new Vector2(0f, -60f);

            // Candidate Cards Container
            GameObject cardContainerGo = new GameObject("CardContainer");
            cardContainerGo.transform.SetParent(box.transform, false);
            _cardContainer = cardContainerGo.transform;

            Image cardBgImg = cardContainerGo.AddComponent<Image>();
            cardBgImg.color = new Color(0.05f, 0.07f, 0.12f, 0.60f);
            cardBgImg.raycastTarget = false;

            RectTransform cardContainerRt = cardContainerGo.GetComponent<RectTransform>();
            cardContainerRt.anchorMin = new Vector2(0.02f, 0.14f);
            cardContainerRt.anchorMax = new Vector2(0.98f, 0.85f);
            cardContainerRt.offsetMin = Vector2.zero;
            cardContainerRt.offsetMax = Vector2.zero;

            // Bottom Buttons (Close & Refresh)
            CreateButton(box.transform, "CLOSE (Esc/R)", new Vector2(-160f, 32f), new Vector2(200f, 44f), () => {
                Debug.Log("[UI] Recruitment Close clicked");
                CloseWindow();
            });

            CreateButton(box.transform, "REFRESH CANDIDATES", new Vector2(160f, 32f), new Vector2(250f, 44f), () => {
                Debug.Log("[UI] Recruitment Refresh clicked");
                if (RecruitmentManager.Instance != null) RecruitmentManager.Instance.RefreshCandidates();
            });

            _panel.SetActive(false);
        }

        private GameObject CreateButton(Transform parent, string label, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnGo = new GameObject("Btn_" + label);
            btnGo.transform.SetParent(parent, false);

            Image img = btnGo.AddComponent<Image>();
            img.color = new Color(0.18f, 0.24f, 0.36f);
            img.raycastTarget = true;

            Button btn = btnGo.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = new Color(0.18f, 0.24f, 0.36f);
            cb.highlightedColor = new Color(0.28f, 0.38f, 0.58f);
            cb.pressedColor = new Color(0.12f, 0.16f, 0.24f);
            btn.colors = cb;
            btn.onClick.AddListener(onClick);

            RectTransform rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            GameObject txtGo = new GameObject("Text");
            txtGo.transform.SetParent(btnGo.transform, false);
            Text txt = txtGo.AddComponent<Text>();
            txt.font = _uiFont;
            txt.fontSize = 16;
            txt.fontStyle = FontStyle.Bold;
            txt.color = Color.white;
            txt.text = label;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.raycastTarget = false;

            RectTransform txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            return btnGo;
        }

        private void PopulateCandidates()
        {
            if (_cardContainer == null && _panel != null)
            {
                _cardContainer = _panel.transform.Find("ContentBox/CardContainer");
            }

            if (_cardContainer == null || _panel == null)
            {
                Debug.Log("[Recruitment TRACE] _cardContainer or _panel null in PopulateCandidates. Rebuilding UI...");
                BuildUI();
            }

            Debug.Log($"[Recruitment TRACE] PopulateCandidates ENTER. _cardContainer null? {(_cardContainer == null)}, RecruitmentManager.Instance null? {(RecruitmentManager.Instance == null)}");
            if (_cardContainer == null || RecruitmentManager.Instance == null)
            {
                Debug.LogWarning($"[Recruitment TRACE] PopulateCandidates ABORTED due to null references! _cardContainer null? {(_cardContainer == null)}, RecruitmentManager null? {(RecruitmentManager.Instance == null)}");
                return;
            }

            // Clear previously tracked card GameObjects safely
            foreach (var oldCard in _spawnedCards)
            {
                if (oldCard != null) DestroyImmediate(oldCard);
            }
            _spawnedCards.Clear();

            int currentEmp = EmployeeManager.Instance != null ? EmployeeManager.Instance.Employees.Count : 0;
            int maxCap = RecruitmentManager.Instance != null ? RecruitmentManager.Instance.MaxStaffCapacity : 3;
            int officeCap = OfficeManager.Instance != null ? OfficeManager.Instance.CurrentCapacity : 2;

            if (_capacityText != null)
                _capacityText.text = $"OFFICE CAPACITY: <b>{currentEmp} / {officeCap}</b>   (SAFETY LIMIT: {maxCap})";

            var pool = RecruitmentManager.Instance.CandidatePool;

            if (pool == null || pool.Count == 0)
            {
                Debug.Log("[Recruitment TRACE] Candidate pool is empty. Triggering RefreshCandidates(3)...");
                RecruitmentManager.Instance.RefreshCandidates(3);
                pool = RecruitmentManager.Instance.CandidatePool;
            }

            Debug.Log($"[Recruitment TRACE] Candidate pool count = {(pool != null ? pool.Count : 0)}");

            if (pool == null)
            {
                Debug.LogWarning("[Recruitment TRACE] Candidate pool is still null after refresh!");
                return;
            }

            float[] xPositions = new float[] { -330f, 0f, 330f };

            for (int i = 0; i < pool.Count && i < 3; i++)
            {
                var candidate = pool[i];
                Debug.Log($"[Recruitment TRACE] Creating candidate card #{i}: {candidate.employeeName}");
                CreateCandidateCard(candidate, i, xPositions[i]);
            }

            Debug.Log($"[Recruitment TRACE] PopulateCandidates EXIT. Spawned {_spawnedCards.Count} cards successfully.");
        }

        private void CreateCandidateCard(EmployeeData c, int index, float xPos)
        {
            Debug.Log($"[Recruitment TRACE] CreateCandidateCard({index}) ENTER. Name={c.employeeName}");
            GameObject cardGo = new GameObject($"CandidateCard_{index}_{c.employeeName}");
            cardGo.transform.SetParent(_cardContainer, false);
            _spawnedCards.Add(cardGo);

            Debug.Log($"[Recruitment TRACE] Card GO created: '{cardGo.name}'. Parent: '{cardGo.transform.parent?.name}'");

            // Card RectTransform
            RectTransform rt = cardGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition3D = new Vector3(xPos, 0f, 0f);
            rt.sizeDelta = new Vector2(300f, 440f);
            cardGo.transform.localScale = Vector3.one;

            // Card Background Image
            Image cardImg = cardGo.AddComponent<Image>();
            cardImg.color = new Color(0.14f, 0.18f, 0.28f, 1.0f);
            cardImg.raycastTarget = true;

            Debug.Log($"[Recruitment TRACE] Card RectTransform setup: position={rt.anchoredPosition3D}, sizeDelta={rt.sizeDelta}, scale={cardGo.transform.localScale}, activeSelf={cardGo.activeSelf}, activeInHierarchy={cardGo.activeInHierarchy}");

            // Border Line
            GameObject borderGo = new GameObject("Border");
            borderGo.transform.SetParent(cardGo.transform, false);
            RectTransform borderRt = borderGo.AddComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.offsetMin = new Vector2(-2, -2);
            borderRt.offsetMax = new Vector2(2, 2);
            borderRt.anchoredPosition3D = Vector3.zero;
            Image borderImg = borderGo.AddComponent<Image>();
            borderImg.color = new Color(0.0f, 0.75f, 0.95f, 0.8f);
            borderImg.raycastTarget = false;

            // Candidate Name Text
            GameObject nameGo = new GameObject("CandidateName");
            nameGo.transform.SetParent(cardGo.transform, false);
            RectTransform nameRt = nameGo.AddComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0.05f, 0.84f);
            nameRt.anchorMax = new Vector2(0.95f, 0.96f);
            nameRt.offsetMin = Vector2.zero;
            nameRt.offsetMax = Vector2.zero;
            nameRt.anchoredPosition3D = Vector3.zero;

            Text nameTxt = nameGo.AddComponent<Text>();
            nameTxt.font = _uiFont;
            nameTxt.fontSize = 20;
            nameTxt.fontStyle = FontStyle.Bold;
            nameTxt.color = new Color(0.0f, 0.95f, 1.0f, 1.0f);
            nameTxt.text = c.employeeName;
            nameTxt.alignment = TextAnchor.MiddleCenter;
            nameTxt.raycastTarget = false;

            // Candidate Role Text
            GameObject roleGo = new GameObject("CandidateRole");
            roleGo.transform.SetParent(cardGo.transform, false);
            RectTransform roleRt = roleGo.AddComponent<RectTransform>();
            roleRt.anchorMin = new Vector2(0.05f, 0.74f);
            roleRt.anchorMax = new Vector2(0.95f, 0.83f);
            roleRt.offsetMin = Vector2.zero;
            roleRt.offsetMax = Vector2.zero;
            roleRt.anchoredPosition3D = Vector3.zero;

            Text roleTxt = roleGo.AddComponent<Text>();
            roleTxt.font = _uiFont;
            roleTxt.fontSize = 15;
            roleTxt.fontStyle = FontStyle.Bold;
            roleTxt.color = new Color(1.0f, 0.85f, 0.30f, 1.0f);
            roleTxt.text = c.role;
            roleTxt.alignment = TextAnchor.MiddleCenter;
            roleTxt.raycastTarget = false;

            // Divider Line
            GameObject divGo = new GameObject("Divider");
            divGo.transform.SetParent(cardGo.transform, false);
            RectTransform divRt = divGo.AddComponent<RectTransform>();
            divRt.anchorMin = new Vector2(0.08f, 0.72f);
            divRt.anchorMax = new Vector2(0.92f, 0.73f);
            divRt.offsetMin = Vector2.zero;
            divRt.offsetMax = Vector2.zero;
            divRt.anchoredPosition3D = Vector3.zero;
            Image divImg = divGo.AddComponent<Image>();
            divImg.color = new Color(0.3f, 0.4f, 0.6f, 0.8f);
            divImg.raycastTarget = false;

            // Stats Body Text
            GameObject statsGo = new GameObject("StatsBody");
            statsGo.transform.SetParent(cardGo.transform, false);
            RectTransform statsRt = statsGo.AddComponent<RectTransform>();
            statsRt.anchorMin = new Vector2(0.08f, 0.26f);
            statsRt.anchorMax = new Vector2(0.92f, 0.70f);
            statsRt.offsetMin = Vector2.zero;
            statsRt.offsetMax = Vector2.zero;
            statsRt.anchoredPosition3D = Vector3.zero;

            Text statsTxt = statsGo.AddComponent<Text>();
            statsTxt.font = _uiFont;
            statsTxt.fontSize = 14;
            statsTxt.color = Color.white;
            statsTxt.alignment = TextAnchor.UpperLeft;
            statsTxt.raycastTarget = false;
            statsTxt.text = $"Research:       {c.researchSkill}\n" +
                            $"Reasoning:     {c.reasoningSkill}\n" +
                            $"Engineering:   {c.engineeringSkill}\n" +
                            $"Motivation:    {c.motivation}\n\n" +
                            $"Salary: ${c.salaryPerDay}/day\n" +
                            $"Trait: {c.traits}";

            // HIRE Button
            bool underOfficeLimit = OfficeManager.Instance == null || OfficeManager.Instance.CanHireMoreEmployees();
            bool canHire = RecruitmentManager.Instance != null && RecruitmentManager.Instance.CanHire();

            string buttonLabel = "HIRE";
            if (!underOfficeLimit) buttonLabel = "OFFICE FULL";
            else if (!canHire) buttonLabel = "LIMIT REACHED";

            GameObject hireBtnGo = new GameObject("HireButton");
            hireBtnGo.transform.SetParent(cardGo.transform, false);
            RectTransform hireRt = hireBtnGo.AddComponent<RectTransform>();
            hireRt.anchorMin = new Vector2(0.5f, 0f);
            hireRt.anchorMax = new Vector2(0.5f, 0f);
            hireRt.pivot     = new Vector2(0.5f, 0.5f);
            hireRt.anchoredPosition3D = new Vector3(0f, 35f, 0f);
            hireRt.sizeDelta = new Vector2(210f, 44f);
            hireRt.localScale = Vector3.one;

            Image hireImg = hireBtnGo.AddComponent<Image>();
            hireImg.color = canHire ? new Color(0.15f, 0.68f, 0.32f, 1.0f) : new Color(0.35f, 0.38f, 0.45f, 1.0f);
            hireImg.raycastTarget = true;

            Button btn = hireBtnGo.AddComponent<Button>();
            if (canHire)
            {
                ColorBlock cb = btn.colors;
                cb.normalColor = new Color(0.15f, 0.68f, 0.32f, 1.0f);
                cb.highlightedColor = new Color(0.22f, 0.85f, 0.40f, 1.0f);
                cb.pressedColor = new Color(0.10f, 0.50f, 0.22f, 1.0f);
                btn.colors = cb;

                btn.onClick.AddListener(() => {
                    Debug.Log($"[UI] Hire clicked for {c.employeeName}");
                    if (RecruitmentManager.Instance != null && RecruitmentManager.Instance.HireCandidate(c))
                    {
                        PopulateCandidates();
                    }
                });
            }

            GameObject hireTxtGo = new GameObject("Text");
            hireTxtGo.transform.SetParent(hireBtnGo.transform, false);
            RectTransform hireTxtRt = hireTxtGo.AddComponent<RectTransform>();
            hireTxtRt.anchorMin = Vector2.zero;
            hireTxtRt.anchorMax = Vector2.one;
            hireTxtRt.offsetMin = Vector2.zero;
            hireTxtRt.offsetMax = Vector2.zero;
            hireTxtRt.anchoredPosition3D = Vector3.zero;

            Text hireTxt = hireTxtGo.AddComponent<Text>();
            hireTxt.font = _uiFont;
            hireTxt.fontSize = 17;
            hireTxt.fontStyle = FontStyle.Bold;
            hireTxt.color = Color.white;
            hireTxt.text = buttonLabel;
            hireTxt.alignment = TextAnchor.MiddleCenter;
            hireTxt.raycastTarget = false;
        }
    }
}
