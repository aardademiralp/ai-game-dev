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
            ResetRuntimeState();

            CompanyName = companyName;
            DifficultyManager.Instance?.SetDifficulty(difficulty);

            // Apply starting money from difficulty
            int startMoney = DifficultyManager.Instance?.Current?.StartingMoney ?? 5000;
            Economy.MoneyManager.Instance?.SetMoney(startMoney);

            Core.GameTimeManager.Instance?.SetPause(false);
            GameStateManager.Instance?.GoGameplay();

            Debug.Log($"[GameFlow] New game — Company:'{companyName}' Difficulty:{difficulty} StartMoney:${startMoney}");
        }

        /// <summary>
        /// Resets all in-memory runtime systems to default states for a clean New Game.
        /// Does NOT modify or touch any save files on disk.
        /// </summary>
        private void ResetRuntimeState()
        {
            Debug.Log("[GameFlow] Starting ResetRuntimeState for New Game...");

            // 1. Office Furniture & Grid Cleanup
            Office.FurniturePlacer.Instance?.ClearAllPlacedFurniture();

            // 2. Employees & Workstations Cleanup
            Characters.EmployeeManager.Instance?.ClearAllEmployees();

            // 3. Office Level Reset (6x6 starter office)
            Office.OfficeManager.Instance?.ResetToDefault();

            // 4. Game Time Reset (Day 1 08:00)
            Core.GameTimeManager.Instance?.ResetState();

            // 5. AI Core Stats Reset
            AI.AICore.Instance?.ResetStats();

            // 6. Technology Tree & Research Managers Reset
            AI.TechnologyDatabase.Instance?.ResetToDefault();
            AI.TechnologyResearchManager.Instance?.ResetState();
            AI.ResearchManager.Instance?.ResetState();

            // 7. Product Development Manager Reset
            Products.ProductDevelopmentManager.Instance?.ResetState();

            Debug.Log("[GameFlow] ResetRuntimeState completed cleanly.");
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
