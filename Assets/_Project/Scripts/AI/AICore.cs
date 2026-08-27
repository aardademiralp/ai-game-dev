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
        Reliability
    }

    /// <summary>
    /// Central AI System data model representing the company's core AI model capabilities (0-100).
    /// </summary>
    public class AICore : MonoBehaviour
    {
        public static AICore Instance { get; private set; }

        [Header("AI System Stats (0–100)")]
        [Range(0, 100)] [SerializeField] private int quality     = 20;
        [Range(0, 100)] [SerializeField] private int speed       = 15;
        [Range(0, 100)] [SerializeField] private int reasoning   = 10;
        [Range(0, 100)] [SerializeField] private int creativity  = 12;
        [Range(0, 100)] [SerializeField] private int reliability = 25;

        public int Quality     => quality;
        public int Speed       => speed;
        public int Reasoning   => reasoning;
        public int Creativity  => creativity;
        public int Reliability => reliability;

        public event Action<AIStatType, int, int> OnAIStatChanged; // (statType, oldValue, newValue)

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public int GetStat(AIStatType statType)
        {
            switch (statType)
            {
                case AIStatType.Quality:     return quality;
                case AIStatType.Speed:       return speed;
                case AIStatType.Reasoning:   return reasoning;
                case AIStatType.Creativity:  return creativity;
                case AIStatType.Reliability: return reliability;
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
            }

            Debug.Log($"[AICore] Improved {statType} from {oldVal} → {newVal}");
            OnAIStatChanged?.Invoke(statType, oldVal, newVal);
        }
    }
}
