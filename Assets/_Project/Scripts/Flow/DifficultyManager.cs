using UnityEngine;

namespace GameDevStudio.Flow
{
    /// <summary>
    /// Singleton that holds the current game session's DifficultyData.
    /// Other systems call DifficultyManager.Current.ResearchSpeedMultiplier etc.
    /// </summary>
    public class DifficultyManager : MonoBehaviour
    {
        public static DifficultyManager Instance { get; private set; }

        public DifficultyData Current { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // Default to standard difficulty until a game is started
            Current = DifficultyData.ForType(DifficultyType.BizHallederiz);
        }

        public void SetDifficulty(DifficultyType type)
        {
            Current = DifficultyData.ForType(type);
            Debug.Log($"[Difficulty] Set to: {type} (StartMoney={Current.StartingMoney}, ResearchSpeed={Current.ResearchSpeedMultiplier}x)");
        }

        public void SetDifficulty(DifficultyData data)
        {
            Current = data;
        }
    }
}
