using UnityEngine;
using System;

namespace GameDevStudio.Flow
{
    /// <summary>
    /// Stores and persists player settings (language, volume, etc.)
    /// using Unity's PlayerPrefs. Separate from save slots.
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        // Keys
        private const string KeyLanguage    = "pref_language";
        private const string KeyMasterVol   = "pref_master_vol";
        private const string KeyMusicVol    = "pref_music_vol";
        private const string KeySfxVol      = "pref_sfx_vol";

        // ── Public State ──────────────────────────────────────────────────
        public string Language   { get; private set; } = "en";
        public float  MasterVol  { get; private set; } = 1f;
        public float  MusicVol   { get; private set; } = 0.7f;
        public float  SfxVol     { get; private set; } = 1f;

        // ── Events ────────────────────────────────────────────────────────
        public event Action OnSettingsChanged;

        // ─────────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }

        // ── Public API ────────────────────────────────────────────────────
        public void SetLanguage(string langCode)
        {
            Language = langCode;
            if (Localization.LocalizationManager.Instance != null)
            {
                var lang = langCode == "tr"
                    ? Localization.LocalizationManager.Language.Turkish
                    : Localization.LocalizationManager.Language.English;
                Localization.LocalizationManager.Instance.SetLanguage(lang);
            }
            Save();
            OnSettingsChanged?.Invoke();
        }

        public void SetMasterVolume(float vol)  { MasterVol = Mathf.Clamp01(vol); ApplyVolumes(); Save(); }
        public void SetMusicVolume(float vol)   { MusicVol  = Mathf.Clamp01(vol); ApplyVolumes(); Save(); }
        public void SetSfxVolume(float vol)     { SfxVol    = Mathf.Clamp01(vol); ApplyVolumes(); Save(); }

        // ── Internal ──────────────────────────────────────────────────────
        private void Load()
        {
            Language  = PlayerPrefs.GetString(KeyLanguage, "en");
            MasterVol = PlayerPrefs.GetFloat(KeyMasterVol, 1f);
            MusicVol  = PlayerPrefs.GetFloat(KeyMusicVol,  0.7f);
            SfxVol    = PlayerPrefs.GetFloat(KeySfxVol,    1f);

            // Apply language immediately
            if (Localization.LocalizationManager.Instance != null)
            {
                var lang = Language == "tr"
                    ? Localization.LocalizationManager.Language.Turkish
                    : Localization.LocalizationManager.Language.English;
                Localization.LocalizationManager.Instance.SetLanguage(lang);
            }
            ApplyVolumes();
        }

        private void Save()
        {
            PlayerPrefs.SetString(KeyLanguage, Language);
            PlayerPrefs.SetFloat(KeyMasterVol, MasterVol);
            PlayerPrefs.SetFloat(KeyMusicVol,  MusicVol);
            PlayerPrefs.SetFloat(KeySfxVol,    SfxVol);
            PlayerPrefs.Save();
        }

        private void ApplyVolumes()
        {
            AudioListener.volume = MasterVol;
            // Future: AudioMixer groups for music/sfx
        }
    }
}
