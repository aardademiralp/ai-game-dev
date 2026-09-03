using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using GameDevStudio.Flow;
using GameDevStudio.Save;

namespace GameDevStudio.UI
{
    /// <summary>
    /// Pause Menu UI. Toggled with Escape key during active gameplay.
    /// Offers: Resume, Save (3 slots), Settings, Return to Main Menu.
    /// Built entirely in code — no prefab required.
    /// </summary>
    public class PauseMenuUI : MonoBehaviour
    {
        public static PauseMenuUI Instance { get; private set; }

        private Canvas      _canvas;
        private GameObject  _mainPanel;
        private GameObject  _savePanel;

        // Save panel refs
        private Text[]   _saveSlotTexts   = new Text[SaveManager.MaxSlots];
        private Button[] _saveSlotButtons = new Button[SaveManager.MaxSlots];

        private InputAction _keyEsc;
        private bool _wasTimePausedBeforeMenu;

        // ─────────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _keyEsc = new InputAction("PauseMenu_Esc", InputActionType.Button, "<Keyboard>/escape");
            _keyEsc.Enable();
        }

        private void OnDestroy()
        {
            _keyEsc?.Dispose();
        }

        private void Start()
        {
            BuildUI();
            Hide();
        }

        private void Update()
        {
            if (_keyEsc.WasPressedThisFrame())
            {
                if (GameFlowManager.Instance == null || !GameFlowManager.Instance.SessionActive)
                    return;

                if (_canvas != null && _canvas.gameObject.activeSelf)
                    OnResumeClicked();
                else
                    Show();
            }
        }

        // ── Public API ────────────────────────────────────────────────────
        public void Show()
        {
            // Pause game time while menu is open
            _wasTimePausedBeforeMenu = Core.GameTimeManager.Instance?.IsPaused ?? false;
            Core.GameTimeManager.Instance?.SetPause(true);

            if (_canvas != null) _canvas.gameObject.SetActive(true);
            ShowMainPanel();
            RefreshSaveSlots();
        }

        public void Hide()
        {
            if (_canvas != null) _canvas.gameObject.SetActive(false);

            // Restore time pause state
            if (!_wasTimePausedBeforeMenu)
                Core.GameTimeManager.Instance?.SetPause(false);
        }

        // ── UI Builder ────────────────────────────────────────────────────
        private void BuildUI()
        {
            // Canvas
            GameObject canvasGo = new GameObject("PauseMenuCanvas");
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 90;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            // Background
            GameObject bg = CreatePanel(canvasGo.transform, "BG",
                new Color(0f, 0f, 0f, 0.75f));

            // ── Main Panel ─────────────────────────────────────────────────
            _mainPanel = CreateCard(bg.transform, "MainPanel", new Vector2(0, 0), new Vector2(380, 480));

            // Title
            CreateLabel(_mainPanel.transform, "lbl_paused", "PAUSED",
                new Vector2(0, 160), new Vector2(340, 60), TextAnchor.MiddleCenter, 40).color = new Color(0.3f, 0.8f, 1f);

            // Buttons
            Button btnResume  = CreateMenuButton(_mainPanel.transform, "btn_resume",  "RESUME",          new Vector2(0,  60));
            Button btnSave    = CreateMenuButton(_mainPanel.transform, "btn_save",    "SAVE GAME",       new Vector2(0, -20));
            Button btnMain    = CreateMenuButton(_mainPanel.transform, "btn_main",    "MAIN MENU",       new Vector2(0, -100));

            btnResume.onClick.AddListener(OnResumeClicked);
            btnSave.onClick.AddListener(OnSaveClicked);
            btnMain.onClick.AddListener(OnMainMenuClicked);

            ColorBlock exitCb = btnMain.colors;
            exitCb.normalColor = new Color(0.5f, 0.1f, 0.1f);
            btnMain.colors = exitCb;

            // ── Save Panel ─────────────────────────────────────────────────
            _savePanel = CreateCard(bg.transform, "SavePanel", new Vector2(0, 0), new Vector2(420, 480));

            CreateLabel(_savePanel.transform, "lbl_save", "SAVE GAME",
                new Vector2(0, 160), new Vector2(380, 60), TextAnchor.MiddleCenter, 36).color = new Color(0.3f, 0.8f, 1f);

            for (int i = 0; i < SaveManager.MaxSlots; i++)
            {
                int slot = i;
                float y = 70 - i * 100f;

                _saveSlotTexts[i] = CreateLabel(_savePanel.transform, $"lbl_slot_{i}", "",
                    new Vector2(-30, y), new Vector2(280, 70), TextAnchor.MiddleLeft, 18);

                _saveSlotButtons[i] = CreateSmallButton(_savePanel.transform, $"btn_save_{i}",
                    $"Slot {i + 1}", new Vector2(150, y));
                _saveSlotButtons[i].onClick.AddListener(() => OnSaveToSlot(slot));
            }

            Button btnSaveBack = CreateMenuButton(_savePanel.transform, "btn_save_back", "BACK", new Vector2(0, -165));
            btnSaveBack.onClick.AddListener(ShowMainPanel);
        }

        private void ShowMainPanel()
        {
            _mainPanel.SetActive(true);
            _savePanel.SetActive(false);
        }

        private void RefreshSaveSlots()
        {
            for (int i = 0; i < SaveManager.MaxSlots; i++)
            {
                bool has = SaveManager.Instance != null && SaveManager.Instance.HasSave(i);
                if (has)
                {
                    var info = SaveManager.Instance.GetSlotInfo(i);
                    _saveSlotTexts[i].text = info != null
                        ? $"Day {info.DisplayDay}  ${info.DisplayMoney:N0}\n{info.DisplayCompanyName}"
                        : $"Slot {i + 1}";
                }
                else
                {
                    _saveSlotTexts[i].text = "(empty)";
                }
            }
        }

        // ── Callbacks ─────────────────────────────────────────────────────
        private void OnResumeClicked() => Hide();

        private void OnSaveClicked()
        {
            RefreshSaveSlots();
            _mainPanel.SetActive(false);
            _savePanel.SetActive(true);
        }

        private void OnSaveToSlot(int slot)
        {
            SaveManager.Instance?.Save(slot);
            RefreshSaveSlots();
        }

        private void OnMainMenuClicked()
        {
            Hide();
            GameFlowManager.Instance?.ReturnToMainMenu();
        }

        // ── UI Helpers ────────────────────────────────────────────────────
        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.color = color;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return go;
        }

        private static GameObject CreateCard(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.color = new Color(0.06f, 0.09f, 0.18f, 0.97f);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size; rt.anchoredPosition = pos;
            return go;
        }

        private static Text CreateLabel(Transform parent, string name, string text,
            Vector2 pos, Vector2 size, TextAnchor alignment, int fontSize)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Text t = go.AddComponent<Text>();
            t.text = text; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = fontSize; t.alignment = alignment; t.color = Color.white;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size; rt.anchoredPosition = pos;
            return t;
        }

        private static Button CreateMenuButton(Transform parent, string name, string label, Vector2 pos)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>(); img.color = new Color(0.14f, 0.20f, 0.40f);
            Button btn = go.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.highlightedColor = new Color(0.25f, 0.55f, 1f); cb.pressedColor = new Color(0.1f, 0.3f, 0.7f);
            btn.colors = cb;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300, 56); rt.anchoredPosition = pos;

            GameObject lgo = new GameObject("Label"); lgo.transform.SetParent(go.transform, false);
            Text t = lgo.AddComponent<Text>();
            t.text = label; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 22; t.alignment = TextAnchor.MiddleCenter; t.color = Color.white;
            RectTransform lrt = lgo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            return btn;
        }

        private static Button CreateSmallButton(Transform parent, string name, string label, Vector2 pos)
        {
            Button btn = CreateMenuButton(parent, name, label, pos);
            btn.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 46);
            foreach (Text t in btn.GetComponentsInChildren<Text>()) t.fontSize = 16;
            return btn;
        }
    }
}
