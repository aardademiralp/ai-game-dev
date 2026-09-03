using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace GameDevStudio.Localization
{
    /// <summary>
    /// Key-based localization system. Load JSON locale files from StreamingAssets or Resources.
    /// Call LocalizationManager.Get("key") anywhere to retrieve localized text.
    /// Change language at runtime with SetLanguage() — fires OnLanguageChanged for all listeners.
    /// </summary>
    public class LocalizationManager : MonoBehaviour
    {
        public static LocalizationManager Instance { get; private set; }

        public enum Language { English, Turkish }

        public Language CurrentLanguage { get; private set; } = Language.English;

        private Dictionary<string, string> _strings = new Dictionary<string, string>();

        public event System.Action OnLanguageChanged;

        // ── Lifecycle ─────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Load language from saved settings if available
            Language saved = (Language)PlayerPrefs.GetInt("Language", (int)Language.English);
            LoadLanguage(saved);
        }

        // ── Public API ────────────────────────────────────────────────────────
        public void SetLanguage(Language lang)
        {
            if (CurrentLanguage == lang && _strings.Count > 0) return;
            LoadLanguage(lang);
            PlayerPrefs.SetInt("Language", (int)lang);
            PlayerPrefs.Save();
            OnLanguageChanged?.Invoke();
        }

        /// <summary>Returns localized string for key. Falls back to key itself if missing.</summary>
        public static string Get(string key)
        {
            if (Instance != null && Instance._strings.TryGetValue(key, out string val))
                return val;
            return key; // Fallback: show the key itself so missing keys are obvious
        }

        // ── Internal ──────────────────────────────────────────────────────────
        private void LoadLanguage(Language lang)
        {
            CurrentLanguage = lang;
            string code = lang == Language.Turkish ? "tr" : "en";
            _strings = LoadJson(code);
            Debug.Log($"[Localization] Loaded '{code}' ({_strings.Count} keys).");
        }

        private static Dictionary<string, string> LoadJson(string code)
        {
            var result = new Dictionary<string, string>();

            // Try Application.dataPath-relative path (works in Editor and builds)
            string path = Path.Combine(Application.dataPath, "_Project", "Localization", code + ".json");

            string json = null;
            if (File.Exists(path))
            {
                json = File.ReadAllText(path);
            }
            else
            {
                // Fallback: try Resources (if locale files placed under Resources/)
                TextAsset ta = Resources.Load<TextAsset>("Localization/" + code);
                if (ta != null) json = ta.text;
            }

            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning($"[Localization] Could not find locale file: {code}.json");
                return result;
            }

            // Minimal JSON parser for flat key:value string objects
            // Strips outer braces, splits on lines, parses "key": "value" pairs
            json = json.Trim();
            if (json.StartsWith("{")) json = json.Substring(1);
            if (json.EndsWith("}")) json = json.Substring(0, json.Length - 1);

            string[] lines = json.Split('\n');
            foreach (string raw in lines)
            {
                string line = raw.Trim().TrimEnd(',');
                if (!line.StartsWith("\"")) continue;

                int colon = line.IndexOf("\": \"");
                if (colon < 0) continue;

                string k = line.Substring(1, colon - 1);
                string v = line.Substring(colon + 4);
                if (v.EndsWith("\"")) v = v.Substring(0, v.Length - 1);
                // Unescape basic sequences
                v = v.Replace("\\n", "\n").Replace("\\\"", "\"");
                result[k] = v;
            }

            return result;
        }
    }
}
