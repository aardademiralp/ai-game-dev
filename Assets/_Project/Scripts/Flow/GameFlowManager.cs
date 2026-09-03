using UnityEngine;

namespace GameDevStudio.Flow
{
    /// <summary>
    /// Coordinates high-level game flow: start new session, load, pause, return to menu.
    /// Delegates state tracking to GameStateManager.
    /// </summary>
    public class GameFlowManager : MonoBehaviour
    {
        public static GameFlowManager Instance { get; private set; }

        public string CompanyName   { get; private set; } = "My AI Studio";

        // Legacy compatibility — true when a session is in Gameplay or Paused state
        public bool SessionActive =>
            GameStateManager.Instance != null &&
            (GameStateManager.Instance.Current == GameStateManager.GameState.Gameplay ||
             GameStateManager.Instance.Current == GameStateManager.GameState.Paused);

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Ensure time is paused and state is MainMenu on boot
            Core.GameTimeManager.Instance?.SetPause(true);
            GameStateManager.Instance?.GoMainMenu();

            // Show main menu
            UI.MainMenuUI.Instance?.Show();
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void StartNewGame(string companyName, DifficultyType difficulty)
        {
            CompanyName = companyName;
            DifficultyManager.Instance?.SetDifficulty(difficulty);

            // Apply starting money from difficulty
            int startMoney = DifficultyManager.Instance?.Current?.StartingMoney ?? 5000;
            Economy.MoneyManager.Instance?.SetMoney(startMoney);

            Core.GameTimeManager.Instance?.SetPause(false);
            GameStateManager.Instance?.GoGameplay();

            Debug.Log($"[GameFlow] New game — Company:'{companyName}' Difficulty:{difficulty} StartMoney:${startMoney}");
        }

        public void ResumeSave(int slot)
        {
            bool ok = Save.SaveManager.Instance?.Load(slot) ?? false;
            if (!ok) { Debug.LogWarning($"[GameFlow] Failed to load slot {slot}."); return; }

            Core.GameTimeManager.Instance?.SetPause(false);
            GameStateManager.Instance?.GoGameplay();
            Debug.Log($"[GameFlow] Session resumed from slot {slot}.");
        }

        public void QuickSave(int slot = 0) => Save.SaveManager.Instance?.Save(slot);

        public void ReturnToMainMenu()
        {
            Core.GameTimeManager.Instance?.SetPause(true);
            GameStateManager.Instance?.GoMainMenu();
            UI.PauseMenuUI.Instance?.Hide();
            UI.MainMenuUI.Instance?.Show();
        }

        public void PauseSession()
        {
            if (!SessionActive) return;
            Core.GameTimeManager.Instance?.SetPause(true);
            GameStateManager.Instance?.GoPaused();
        }

        public void ResumeSession()
        {
            if (GameStateManager.Instance?.Current != GameStateManager.GameState.Paused) return;
            Core.GameTimeManager.Instance?.SetPause(false);
            GameStateManager.Instance?.GoResume();
        }

        // Called by SaveManager on load
        public void SetCompanyName(string name) => CompanyName = name;
    }
}
