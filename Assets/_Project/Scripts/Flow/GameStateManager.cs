using UnityEngine;

namespace GameDevStudio.Flow
{
    /// <summary>
    /// Central Game State authority.
    ///
    /// States:
    ///   MainMenu       → Only MainMenuUI visible. All gameplay systems silent.
    ///   NewGameSetup   → NewGame screen open. Gameplay still blocked.
    ///   LoadGame       → Load screen open. Gameplay still blocked.
    ///   Gameplay       → Full gameplay active. All hotkeys and systems live.
    ///   Paused         → PauseMenuUI open. Gameplay suspended.
    ///
    /// Every input-handling system checks IsGameplayActive before processing input.
    /// </summary>
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        public enum GameState
        {
            MainMenu,
            NewGameSetup,
            LoadGame,
            Gameplay,
            Paused
        }

        public GameState Current { get; private set; } = GameState.MainMenu;

        /// <summary>True only while the player is actively in a gameplay session.</summary>
        public static bool IsGameplayActive =>
            Instance != null &&
            (Instance.Current == GameState.Gameplay || Instance.Current == GameState.Paused);

        public event System.Action<GameState, GameState> OnStateChanged; // (oldState, newState)

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetState(GameState newState)
        {
            if (newState == Current) return;
            GameState old = Current;
            Current = newState;
            Debug.Log($"[GameState] {old} → {newState}");
            OnStateChanged?.Invoke(old, newState);
        }

        // ── Convenience helpers ───────────────────────────────────────────────
        public void GoMainMenu()    => SetState(GameState.MainMenu);
        public void GoNewGame()     => SetState(GameState.NewGameSetup);
        public void GoLoadGame()    => SetState(GameState.LoadGame);
        public void GoGameplay()    => SetState(GameState.Gameplay);
        public void GoPaused()      => SetState(GameState.Paused);
        public void GoResume()      => SetState(GameState.Gameplay);
    }
}
