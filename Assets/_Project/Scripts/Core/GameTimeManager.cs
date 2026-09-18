using UnityEngine;
using UnityEngine.InputSystem;
using System;

namespace GameDevStudio.Core
{
    /// <summary>
    /// Manages simulation time, day progression, and game speed.
    /// Controls:
    ///   Space     → Pause / Resume
    ///   1 / 2 / 3 → 1x / 2x / 4x speed (when not placing furniture)
    /// </summary>
    public class GameTimeManager : MonoBehaviour
    {
        public static GameTimeManager Instance { get; private set; }

        [Header("Time Configuration")]
        [SerializeField] private int startDay = 1;
        [SerializeField] private int startHour = 8;
        [SerializeField] private int startMinute = 0;
        [SerializeField] private float secondsPerGameMinute = 0.1f; // 1 game hour = 6 real seconds at 1x speed

        // ── State ─────────────────────────────────────────────────────────
        public int CurrentDay { get; private set; }
        public int CurrentHour { get; private set; }
        public int CurrentMinute { get; private set; }

        public bool IsPaused { get; private set; }
        public float CurrentSpeedMultiplier { get; private set; } = 1.0f;

        private float _minuteAccumulator;

        // ── Input ─────────────────────────────────────────────────────────
        private InputAction _keySpace;
        private InputAction _key1, _key2, _key3;

        // ── Events ────────────────────────────────────────────────────────
        public event Action<int, int> OnTimeChanged; // (hour, minute)
        public event Action<int> OnDayChanged;      // (day)
        public event Action<bool> OnPauseChanged;    // (isPaused)
        public event Action<float> OnSpeedChanged;   // (speedMultiplier)

        // ─────────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            CurrentDay = startDay;
            CurrentHour = startHour;
            CurrentMinute = startMinute;

            _keySpace = new InputAction("Time_Space", InputActionType.Button, "<Keyboard>/space");
            _key1     = new InputAction("Time_Key1",  InputActionType.Button, "<Keyboard>/1");
            _key2     = new InputAction("Time_Key2",  InputActionType.Button, "<Keyboard>/2");
            _key3     = new InputAction("Time_Key3",  InputActionType.Button, "<Keyboard>/3");

            _keySpace.Enable();
            _key1.Enable();
            _key2.Enable();
            _key3.Enable();
        }

        private void OnDestroy()
        {
            _keySpace?.Dispose();
            _key1?.Dispose();
            _key2?.Dispose();
            _key3?.Dispose();
        }

        private void Start()
        {
            Debug.Log($"[Time] Day {CurrentDay} — {GetFormattedTime()}");
            OnTimeChanged?.Invoke(CurrentHour, CurrentMinute);
            OnDayChanged?.Invoke(CurrentDay);
            OnSpeedChanged?.Invoke(CurrentSpeedMultiplier);
        }

        private void Update()
        {
            HandleInput();
            TickTime();
        }

        private void HandleInput()
        {
            // All time controls are silenced outside of active gameplay
            if (!Flow.GameStateManager.IsGameplayActive) return;

            if (_keySpace.WasPressedThisFrame())
            {
                TogglePause();
            }

            // Only process speed keys 1, 2, 3 if furniture placement is NOT active
            bool isPlacingFurniture = GameDevStudio.Office.FurniturePlacer.IsPlacing;
            if (!isPlacingFurniture)
            {
                if (_key1.WasPressedThisFrame()) SetSpeed(1.0f);
                else if (_key2.WasPressedThisFrame()) SetSpeed(2.0f);
                else if (_key3.WasPressedThisFrame()) SetSpeed(4.0f);
            }
        }

        private void TickTime()
        {
            if (IsPaused) return;

            _minuteAccumulator += (Time.deltaTime / secondsPerGameMinute) * CurrentSpeedMultiplier;

            while (_minuteAccumulator >= 1.0f)
            {
                _minuteAccumulator -= 1.0f;
                AdvanceMinute();
            }
        }

        private void AdvanceMinute()
        {
            CurrentMinute++;
            if (CurrentMinute >= 60)
            {
                CurrentMinute = 0;
                CurrentHour++;
                if (CurrentHour >= 24)
                {
                    CurrentHour = 0;
                    CurrentDay++;
                    Debug.Log($"[Time] Day changed → Day {CurrentDay}");
                    OnDayChanged?.Invoke(CurrentDay);
                }
                Debug.Log($"[Time] Day {CurrentDay} — {GetFormattedTime()}");
            }

            OnTimeChanged?.Invoke(CurrentHour, CurrentMinute);
        }

        public void TogglePause()
        {
            SetPause(!IsPaused);
        }

        public void SetPause(bool pause)
        {
            IsPaused = pause;
            Debug.Log(IsPaused ? "[Time] Paused" : "[Time] Resumed");
            OnPauseChanged?.Invoke(IsPaused);
        }

        public void SetSpeed(float speedMultiplier)
        {
            if (Mathf.Approximately(CurrentSpeedMultiplier, speedMultiplier)) return;

            CurrentSpeedMultiplier = speedMultiplier;
            if (IsPaused)
            {
                SetPause(false); // Changing speed unpauses
            }

            Debug.Log($"[Time] Speed → {speedMultiplier}x");
            OnSpeedChanged?.Invoke(CurrentSpeedMultiplier);
        }

        public string GetFormattedTime()
        {
            return $"{CurrentHour:D2}:{CurrentMinute:D2}";
        }

        /// <summary>Called by SaveManager when loading a save.</summary>
        public void LoadState(int day, int hour, int minute, float speed)
        {
            CurrentDay    = day;
            CurrentHour   = hour;
            CurrentMinute = minute;
            _minuteAccumulator = 0f;
            CurrentSpeedMultiplier = speed;

            OnTimeChanged?.Invoke(CurrentHour, CurrentMinute);
            OnDayChanged?.Invoke(CurrentDay);
            OnSpeedChanged?.Invoke(CurrentSpeedMultiplier);
        }

        /// <summary>Resets time state to default starting values for a new game.</summary>
        public void ResetState()
        {
            CurrentDay             = startDay;
            CurrentHour            = startHour;
            CurrentMinute          = startMinute;
            _minuteAccumulator     = 0f;
            CurrentSpeedMultiplier = 1.0f;
            IsPaused               = true;

            OnTimeChanged?.Invoke(CurrentHour, CurrentMinute);
            OnDayChanged?.Invoke(CurrentDay);
            OnSpeedChanged?.Invoke(CurrentSpeedMultiplier);
            OnPauseChanged?.Invoke(IsPaused);
            Debug.Log($"[GameTimeManager] Reset to Day {CurrentDay} {GetFormattedTime()} (1.0x, Paused)");
        }
    }
}
