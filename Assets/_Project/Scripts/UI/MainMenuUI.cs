using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using GameDevStudio.Flow;
using GameDevStudio.Save;
using GameDevStudio.Localization;

namespace GameDevStudio.UI
{
    /// <summary>
    /// Main Menu UI — built entirely in code, no prefabs required.
    /// BuildUI() is called in Awake() so buttons exist before any Start() fires.
    ///
    /// Root Cause Fixes:
    ///   1. MissingComponentException: all GameObjects created with typeof(RectTransform)
    ///   2. NullReferenceException in RefreshRootButtons: BuildUI moved to Awake
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        public static MainMenuUI Instance { get; private set; }

        private enum Screen { Root, NewGame, LoadGame, Settings, Language }

        // ── Panel roots ────────────────────────────────────────────────────
        private Canvas     _canvas;
        private GameObject _rootScreen, _newGameScreen, _loadScreen, _settingsScreen, _langScreen;

        // Root buttons
        private Button _btnNew, _btnLoad, _btnSettings, _btnLang, _btnQuit;

        // New Game
        private InputField _nameField;
        private Button[]   _diffBtns = new Button[4];
        private DifficultyType _chosenDiff = DifficultyType.BizHallederiz;

        // Load Game
        private Text[]   _slotLabels  = new Text[SaveManager.MaxSlots];
        private Button[] _slotLoadBtn = new Button[SaveManager.MaxSlots];
        private Button[] _slotDelBtn  = new Button[SaveManager.MaxSlots];

        // Settings sliders / toggles
        private Slider _masterSlider, _musicSlider, _sfxSlider;
        private Toggle _fullscreenToggle;

        // ─────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            BuildUI();   // Must be in Awake — GameFlowManager.Start calls Show() before MainMenuUI.Start
        }

        private void Start()
        {
            // Sync settings sliders to current persisted values
            SyncSettingsSliders();
        }

        // ── Public API ────────────────────────────────────────────────────────
        public void Show()
        {
            _canvas.gameObject.SetActive(true);
            GoTo(Screen.Root);
        }

        public void Hide()
        {
            _canvas.gameObject.SetActive(false);
        }

        // ── Screen routing ────────────────────────────────────────────────────
        private void GoTo(Screen s)
        {
            _rootScreen.SetActive(s == Screen.Root);
            _newGameScreen.SetActive(s == Screen.NewGame);
            _loadScreen.SetActive(s == Screen.LoadGame);
            _settingsScreen.SetActive(s == Screen.Settings);
            _langScreen.SetActive(s == Screen.Language);

            if (s == Screen.Root)
            {
                GameStateManager.Instance?.GoMainMenu();
                RefreshRootButtons();
            }
            else if (s == Screen.NewGame)
            {
                GameStateManager.Instance?.GoNewGame();
            }
            else if (s == Screen.LoadGame)
            {
                GameStateManager.Instance?.GoLoadGame();
                RefreshLoadSlots();
            }
            else if (s == Screen.Settings)
            {
                SyncSettingsSliders();
            }
        }

        private void RefreshRootButtons()
        {
            // _btnNew is always interactable; _btnLoad only when saves exist
            bool hasSave = false;
            if (SaveManager.Instance != null)
                for (int i = 0; i < SaveManager.MaxSlots; i++)
                    if (SaveManager.Instance.HasSave(i)) { hasSave = true; break; }
            _btnLoad.interactable = hasSave;
        }

        private void RefreshLoadSlots()
        {
            for (int i = 0; i < SaveManager.MaxSlots; i++)
            {
                bool has = SaveManager.Instance != null && SaveManager.Instance.HasSave(i);
                if (has)
                {
                    var info = SaveManager.Instance.GetSlotInfo(i);
                    _slotLabels[i].text = info != null
                        ? $"[{i+1}]  {info.DisplayCompanyName}   Gün {info.DisplayDay}   ${info.DisplayMoney:N0}"
                        : $"[{i+1}]  Slot {i+1}";
                }
                else
                {
                    _slotLabels[i].text = $"[{i+1}]  — Boş —";
                }
                _slotLoadBtn[i].interactable = has;
                _slotDelBtn[i].interactable  = has;
            }
        }

        private void SyncSettingsSliders()
        {
            if (SettingsManager.Instance == null) return;
            if (_masterSlider    != null) _masterSlider.value    = SettingsManager.Instance.MasterVol;
            if (_musicSlider     != null) _musicSlider.value     = SettingsManager.Instance.MusicVol;
            if (_sfxSlider       != null) _sfxSlider.value       = SettingsManager.Instance.SfxVol;
            if (_fullscreenToggle != null) _fullscreenToggle.isOn = UnityEngine.Screen.fullScreen;
        }

        // ── Callbacks ─────────────────────────────────────────────────────────
        private void OnStartGame()
        {
            string company = _nameField != null && !string.IsNullOrWhiteSpace(_nameField.text)
                ? _nameField.text.Trim() : "My AI Studio";
            Hide();
            GameFlowManager.Instance?.StartNewGame(company, _chosenDiff);
        }

        private void OnLoadSlot(int slot)
        {
            Hide();
            GameFlowManager.Instance?.ResumeSave(slot);
        }

        private void OnDeleteSlot(int slot)
        {
            SaveManager.Instance?.DeleteSave(slot);
            RefreshLoadSlots();
        }

        private void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void SelectDiff(DifficultyType d)
        {
            _chosenDiff = d;
            Color sel   = new Color(0.2f, 0.7f, 1f);
            Color unsel = new Color(0.12f, 0.18f, 0.32f);
            for (int i = 0; i < 4; i++)
            {
                if (_diffBtns[i] == null) continue;
                ColorBlock cb = _diffBtns[i].colors;
                cb.normalColor = (i == (int)d) ? sel : unsel;
                _diffBtns[i].colors = cb;
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  UI BUILDER
        // ══════════════════════════════════════════════════════════════════════
        private void BuildUI()
        {
            // ── Canvas ──────────────────────────────────────────────────────
            var canvasGo = MakeRT(transform, "MainMenuCanvas");
            _canvas = canvasGo.gameObject.AddComponent<Canvas>();
            _canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;
            var scaler = canvasGo.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight  = 0.5f;
            canvasGo.gameObject.AddComponent<GraphicRaycaster>();

            // ── Full-screen dark opaque overlay (hides 3D game world completely) ──
            var overlay = MakeRT(_canvas.transform, "Overlay");
            Stretch(overlay);
            AddImage(overlay, new Color(0.04f, 0.06f, 0.13f, 1.0f));

            // ── Build each screen ────────────────────────────────────────────
            _rootScreen    = BuildRootScreen(overlay).gameObject;
            _newGameScreen = BuildNewGameScreen(overlay).gameObject;
            _loadScreen    = BuildLoadScreen(overlay).gameObject;
            _settingsScreen= BuildSettingsScreen(overlay).gameObject;
            _langScreen    = BuildLangScreen(overlay).gameObject;

            // Start hidden; Show() will activate canvas + GoTo(Root)
            _canvas.gameObject.SetActive(false);
        }

        // ─────────────── ROOT ───────────────────────────────────────────────
        private RectTransform BuildRootScreen(RectTransform parent)
        {
            var rt = MakeRT(parent, "Screen_Root"); Stretch(rt);

            float y = 140f;
            _btnNew      = MenuBtn(rt, "btn_new",  "YENİ OYUN",   0, y); y -= 72;
            _btnLoad     = MenuBtn(rt, "btn_load", "OYUN YÜKLE",  0, y); y -= 72;
            var btnSet   = MenuBtn(rt, "btn_set",  "AYARLAR",     0, y); y -= 72;
            var btnLang  = MenuBtn(rt, "btn_lang", "DİL / LANGUAGE", 0, y); y -= 72;
            _btnQuit     = MenuBtn(rt, "btn_quit", "ÇIKIŞ",       0, y);

            ColorBlock qCb = _btnQuit.colors;
            qCb.normalColor = new Color(0.45f, 0.08f, 0.08f);
            _btnQuit.colors = qCb;

            _btnNew.onClick.AddListener(() => GoTo(Screen.NewGame));
            _btnLoad.onClick.AddListener(() => GoTo(Screen.LoadGame));
            btnSet.onClick.AddListener(() => GoTo(Screen.Settings));
            btnLang.onClick.AddListener(() => GoTo(Screen.Language));
            _btnQuit.onClick.AddListener(OnQuit);

            Label(rt, "v0.1-dev", -860, -510, 120, 26, 16, new Color(0.4f, 0.4f, 0.5f));
            return rt;
        }

        // ─────────────── NEW GAME ───────────────────────────────────────────
        private RectTransform BuildNewGameScreen(RectTransform parent)
        {
            var rt = MakeRT(parent, "Screen_NewGame"); Stretch(rt);

            Label(rt, "YENİ OYUN", 0, 220, 500, 60, 44, new Color(0.3f, 0.82f, 1f), FontStyle.Bold);
            Label(rt, "Şirket / Stüdyo Adı", 0, 148, 420, 32, 20, new Color(0.75f, 0.85f, 1f));

            // Company name input
            _nameField = InputF(rt, "input_name", "My AI Studio", 0, 100, 420, 52);

            // Difficulty label
            Label(rt, "Zorluk Seviyesi", 0, 42, 420, 30, 20, new Color(0.75f, 0.85f, 1f));

            // Difficulty cards — 4 wide
            string[] names = { "SİZ BİZE BIRAKIN", "BİZ HALLEDERİZ", "UYKU HARAM", "YATIRIMCIYI ARAMA" };
            string[] descs = {
                "Kontrol altında.\nMuhtemelen.",
                "Startup kurmak ne\nkadar zor olabilir ki?",
                "Kahve soğumadan\nyeni bir kriz çıkabilir.",
                "Bu noktada kimse\nsize para vermeyecek."
            };
            float[] xs = { -310f, -100f, 110f, 320f };
            for (int i = 0; i < 4; i++)
            {
                int idx = i;
                var card = DiffCard(rt, names[i], descs[i], xs[i], -50f);
                _diffBtns[i] = card;
                _diffBtns[i].onClick.AddListener(() => SelectDiff((DifficultyType)idx));
            }
            SelectDiff(DifficultyType.BizHallederiz);

            var btnStart = MenuBtn(rt, "btn_start", "OYUNU BAŞLAT", 0, -185);
            ColorBlock sc = btnStart.colors;
            sc.normalColor = new Color(0.08f, 0.38f, 0.12f);
            btnStart.colors = sc;
            btnStart.onClick.AddListener(OnStartGame);

            var btnBack = SmallBtn(rt, "btn_back", "← GERİ", -200, -255);
            btnBack.onClick.AddListener(() => GoTo(Screen.Root));

            return rt;
        }

        // ─────────────── LOAD GAME ──────────────────────────────────────────
        private RectTransform BuildLoadScreen(RectTransform parent)
        {
            var rt = MakeRT(parent, "Screen_LoadGame"); Stretch(rt);

            Label(rt, "OYUN YÜKLE", 0, 220, 500, 60, 44, new Color(0.3f, 0.82f, 1f), FontStyle.Bold);

            for (int i = 0; i < SaveManager.MaxSlots; i++)
            {
                int slot = i;
                float y = 110f - i * 110f;

                // Slot background card
                var card = MakeRT(rt, $"slot_{i}");
                card.sizeDelta = new Vector2(780, 88); card.anchoredPosition = new Vector2(0, y);
                AddImage(card, new Color(0.10f, 0.14f, 0.24f));

                _slotLabels[i] = Lbl(card, $"lbl_{i}", "", -150, 0, 480, 70, 20);
                _slotLabels[i].alignment = TextAnchor.MiddleLeft;

                _slotLoadBtn[i] = SmallBtn(card, $"load_{i}", "YÜKLE", 200, 0);
                _slotLoadBtn[i].onClick.AddListener(() => OnLoadSlot(slot));

                _slotDelBtn[i] = SmallBtn(card, $"del_{i}", "✕", 305, 0);
                ColorBlock dc = _slotDelBtn[i].colors;
                dc.normalColor = new Color(0.5f, 0.08f, 0.08f);
                _slotDelBtn[i].colors = dc;
                _slotDelBtn[i].onClick.AddListener(() => OnDeleteSlot(slot));
            }

            var btnBack = SmallBtn(rt, "btn_back", "← GERİ", -340, -220);
            btnBack.onClick.AddListener(() => GoTo(Screen.Root));

            return rt;
        }

        // ─────────────── SETTINGS ───────────────────────────────────────────
        private RectTransform BuildSettingsScreen(RectTransform parent)
        {
            var rt = MakeRT(parent, "Screen_Settings"); Stretch(rt);

            Label(rt, "AYARLAR", 0, 240, 400, 60, 44, new Color(0.3f, 0.82f, 1f), FontStyle.Bold);

            // Master volume
            Label(rt, "Ana Ses", -300, 158, 200, 30, 20, new Color(0.8f, 0.9f, 1f));
            _masterSlider = MakeSlider(rt, "sl_master", 0, 160, 380, 36);
            _masterSlider.onValueChanged.AddListener(v => SettingsManager.Instance?.SetMasterVolume(v));

            // Music volume
            Label(rt, "Müzik Sesi", -300, 90, 200, 30, 20, new Color(0.8f, 0.9f, 1f));
            _musicSlider = MakeSlider(rt, "sl_music", 0, 92, 380, 36);
            _musicSlider.onValueChanged.AddListener(v => SettingsManager.Instance?.SetMusicVolume(v));

            // SFX volume
            Label(rt, "Efekt Sesi", -300, 22, 200, 30, 20, new Color(0.8f, 0.9f, 1f));
            _sfxSlider = MakeSlider(rt, "sl_sfx", 0, 24, 380, 36);
            _sfxSlider.onValueChanged.AddListener(v => SettingsManager.Instance?.SetSfxVolume(v));

            // Fullscreen toggle
            Label(rt, "Tam Ekran", -160, -52, 200, 30, 20, new Color(0.8f, 0.9f, 1f));
            _fullscreenToggle = MakeToggle(rt, "tog_full", -10, -50);
            _fullscreenToggle.onValueChanged.AddListener(v => UnityEngine.Screen.fullScreen = v);

            var btnBack = SmallBtn(rt, "btn_back", "← GERİ", -340, -240);
            btnBack.onClick.AddListener(() => GoTo(Screen.Root));

            return rt;
        }

        // ─────────────── LANGUAGE ───────────────────────────────────────────
        private RectTransform BuildLangScreen(RectTransform parent)
        {
            var rt = MakeRT(parent, "Screen_Language"); Stretch(rt);

            Label(rt, "DİL / LANGUAGE", 0, 150, 500, 60, 40, new Color(0.3f, 0.82f, 1f), FontStyle.Bold);

            var btnTR = MenuBtn(rt, "btn_tr", "🇹🇷  Türkçe", 0, 40);
            btnTR.onClick.AddListener(() => { SettingsManager.Instance?.SetLanguage("tr"); GoTo(Screen.Root); });

            var btnEN = MenuBtn(rt, "btn_en", "🇬🇧  English", 0, -50);
            btnEN.onClick.AddListener(() => { SettingsManager.Instance?.SetLanguage("en"); GoTo(Screen.Root); });

            var btnBack = SmallBtn(rt, "btn_back", "← GERİ", -340, -200);
            btnBack.onClick.AddListener(() => GoTo(Screen.Root));

            return rt;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  FACTORY HELPERS — all use typeof(RectTransform) to avoid MissingComponentException
        // ══════════════════════════════════════════════════════════════════════

        /// Creates a UI GameObject with a RectTransform — never plain Transform.
        private static RectTransform MakeRT(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static Image AddImage(RectTransform rt, Color c)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = c;
            return img;
        }

        private static Text Label(RectTransform parent, string text,
            float x, float y, float w, float h, int size,
            Color? color = null, FontStyle style = FontStyle.Normal)
        {
            var rt = MakeRT(parent, "lbl_" + text.Substring(0, Mathf.Min(12, text.Length)));
            rt.sizeDelta = new Vector2(w, h); rt.anchoredPosition = new Vector2(x, y);
            var t = rt.gameObject.AddComponent<Text>();
            t.text      = text;
            t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize  = size;
            t.color     = color ?? Color.white;
            t.fontStyle = style;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return t;
        }

        private static Text Lbl(RectTransform parent, string id, string text,
            float x, float y, float w, float h, int size)
        {
            var rt = MakeRT(parent, id);
            rt.sizeDelta = new Vector2(w, h); rt.anchoredPosition = new Vector2(x, y);
            var t = rt.gameObject.AddComponent<Text>();
            t.text = text; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size; t.color = Color.white; t.alignment = TextAnchor.MiddleCenter;
            return t;
        }

        private static Button MenuBtn(RectTransform parent, string id, string label, float x, float y)
        {
            var rt = MakeRT(parent, id);
            rt.sizeDelta = new Vector2(340, 58); rt.anchoredPosition = new Vector2(x, y);
            AddImage(rt, new Color(0.10f, 0.16f, 0.32f));
            var btn = rt.gameObject.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.highlightedColor = new Color(0.22f, 0.52f, 1f);
            cb.pressedColor     = new Color(0.08f, 0.28f, 0.7f);
            btn.colors = cb;

            var lrt = MakeRT(rt, "Label");
            Stretch(lrt);
            var t = lrt.gameObject.AddComponent<Text>();
            t.text = label; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 22; t.alignment = TextAnchor.MiddleCenter; t.color = Color.white;
            return btn;
        }

        private static Button SmallBtn(RectTransform parent, string id, string label, float x, float y)
        {
            var btn = MenuBtn(parent, id, label, x, y);
            btn.GetComponent<RectTransform>().sizeDelta = new Vector2(130, 46);
            foreach (var t in btn.GetComponentsInChildren<Text>()) t.fontSize = 17;
            return btn;
        }

        private static Button DiffCard(RectTransform parent, string title, string desc, float x, float y)
        {
            var rt = MakeRT(parent, "diff_" + title.Substring(0, 3));
            rt.sizeDelta = new Vector2(188, 130); rt.anchoredPosition = new Vector2(x, y);
            AddImage(rt, new Color(0.10f, 0.16f, 0.32f));
            var btn = rt.gameObject.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.highlightedColor = new Color(0.22f, 0.52f, 1f);
            cb.pressedColor     = new Color(0.08f, 0.28f, 0.7f);
            btn.colors = cb;

            var trt = MakeRT(rt, "Title");
            trt.anchorMin = new Vector2(0, 0.5f); trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(6, 0); trt.offsetMax = new Vector2(-6, -4);
            var tt = trt.gameObject.AddComponent<Text>();
            tt.text = title; tt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tt.fontSize = 13; tt.alignment = TextAnchor.MiddleCenter; tt.color = Color.white;
            tt.fontStyle = FontStyle.Bold;

            var drt = MakeRT(rt, "Desc");
            drt.anchorMin = Vector2.zero; drt.anchorMax = new Vector2(1, 0.5f);
            drt.offsetMin = new Vector2(6, 4); drt.offsetMax = new Vector2(-6, 0);
            var dt = drt.gameObject.AddComponent<Text>();
            dt.text = desc; dt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            dt.fontSize = 11; dt.alignment = TextAnchor.MiddleCenter;
            dt.color = new Color(0.75f, 0.85f, 1f);

            return btn;
        }

        private static InputField InputF(RectTransform parent, string id,
            string placeholder, float x, float y, float w, float h)
        {
            var rt = MakeRT(parent, id);
            rt.sizeDelta = new Vector2(w, h); rt.anchoredPosition = new Vector2(x, y);
            AddImage(rt, new Color(0.12f, 0.16f, 0.26f));
            var field = rt.gameObject.AddComponent<InputField>();

            var phRT = MakeRT(rt, "Placeholder"); Stretch(phRT);
            phRT.offsetMin = new Vector2(10, 0); phRT.offsetMax = new Vector2(-10, 0);
            var phT = phRT.gameObject.AddComponent<Text>();
            phT.text = placeholder; phT.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            phT.fontSize = 20; phT.color = new Color(0.5f, 0.5f, 0.55f);
            phT.alignment = TextAnchor.MiddleLeft;

            var txRT = MakeRT(rt, "Text"); Stretch(txRT);
            txRT.offsetMin = new Vector2(10, 0); txRT.offsetMax = new Vector2(-10, 0);
            var txT = txRT.gameObject.AddComponent<Text>();
            txT.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txT.fontSize = 20; txT.color = Color.white; txT.alignment = TextAnchor.MiddleLeft;

            field.textComponent = txT;
            field.placeholder   = phT;
            field.characterLimit = 32;
            return field;
        }

        private static UnityEngine.UI.Slider MakeSlider(RectTransform parent, string id,
            float x, float y, float w, float h)
        {
            var rt = MakeRT(parent, id);
            rt.sizeDelta = new Vector2(w, h); rt.anchoredPosition = new Vector2(x, y);

            // Background
            var bg = MakeRT(rt, "Background"); Stretch(bg);
            AddImage(bg, new Color(0.15f, 0.18f, 0.28f));

            // Fill area
            var fillArea = MakeRT(rt, "Fill Area");
            fillArea.anchorMin = new Vector2(0, 0.25f); fillArea.anchorMax = new Vector2(1, 0.75f);
            fillArea.offsetMin = new Vector2(5, 0); fillArea.offsetMax = new Vector2(-15, 0);

            var fill = MakeRT(fillArea, "Fill"); Stretch(fill);
            AddImage(fill, new Color(0.22f, 0.55f, 1f));

            // Handle area
            var handleArea = MakeRT(rt, "Handle Slide Area"); Stretch(handleArea);
            var handle = MakeRT(handleArea, "Handle");
            handle.sizeDelta = new Vector2(20, 0);
            AddImage(handle, Color.white);

            var slider = rt.gameObject.AddComponent<UnityEngine.UI.Slider>();
            slider.fillRect   = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handle.gameObject.GetComponent<Image>();
            slider.direction  = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = 0f; slider.maxValue = 1f; slider.value = 1f;
            return slider;
        }

        private static UnityEngine.UI.Toggle MakeToggle(RectTransform parent, string id, float x, float y)
        {
            var rt = MakeRT(parent, id);
            rt.sizeDelta = new Vector2(40, 40); rt.anchoredPosition = new Vector2(x, y);

            var bg = MakeRT(rt, "Background"); Stretch(bg);
            var bgImg = AddImage(bg, new Color(0.2f, 0.2f, 0.3f));

            var checkRT = MakeRT(bg, "Checkmark"); Stretch(checkRT);
            var checkImg = AddImage(checkRT, new Color(0.25f, 0.65f, 1f));

            var tog = rt.gameObject.AddComponent<UnityEngine.UI.Toggle>();
            tog.targetGraphic = bgImg;
            tog.graphic       = checkImg;
            tog.isOn          = UnityEngine.Screen.fullScreen;
            return tog;
        }
    }
}
