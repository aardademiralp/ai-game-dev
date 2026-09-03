using UnityEngine;
using System;

namespace GameDevStudio.AI
{
    public enum AIStatType
    {
        Quality,
        Speed,
        Reasoning,
        Creativity,
        Reliability,
        Learning      // NEW — represents AI learning ability
    }

    /// <summary>
    /// Central AI System data model.
    /// Tracks capability stats (0-100), Compute Capacity, and Energy Capacity.
    /// Stats are improved by technology effects, not by individual employee tasks.
    /// </summary>
    public class AICore : MonoBehaviour
    {
        public static AICore Instance { get; private set; }

        [Header("AI Capability Stats (0–100)")]
        [Range(0, 100)] [SerializeField] private int quality     = 20;
        [Range(0, 100)] [SerializeField] private int speed       = 15;
        [Range(0, 100)] [SerializeField] private int reasoning   = 10;
        [Range(0, 100)] [SerializeField] private int creativity  = 12;
        [Range(0, 100)] [SerializeField] private int reliability = 25;
        [Range(0, 100)] [SerializeField] private int learning    = 5;

        [Header("Infrastructure")]
        [SerializeField] private int computeCapacity = 10;  // Base compute available
        [SerializeField] private int energyCapacity  = 20;  // Base energy available

        // ── Public read-only properties ───────────────────────────────────────
        public int Quality           => quality;
        public int Speed             => speed;
        public int Reasoning         => reasoning;
        public int Creativity        => creativity;
        public int Reliability       => reliability;
        public int Learning          => learning;
        public int CurrentComputeCapacity => computeCapacity;
        public int CurrentEnergyCapacity  => energyCapacity;

        // ── Events ────────────────────────────────────────────────────────────
        public event Action<AIStatType, int, int> OnAIStatChanged;   // (stat, oldVal, newVal)
        public event Action<int>                  OnComputeChanged;  // newCapacity
        public event Action<int>                  OnEnergyChanged;   // newCapacity

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ── Stat access ───────────────────────────────────────────────────────
        public int GetStat(AIStatType statType)
        {
            switch (statType)
            {
                case AIStatType.Quality:     return quality;
                case AIStatType.Speed:       return speed;
                case AIStatType.Reasoning:   return reasoning;
                case AIStatType.Creativity:  return creativity;
                case AIStatType.Reliability: return reliability;
                case AIStatType.Learning:    return learning;
                default: return 0;
            }
        }

        public void ImproveStat(AIStatType statType, int amount = 1)
        {
            int oldVal = GetStat(statType);
            int newVal = Mathf.Clamp(oldVal + amount, 0, 100);

            switch (statType)
            {
                case AIStatType.Quality:     quality     = newVal; break;
                case AIStatType.Speed:       speed       = newVal; break;
                case AIStatType.Reasoning:   reasoning   = newVal; break;
                case AIStatType.Creativity:  creativity  = newVal; break;
                case AIStatType.Reliability: reliability = newVal; break;
                case AIStatType.Learning:    learning    = newVal; break;
            }

            Debug.Log($"[AICore] {statType}: {oldVal} → {newVal}");
            OnAIStatChanged?.Invoke(statType, oldVal, newVal);
        }

        // ── Infrastructure ────────────────────────────────────────────────────
        public void AddComputeCapacity(int amount)
        {
            computeCapacity += amount;
            Debug.Log($"[AICore] Compute Capacity → {computeCapacity}");
            OnComputeChanged?.Invoke(computeCapacity);
        }

        public void AddEnergyCapacity(int amount)
        {
            energyCapacity += amount;
            Debug.Log($"[AICore] Energy Capacity → {energyCapacity}");
            OnEnergyChanged?.Invoke(energyCapacity);
        }

        /// <summary>Called by SaveManager when loading a save. Overwrites all stats directly.</summary>
        public void LoadStats(Save.AICoreSaveData d)
        {
            quality          = Mathf.Clamp(d.Quality,         0, 100);
            speed            = Mathf.Clamp(d.Speed,           0, 100);
            reasoning        = Mathf.Clamp(d.Reasoning,       0, 100);
            creativity       = Mathf.Clamp(d.Creativity,      0, 100);
            reliability      = Mathf.Clamp(d.Reliability,     0, 100);
            learning         = Mathf.Clamp(d.Learning,        0, 100);
            computeCapacity  = d.ComputeCapacity;
            energyCapacity   = d.EnergyCapacity;

            OnComputeChanged?.Invoke(computeCapacity);
            OnEnergyChanged?.Invoke(energyCapacity);
        }
    }
}
