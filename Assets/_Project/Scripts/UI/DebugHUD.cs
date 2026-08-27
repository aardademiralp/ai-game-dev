using UnityEngine;
using GameDevStudio.Economy;
using GameDevStudio.Core;
using GameDevStudio.Office;

namespace GameDevStudio.UI
{
    /// <summary>
    /// Lightweight debug HUD displaying Money, Day, Time, Speed, and Controls overlay.
    /// </summary>
    public class DebugHUD : MonoBehaviour
    {
        private GUIStyle _hudBoxStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _subStyle;

        private void OnGUI()
        {
            EnsureStyles();

            int money = MoneyManager.Instance != null ? MoneyManager.Instance.CurrentMoney : 0;
            int day = GameTimeManager.Instance != null ? GameTimeManager.Instance.CurrentDay : 1;
            string timeStr = GameTimeManager.Instance != null ? GameTimeManager.Instance.GetFormattedTime() : "08:00";
            float speed = GameTimeManager.Instance != null ? GameTimeManager.Instance.CurrentSpeedMultiplier : 1f;
            bool isPaused = GameTimeManager.Instance != null && GameTimeManager.Instance.IsPaused;
            bool isPlacing = FurniturePlacer.IsPlacing;

            string statusText = isPaused ? "PAUSED" : $"{speed:0.#}x";

            float width = 450f;
            float height = 64f;
            float left = (Screen.width - width) * 0.5f;
            float top = 12f;

            Rect rect = new Rect(left, top, width, height);
            GUI.Box(rect, GUIContent.none, _hudBoxStyle);

            GUILayout.BeginArea(rect);
            GUILayout.BeginVertical();
            GUILayout.Space(6);

            // Row 1: Money, Day, Time, Speed
            GUILayout.BeginHorizontal();
            GUILayout.Space(12);
            GUILayout.Label($"<b>Money:</b> <color=#4DE94C>${money:N0}</color>", _titleStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"<b>Day:</b> {day}", _titleStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"<b>Time:</b> {timeStr}", _titleStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"<b>Speed:</b> {(isPaused ? "<color=#FF4D4D>PAUSED</color>" : $"<color=#00E5FF>{statusText}</color>")}", _titleStyle);
            GUILayout.Space(12);
            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            // Row 2: Controls hint
            GUILayout.BeginHorizontal();
            GUILayout.Space(12);
            if (isPlacing)
            {
                GUILayout.Label("<color=#FFD700><b>BUILD MODE:</b> [1] Desk [2] Chair [3] Comp | [R] Rotate | [Esc] Exit</color>", _subStyle);
            }
            else
            {
                GUILayout.Label("<b>Controls:</b> [Space] Pause | [1] 1x [2] 2x [3] 4x | [B] Build Mode", _subStyle);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (_hudBoxStyle == null)
            {
                Texture2D bgTex = new Texture2D(1, 1);
                bgTex.SetPixel(0, 0, new Color(0.10f, 0.12f, 0.16f, 0.88f));
                bgTex.Apply();

                _hudBoxStyle = new GUIStyle();
                _hudBoxStyle.normal.background = bgTex;
            }

            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle(GUI.skin.label);
                _titleStyle.fontSize = 14;
                _titleStyle.richText = true;
                _titleStyle.normal.textColor = Color.white;
            }

            if (_subStyle == null)
            {
                _subStyle = new GUIStyle(GUI.skin.label);
                _subStyle.fontSize = 11;
                _subStyle.richText = true;
                _subStyle.normal.textColor = new Color(0.75f, 0.80f, 0.85f);
            }
        }
    }
}
