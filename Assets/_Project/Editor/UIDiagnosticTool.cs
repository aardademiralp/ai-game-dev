using UnityEngine;
using UnityEditor;
using GameDevStudio.UI;
using GameDevStudio.Characters;
using GameDevStudio.Employees;

namespace GameDevStudio.EditorTools
{
    [InitializeOnLoad]
    public static class UIDiagnosticTool
    {
        static UIDiagnosticTool()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                EditorApplication.delayCall += () => {
                    EditorApplication.delayCall += () => {
                        EditorApplication.delayCall += PerformDiagnostic;
                    };
                };
            }
        }

        [MenuItem("Tools/Run Diagnostic Now")]
        public static void PerformDiagnostic()
        {
            Debug.Log("==================================================");
            Debug.Log("[UIDiagnosticTool] STARTING RUNTIME UI DIAGNOSTIC");
            Debug.Log("==================================================");

            var allRecUI = Object.FindObjectsByType<RecruitmentUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Debug.Log($"[UIDiagnosticTool] Found {allRecUI.Length} RecruitmentUI components in scene:");
            foreach (var ui in allRecUI)
            {
                Debug.Log($"[UIDiagnosticTool]   - RecruitmentUI on '{ui.gameObject.name}' (Instance == ui? {RecruitmentUI.Instance == ui})");
            }

            var allResUI = Object.FindObjectsByType<ResearchUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Debug.Log($"[UIDiagnosticTool] Found {allResUI.Length} ResearchUI components in scene:");
            foreach (var ui in allResUI)
            {
                Debug.Log($"[UIDiagnosticTool]   - ResearchUI on '{ui.gameObject.name}' (Instance == ui? {ResearchUI.Instance == ui})");
            }

            // 1. RecruitmentUI Diagnostic
            if (RecruitmentUI.Instance == null)
            {
                Debug.LogError("[UIDiagnosticTool] RecruitmentUI.Instance is NULL!");
            }
            else
            {
                Debug.Log($"[UIDiagnosticTool] Calling RecruitmentUI.Instance.OpenWindow() on GameObject '{RecruitmentUI.Instance.gameObject.name}'...");
                RecruitmentUI.Instance.OpenWindow();
                Debug.Log($"[UIDiagnosticTool] RecruitmentUI.IsOpen = {RecruitmentUI.Instance.IsOpen}");

                Transform cardContainer = RecruitmentUI.Instance.transform.Find("RecruitmentCanvas/RecruitmentWindow/ContentBox/CardContainer");
                if (cardContainer == null)
                {
                    Debug.LogError("[UIDiagnosticTool] CardContainer transform NOT FOUND under RecruitmentUI.Instance!");
                }
                else
                {
                    Debug.Log($"[UIDiagnosticTool] CardContainer found! Child count = {cardContainer.childCount}");
                    for (int i = 0; i < cardContainer.childCount; i++)
                    {
                        Transform child = cardContainer.GetChild(i);
                        RectTransform rt = child.GetComponent<RectTransform>();
                        Debug.Log($"[UIDiagnosticTool] Card #{i}: name='{child.name}', activeSelf={child.gameObject.activeSelf}, activeInHierarchy={child.gameObject.activeInHierarchy}, parent='{child.parent.name}'");
                        if (rt != null)
                        {
                            Debug.Log($"[UIDiagnosticTool] Card #{i} RectTransform: position={rt.anchoredPosition3D}, sizeDelta={rt.sizeDelta}, lossyScale={rt.lossyScale}, anchorMin={rt.anchorMin}, anchorMax={rt.anchorMax}");
                        }
                    }
                }
            }

            // 2. Refresh Test
            if (RecruitmentManager.Instance != null && RecruitmentUI.Instance != null)
            {
                Debug.Log("[UIDiagnosticTool] Calling RecruitmentManager.Instance.RefreshCandidates()...");
                RecruitmentManager.Instance.RefreshCandidates();
                Transform cardContainer = RecruitmentUI.Instance.transform.Find("RecruitmentCanvas/RecruitmentWindow/ContentBox/CardContainer");
                Debug.Log($"[UIDiagnosticTool] Post-Refresh CardContainer child count = {(cardContainer != null ? cardContainer.childCount : -1)}");
            }

            // 3. ResearchUI Diagnostic
            if (ResearchUI.Instance == null)
            {
                Debug.LogError("[UIDiagnosticTool] ResearchUI.Instance is NULL!");
            }
            else
            {
                Debug.Log("[UIDiagnosticTool] Calling ResearchUI.Instance.OpenWindow()...");
                ResearchUI.Instance.OpenWindow();
                Debug.Log($"[UIDiagnosticTool] ResearchUI.IsOpen = {ResearchUI.Instance.IsOpen}");

                Transform listContainer = ResearchUI.Instance.transform.Find("ResearchCanvas/ResearchWindow/ContentBox/ListContainer");
                if (listContainer == null)
                {
                    Debug.LogError("[UIDiagnosticTool] ListContainer transform NOT FOUND under ResearchUI.Instance!");
                }
                else
                {
                    Debug.Log($"[UIDiagnosticTool] ListContainer found! Child count = {listContainer.childCount}");
                    for (int i = 0; i < listContainer.childCount; i++)
                    {
                        Transform child = listContainer.GetChild(i);
                        Debug.Log($"[UIDiagnosticTool] Research child #{i}: name='{child.name}', activeSelf={child.gameObject.activeSelf}, activeInHierarchy={child.gameObject.activeInHierarchy}");
                    }
                }
            }

            Debug.Log("==================================================");
            Debug.Log("[UIDiagnosticTool] DIAGNOSTIC COMPLETE");
            Debug.Log("==================================================");
        }
    }
}
