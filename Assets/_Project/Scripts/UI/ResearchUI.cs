using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using GameDevStudio.Characters;
using GameDevStudio.AI;

namespace GameDevStudio.UI
{
    /// <summary>
    /// Professional uGUI Research Assignment panel (toggled with F key or Esc).
    /// Displays current AI Core stats, hired employees, active tasks, and progress bars.
    /// Explicit component positioning and unsubscription on destroy to prevent MissingReferenceException.
    /// </summary>
    public class ResearchUI : MonoBehaviour
    {
        public static ResearchUI Instance { get; private set; }

        public bool IsOpen => _isOpen;

        private GameObject   _panel;
        private Transform    _listContainer;
        private Text         _aiStatsHeader;
        private Font         _uiFont;

        private InputAction  _keyF;
        private InputAction  _keyEsc;
        private bool         _isOpen = false;

        private System.Action<ResearchAssignment> _onProgressUpdatedHandler;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ??
                      Resources.GetBuiltinResource<Font>("Arial.ttf") ??
                      Font.CreateDynamicFontFromOSFont("Arial", 24);

            _keyF   = new InputAction("Research_F", InputActionType.Button, "<Keyboard>/f");
            _keyEsc = new InputAction("Research_Esc", InputActionType.Button, "<Keyboard>/escape");
            _keyF.Enable();
            _keyEsc.Enable();

            _onProgressUpdatedHandler = OnProgressUpdated;
        }

        private void OnDestroy()
        {
            _keyF?.Dispose();
            _keyEsc?.Dispose();

            if (ResearchManager.Instance != null && _onProgressUpdatedHandler != null)
            {
                ResearchManager.Instance.OnResearchProgressUpdated -= _onProgressUpdatedHandler;
            }
        }

        private void Start()
        {
            BuildUI();
            if (ResearchManager.Instance != null)
            {
                ResearchManager.Instance.OnResearchProgressUpdated += _onProgressUpdatedHandler;
            }
        }

        private void Update()
        {
            if (_keyF != null && _keyF.WasPressedThisFrame())
            {
                if (!Office.FurniturePlacer.IsPlacing)
                {
                    if (RecruitmentUI.Instance != null && RecruitmentUI.Instance.IsOpen)
                        RecruitmentUI.Instance.CloseWindow();

                    ToggleWindow();
                }
            }

            if (_isOpen && _keyEsc != null && _keyEsc.WasPressedThisFrame())
            {
                CloseWindow();
            }

            if (_isOpen)
            {
                RefreshAIStatsHeader();
                UpdateProgress();
            }
        }

        public void ToggleWindow()
        {
            if (_isOpen) CloseWindow();
            else OpenWindow();
        }

        public void OpenWindow()
        {
            _isOpen = true;
            if (_panel != null)
            {
                _panel.SetActive(true);
                RebuildEmployeeList();
            }
        }

        public void CloseWindow()
        {
            _isOpen = false;
            if (_panel != null)
            {
                _panel.SetActive(false);
            }
        }

        private void BuildUI()
        {
            GameObject canvasGo = new GameObject("ResearchCanvas");
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
            _panel = new GameObject("ResearchWindow");
            _panel.transform.SetParent(canvasGo.transform, false);

            Image bgImg = _panel.AddComponent<Image>();
            bgImg.color = new Color(0.04f, 0.05f, 0.08f, 0.88f);
            bgImg.raycastTarget = true;

            RectTransform panelRt = _panel.GetComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            // Content Box
            GameObject box = new GameObject("ContentBox");
            box.transform.SetParent(_panel.transform, false);
            Image boxImg = box.AddComponent<Image>();
            boxImg.color = new Color(0.08f, 0.10f, 0.16f, 0.98f);
            boxImg.raycastTarget = true;

            RectTransform boxRt = box.GetComponent<RectTransform>();
            boxRt.anchorMin = new Vector2(0.5f, 0.5f);
            boxRt.anchorMax = new Vector2(0.5f, 0.5f);
            boxRt.pivot = new Vector2(0.5f, 0.5f);
            boxRt.sizeDelta = new Vector2(1050f, 650f);

            // Title Text
            GameObject titleGo = new GameObject("Title");
            titleGo.transform.SetParent(box.transform, false);
            Text titleTxt = titleGo.AddComponent<Text>();
            titleTxt.font = _uiFont;
            titleTxt.fontSize = 28;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.color = new Color(0.0f, 0.88f, 1.0f);
            titleTxt.text = "AI RESEARCH ASSIGNMENT";
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.raycastTarget = false;

            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.sizeDelta = new Vector2(0f, 45f);
            titleRt.anchoredPosition = new Vector2(0f, -12f);

            // AI Core Stats Bar
            GameObject statsGo = new GameObject("AIStatsHeader");
            statsGo.transform.SetParent(box.transform, false);
            _aiStatsHeader = statsGo.AddComponent<Text>();
            _aiStatsHeader.font = _uiFont;
            _aiStatsHeader.fontSize = 18;
            _aiStatsHeader.color = new Color(1.0f, 0.85f, 0.30f);
            _aiStatsHeader.alignment = TextAnchor.MiddleCenter;
            _aiStatsHeader.raycastTarget = false;

            RectTransform statsRt = statsGo.GetComponent<RectTransform>();
            statsRt.anchorMin = new Vector2(0f, 1f);
            statsRt.anchorMax = new Vector2(1f, 1f);
            statsRt.pivot = new Vector2(0.5f, 1f);
            statsRt.sizeDelta = new Vector2(0f, 35f);
            statsRt.anchoredPosition = new Vector2(0f, -55f);

            // List Container
            GameObject listContainerGo = new GameObject("ListContainer");
            listContainerGo.transform.SetParent(box.transform, false);
            _listContainer = listContainerGo.transform;

            Image listBgImg = listContainerGo.AddComponent<Image>();
            listBgImg.color = new Color(0.05f, 0.07f, 0.12f, 0.50f);
            listBgImg.raycastTarget = false;

            RectTransform listRt = listContainerGo.GetComponent<RectTransform>();
            listRt.anchorMin = new Vector2(0.03f, 0.12f);
            listRt.anchorMax = new Vector2(0.97f, 0.82f);
            listRt.offsetMin = Vector2.zero;
            listRt.offsetMax = Vector2.zero;

            VerticalLayoutGroup vlg = listContainerGo.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 12f;
            vlg.padding = new RectOffset(15, 15, 15, 15);
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;

            // Close button
            CreateButton(box.transform, "CLOSE (Esc/F)", new Vector2(0f, 30f), new Vector2(200f, 44f), () => {
                Debug.Log("[UI] Research Close clicked");
                CloseWindow();
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

            return btnGo;
        }

        private void RefreshAIStatsHeader()
        {
            if (_aiStatsHeader == null || AICore.Instance == null) return;
            _aiStatsHeader.text = $"AI CORE  |  Quality: {AICore.Instance.Quality}   Speed: {AICore.Instance.Speed}   Reasoning: {AICore.Instance.Reasoning}   Creativity: {AICore.Instance.Creativity}   Reliability: {AICore.Instance.Reliability}";
        }

        private void RebuildEmployeeList()
        {
            if (_listContainer == null || EmployeeManager.Instance == null) return;

            // Safely destroy all previous row GameObjects
            for (int i = _listContainer.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(_listContainer.GetChild(i).gameObject);
            }

            var employees = EmployeeManager.Instance.Employees;
            int empCount = employees != null ? employees.Count : 0;
            Debug.Log($"[ResearchUI] Employees in studio: {empCount}");

            if (empCount == 0)
            {
                GameObject emptyGo = new GameObject("EmptyText");
                emptyGo.transform.SetParent(_listContainer, false);
                Text t = emptyGo.AddComponent<Text>();
                t.font = _uiFont;
                t.fontSize = 20;
                t.fontStyle = FontStyle.Bold;
                t.color = new Color(0.85f, 0.88f, 0.95f);
                t.text = "No employees hired yet. Press R to recruit AI Researchers!";
                t.alignment = TextAnchor.MiddleCenter;
                t.raycastTarget = false;

                LayoutElement le = emptyGo.AddComponent<LayoutElement>();
                le.minHeight = 100f;
                le.preferredHeight = 100f;
            }
            else
            {
                foreach (var emp in employees)
                {
                    if (emp != null) CreateEmployeeRow(emp);
                }
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_listContainer.GetComponent<RectTransform>());
        }

        private void CreateEmployeeRow(EmployeeController emp)
        {
            GameObject rowGo = new GameObject("Row_" + emp.Data?.employeeName);
            rowGo.transform.SetParent(_listContainer, false);

            Image rowImg = rowGo.AddComponent<Image>();
            rowImg.color = new Color(0.14f, 0.17f, 0.25f, 0.98f);
            rowImg.raycastTarget = true;

            LayoutElement rowLe = rowGo.AddComponent<LayoutElement>();
            rowLe.minHeight = 85f;
            rowLe.preferredHeight = 85f;

            var activeAssign = ResearchManager.Instance != null ? ResearchManager.Instance.GetAssignment(emp) : null;
            AIStatType currentTarget = activeAssign != null ? activeAssign.targetStat : AIStatType.Reasoning;

            // Employee Name, Role & Active Task Label
            GameObject nameGo = new GameObject("EmpName");
            nameGo.transform.SetParent(rowGo.transform, false);
            Text nameTxt = nameGo.AddComponent<Text>();
            nameTxt.font = _uiFont;
            nameTxt.fontSize = 15;
            nameTxt.fontStyle = FontStyle.Bold;
            nameTxt.color = Color.white;
            nameTxt.text = $"{emp.Data?.employeeName}\n<color=#FFD700><size=13>{emp.Data?.role}</size></color>\n<color=#00E0FF><size=12>Task: Improve {currentTarget}</size></color>";
            nameTxt.raycastTarget = false;

            RectTransform nameRt = nameGo.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0.02f, 0f);
            nameRt.anchorMax = new Vector2(0.28f, 1f);
            nameRt.offsetMin = Vector2.zero;
            nameRt.offsetMax = Vector2.zero;

            // Task Selector Buttons (Quality, Speed, Reasoning, Creativity, Reliability)
            AIStatType[] stats = new[] { AIStatType.Quality, AIStatType.Speed, AIStatType.Reasoning, AIStatType.Creativity, AIStatType.Reliability };
            float startX = 0.30f;
            float btnWidth = 0.125f;

            for (int i = 0; i < stats.Length; i++)
            {
                AIStatType statType = stats[i];
                bool isSelected = (statType == currentTarget);

                GameObject taskBtnGo = new GameObject("TaskBtn_" + statType);
                taskBtnGo.transform.SetParent(rowGo.transform, false);

                Image btnImg = taskBtnGo.AddComponent<Image>();
                btnImg.color = isSelected ? new Color(0.0f, 0.65f, 0.85f) : new Color(0.22f, 0.26f, 0.36f);
                btnImg.raycastTarget = true;

                Button btn = taskBtnGo.AddComponent<Button>();
                btn.onClick.AddListener(() => {
                    Debug.Log($"[UI] Research task selected: Improve {statType} for {emp.Data?.employeeName}");
                    if (ResearchManager.Instance != null)
                    {
                        ResearchManager.Instance.AssignTask(emp, statType);
                        RebuildEmployeeList();
                    }
                });

                RectTransform taskRt = taskBtnGo.GetComponent<RectTransform>();
                taskRt.anchorMin = new Vector2(startX + (i * btnWidth) + 0.005f, 0.45f);
                taskRt.anchorMax = new Vector2(startX + ((i + 1) * btnWidth) - 0.005f, 0.90f);
                taskRt.offsetMin = Vector2.zero;
                taskRt.offsetMax = Vector2.zero;

                GameObject taskTxtGo = new GameObject("Text");
                taskTxtGo.transform.SetParent(taskBtnGo.transform, false);
                Text taskTxt = taskTxtGo.AddComponent<Text>();
                taskTxt.font = _uiFont;
                taskTxt.fontSize = 12;
                taskTxt.fontStyle = FontStyle.Bold;
                taskTxt.color = Color.white;
                taskTxt.text = statType.ToString();
                taskTxt.alignment = TextAnchor.MiddleCenter;
                taskTxt.raycastTarget = false;

                RectTransform taskTxtRt = taskTxtGo.GetComponent<RectTransform>();
                taskTxtRt.anchorMin = Vector2.zero;
                taskTxtRt.anchorMax = Vector2.one;
            }

            // Progress Bar Background
            GameObject barBgGo = new GameObject("ProgressBarBg");
            barBgGo.transform.SetParent(rowGo.transform, false);
            Image barBgImg = barBgGo.AddComponent<Image>();
            barBgImg.color = new Color(0.10f, 0.12f, 0.18f);
            barBgImg.raycastTarget = false;

            RectTransform barBgRt = barBgGo.GetComponent<RectTransform>();
            barBgRt.anchorMin = new Vector2(0.305f, 0.10f);
            barBgRt.anchorMax = new Vector2(0.925f, 0.38f);
            barBgRt.offsetMin = Vector2.zero;
            barBgRt.offsetMax = Vector2.zero;

            // Progress Bar Fill
            GameObject barFillGo = new GameObject("ProgressBarFill");
            barFillGo.name = "ProgressFill_" + emp.GetInstanceID();
            barFillGo.transform.SetParent(barBgGo.transform, false);
            Image barFillImg = barFillGo.AddComponent<Image>();
            barFillImg.color = new Color(0.26f, 0.93f, 0.44f);
            barFillImg.raycastTarget = false;

            RectTransform barFillRt = barFillGo.GetComponent<RectTransform>();
            float initialFill = activeAssign != null ? activeAssign.progress / 100f : 0f;
            barFillRt.anchorMin = Vector2.zero;
            barFillRt.anchorMax = new Vector2(initialFill, 1f);
            barFillRt.offsetMin = Vector2.zero;
            barFillRt.offsetMax = Vector2.zero;
        }

        private void OnProgressUpdated(ResearchAssignment assign)
        {
            if (this == null || gameObject == null) return;
            UpdateProgress();
        }

        private void UpdateProgress()
        {
            if (!_isOpen || this == null || _listContainer == null || EmployeeManager.Instance == null || ResearchManager.Instance == null) return;

            foreach (var emp in EmployeeManager.Instance.Employees)
            {
                if (emp == null) continue;
                var assign = ResearchManager.Instance.GetAssignment(emp);
                if (assign == null) continue;

                Transform fillT = _listContainer.Find($"Row_{emp.Data?.employeeName}/ProgressBarBg/ProgressFill_{emp.GetInstanceID()}");
                if (fillT != null)
                {
                    RectTransform fillRt = fillT.GetComponent<RectTransform>();
                    if (fillRt != null)
                    {
                        fillRt.anchorMax = new Vector2(Mathf.Clamp01(assign.progress / 100f), 1f);
                    }
                }
            }
        }
    }
}
