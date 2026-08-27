using UnityEngine;
using UnityEngine.UI;
using GameDevStudio.Economy;
using GameDevStudio.Core;
using GameDevStudio.Office;
using GameDevStudio.Characters;

namespace GameDevStudio.UI
{
    /// <summary>
    /// Professional Tycoon HUD built with Unity uGUI (Canvas + CanvasScaler).
    /// Scaled for 1920x1080 reference resolution.
    ///
    /// Hierarchy:
    ///   HUDCanvas (Canvas, Screen Space Overlay, CanvasScaler 1920x1080)
    ///    └── TopBarPanel (Dark translucent bar, top 20px, margins 30px, height 75px)
    ///         ├── MoneyText (Left, green $ count)
    ///         ├── CenterContainer (Center, Day & Time)
    ///         └── RightContainer (Right, Staff count & Speed indicator)
    ///    └── BuildBadgePanel (Top-center below bar, amber badge for build controls)
    /// </summary>
    public class DebugHUD : MonoBehaviour
    {
        // ── UI References ────
        private Canvas         _canvas;
        private CanvasScaler   _scaler;
        private GameObject     _topBarPanel;
        private GameObject     _buildBadgePanel;

        private Text _moneyText;
        private Text _dayText;
        private Text _timeText;
        private Text _staffText;
        private Text _speedText;
        private Text _buildBadgeText;

        private Font _uiFont;

        // ── Cached State ────
        private int    _lastMoney    = -1;
        private int    _lastDay      = -1;
        private string _lastTimeStr  = "";
        private float  _lastSpeed    = -1f;
        private bool   _lastPaused   = false;
        private bool   _lastPlacing  = false;
        private int    _lastEmpCount = -1;
        private int    _lastWsCount  = -1;

        private void Awake()
        {
            LoadFont();
            BuildUIHierarchy();
        }

        private void LoadFont()
        {
            // Try loading default dynamic font
            _uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_uiFont == null)
            {
                _uiFont = Font.CreateDynamicFontFromOSFont("Arial", 32);
            }
        }

        private void BuildUIHierarchy()
        {
            // 1. Create Canvas GameObject
            GameObject canvasGo = new GameObject("HUDCanvas");
            canvasGo.transform.SetParent(transform, false);

            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;

            // 2. Add CanvasScaler for 1920x1080 reference resolution
            _scaler = canvasGo.AddComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = new Vector2(1920f, 1080f);
            _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            _scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // Create Panel Sprite
            Sprite panelSprite = CreatePanelSprite(new Color(0.08f, 0.10f, 0.14f, 0.92f));
            Sprite badgeSprite = CreatePanelSprite(new Color(0.85f, 0.45f, 0.05f, 0.94f));

            // 3. TopBarPanel
            _topBarPanel = new GameObject("TopBarPanel");
            _topBarPanel.transform.SetParent(canvasGo.transform, false);

            Image topImg = _topBarPanel.AddComponent<Image>();
            topImg.sprite = panelSprite;
            topImg.type = Image.Type.Sliced;
            topImg.color = Color.white;

            RectTransform topRt = _topBarPanel.GetComponent<RectTransform>();
            topRt.anchorMin = new Vector2(0f, 1f);
            topRt.anchorMax = new Vector2(1f, 1f);
            topRt.pivot = new Vector2(0.5f, 1f);
            topRt.offsetMin = new Vector2(30f, -95f);  // Left: 30px, Bottom offset from top anchor
            topRt.offsetMax = new Vector2(-30f, -20f); // Right: -30px, Top margin: 20px (Height: 75px)

            // ── Left: Money ──
            GameObject moneyGo = CreateTextObject("MoneyText", _topBarPanel.transform, 30, FontStyle.Bold, new Color(0.26f, 0.93f, 0.44f), TextAnchor.MiddleLeft);
            _moneyText = moneyGo.GetComponent<Text>();
            RectTransform moneyRt = moneyGo.GetComponent<RectTransform>();
            moneyRt.anchorMin = new Vector2(0f, 0f);
            moneyRt.anchorMax = new Vector2(0.3f, 1f);
            moneyRt.offsetMin = new Vector2(25f, 0f);
            moneyRt.offsetMax = new Vector2(0f, 0f);

            // ── Center Container (Day & Time) ──
            GameObject centerGo = new GameObject("CenterContainer");
            centerGo.transform.SetParent(_topBarPanel.transform, false);
            RectTransform centerRt = centerGo.AddComponent<RectTransform>();
            centerRt.anchorMin = new Vector2(0.35f, 0f);
            centerRt.anchorMax = new Vector2(0.65f, 1f);
            centerRt.offsetMin = Vector2.zero;
            centerRt.offsetMax = Vector2.zero;

            GameObject dayGo = CreateTextObject("DayText", centerGo.transform, 24, FontStyle.Bold, new Color(0.85f, 0.88f, 0.95f), TextAnchor.MiddleRight);
            _dayText = dayGo.GetComponent<Text>();
            RectTransform dayRt = dayGo.GetComponent<RectTransform>();
            dayRt.anchorMin = new Vector2(0f, 0f);
            dayRt.anchorMax = new Vector2(0.48f, 1f);
            dayRt.offsetMin = Vector2.zero;
            dayRt.offsetMax = Vector2.zero;

            GameObject timeGo = CreateTextObject("TimeText", centerGo.transform, 28, FontStyle.Bold, new Color(1.0f, 0.85f, 0.30f), TextAnchor.MiddleLeft);
            _timeText = timeGo.GetComponent<Text>();
            RectTransform timeRt = timeGo.GetComponent<RectTransform>();
            timeRt.anchorMin = new Vector2(0.52f, 0f);
            timeRt.anchorMax = new Vector2(1f, 1f);
            timeRt.offsetMin = Vector2.zero;
            timeRt.offsetMax = Vector2.zero;

            // ── Right Container (Staff & Speed) ──
            GameObject rightGo = new GameObject("RightContainer");
            rightGo.transform.SetParent(_topBarPanel.transform, false);
            RectTransform rightRt = rightGo.AddComponent<RectTransform>();
            rightRt.anchorMin = new Vector2(0.68f, 0f);
            rightRt.anchorMax = new Vector2(1f, 1f);
            rightRt.offsetMin = Vector2.zero;
            rightRt.offsetMax = new Vector2(-25f, 0f);

            GameObject staffGo = CreateTextObject("StaffText", rightGo.transform, 22, FontStyle.Normal, new Color(0.80f, 0.84f, 0.90f), TextAnchor.MiddleRight);
            _staffText = staffGo.GetComponent<Text>();
            RectTransform staffRt = staffGo.GetComponent<RectTransform>();
            staffRt.anchorMin = new Vector2(0f, 0f);
            staffRt.anchorMax = new Vector2(0.58f, 1f);
            staffRt.offsetMin = Vector2.zero;
            staffRt.offsetMax = Vector2.zero;

            GameObject speedGo = CreateTextObject("SpeedText", rightGo.transform, 22, FontStyle.Bold, new Color(0.0f, 0.88f, 1.0f), TextAnchor.MiddleRight);
            _speedText = speedGo.GetComponent<Text>();
            RectTransform speedRt = speedGo.GetComponent<RectTransform>();
            speedRt.anchorMin = new Vector2(0.60f, 0f);
            speedRt.anchorMax = new Vector2(1f, 1f);
            speedRt.offsetMin = Vector2.zero;
            speedRt.offsetMax = Vector2.zero;

            // 4. BuildModeBadge Panel
            _buildBadgePanel = new GameObject("BuildBadgePanel");
            _buildBadgePanel.transform.SetParent(canvasGo.transform, false);

            Image badgeImg = _buildBadgePanel.AddComponent<Image>();
            badgeImg.sprite = badgeSprite;
            badgeImg.type = Image.Type.Sliced;
            badgeImg.color = Color.white;

            RectTransform badgeRt = _buildBadgePanel.GetComponent<RectTransform>();
            badgeRt.anchorMin = new Vector2(0.5f, 1f);
            badgeRt.anchorMax = new Vector2(0.5f, 1f);
            badgeRt.pivot = new Vector2(0.5f, 1f);
            badgeRt.sizeDelta = new Vector2(750f, 42f);
            badgeRt.anchoredPosition = new Vector2(0f, -105f); // Directly below top bar

            GameObject buildTextGo = CreateTextObject("BuildBadgeText", _buildBadgePanel.transform, 19, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            _buildBadgeText = buildTextGo.GetComponent<Text>();
            RectTransform buildTextRt = buildTextGo.GetComponent<RectTransform>();
            buildTextRt.anchorMin = Vector2.zero;
            buildTextRt.anchorMax = Vector2.one;
            buildTextRt.offsetMin = Vector2.zero;
            buildTextRt.offsetMax = Vector2.zero;

            _buildBadgePanel.SetActive(false);
        }

        private GameObject CreateTextObject(string name, Transform parent, int fontSize, FontStyle style, Color color, TextAnchor alignment)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            Text txt = go.AddComponent<Text>();
            txt.font = _uiFont;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.color = color;
            txt.alignment = alignment;
            txt.raycastTarget = false;
            txt.supportRichText = true;

            return go;
        }

        private Sprite CreatePanelSprite(Color color)
        {
            Texture2D tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[16 * 16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(4, 4, 4, 4));
        }

        // ── Update HUD values per frame ─────────────────────────────────
        private void Update()
        {
            int money    = MoneyManager.Instance    != null ? MoneyManager.Instance.CurrentMoney                : 0;
            int day      = GameTimeManager.Instance != null ? GameTimeManager.Instance.CurrentDay             : 1;
            string time  = GameTimeManager.Instance != null ? GameTimeManager.Instance.GetFormattedTime()     : "08:00";
            float speed  = GameTimeManager.Instance != null ? GameTimeManager.Instance.CurrentSpeedMultiplier : 1f;
            bool paused  = GameTimeManager.Instance != null && GameTimeManager.Instance.IsPaused;
            bool placing = FurniturePlacer.IsPlacing;
            int empCount = EmployeeManager.Instance   != null ? EmployeeManager.Instance.Employees.Count   : 0;
            int capCount = RecruitmentManager.Instance != null ? RecruitmentManager.Instance.MaxStaffCapacity : (EmployeeManager.Instance != null ? EmployeeManager.Instance.Workstations.Count : 3);

            if (money != _lastMoney)
            {
                _lastMoney = money;
                if (_moneyText != null) _moneyText.text = $"<b>${money:N0}</b>";
            }

            if (day != _lastDay)
            {
                _lastDay = day;
                if (_dayText != null) _dayText.text = $"DAY {day}";
            }

            if (time != _lastTimeStr)
            {
                _lastTimeStr = time;
                if (_timeText != null) _timeText.text = time;
            }

            if (empCount != _lastEmpCount || capCount != _lastWsCount)
            {
                _lastEmpCount = empCount;
                _lastWsCount  = capCount;
                if (_staffText != null) _staffText.text = $"STAFF  <b>{empCount} / {capCount}</b>";
            }

            if (speed != _lastSpeed || paused != _lastPaused)
            {
                _lastSpeed = speed;
                _lastPaused = paused;
                if (_speedText != null)
                {
                    if (paused)
                    {
                        _speedText.text = "<color=#FF5A5A>❚❚ PAUSED</color>";
                    }
                    else
                    {
                        string spdStr = speed >= 3.9f ? "4x" : speed >= 1.9f ? "2x" : "1x";
                        _speedText.text = $"<color=#00E0FF>► {spdStr}</color>";
                    }
                }
            }

            if (placing != _lastPlacing)
            {
                _lastPlacing = placing;
                if (_buildBadgePanel != null)
                {
                    _buildBadgePanel.SetActive(placing);
                    if (placing && _buildBadgeText != null)
                    {
                        _buildBadgeText.text = "<b>⚒ BUILD MODE</b>   [1] Desk   [2] Chair   [3] Computer   [R] Rotate   [Esc] Cancel";
                    }
                }
            }
        }
    }
}
