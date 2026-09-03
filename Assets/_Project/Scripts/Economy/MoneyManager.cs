using UnityEngine;
using System;

namespace GameDevStudio.Economy
{
    /// <summary>
    /// Central money manager for the studio simulation.
    /// Manages balance, spending validation, and fires events on balance changes.
    /// </summary>
    public class MoneyManager : MonoBehaviour
    {
        public static MoneyManager Instance { get; private set; }

        [Header("Starting Economy")]
        [SerializeField] private int startingMoney = 10000;

        // ── Public State ──────────────────────────────────────────────────
        public int CurrentMoney { get; private set; }

        // ── Events ────────────────────────────────────────────────────────
        public event Action<int> OnMoneyChanged;

        // ─────────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            CurrentMoney = startingMoney;
        }

        private void Start()
        {
            OnMoneyChanged?.Invoke(CurrentMoney);
        }

        // ── Public API ────────────────────────────────────────────────────
        public void AddMoney(int amount)
        {
            if (amount <= 0) return;
            CurrentMoney += amount;
            Debug.Log($"[Money] Added ${amount} → Balance: ${CurrentMoney}");
            OnMoneyChanged?.Invoke(CurrentMoney);
        }

        public bool HasEnoughMoney(int amount)
        {
            return CurrentMoney >= amount;
        }

        public bool TrySpendMoney(int amount)
        {
            if (amount <= 0) return true;

            if (HasEnoughMoney(amount))
            {
                CurrentMoney -= amount;
                Debug.Log($"[Money] Spent ${amount} → Balance: ${CurrentMoney}");
                OnMoneyChanged?.Invoke(CurrentMoney);
                return true;
            }

            Debug.Log($"[Money] Cannot spend ${amount} — Insufficient funds! Balance: ${CurrentMoney}");
            return false;
        }

        /// <summary>Called by SaveManager when loading a save. Overwrites balance directly.</summary>
        public void SetMoney(int amount)
        {
            CurrentMoney = Mathf.Max(0, amount);
            OnMoneyChanged?.Invoke(CurrentMoney);
        }
    }
}
