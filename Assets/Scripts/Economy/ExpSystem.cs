// Attach to: ExpSystem GameObject (Game scene only — no DontDestroyOnLoad)
// Level-up curve: expToNextLevel = baseExpPerLevel * currentLevel
using System;
using UnityEngine;
using ShooterGame.Core;

namespace ShooterGame.Economy
{
    public class ExpSystem : MonoBehaviour
    {
        public static ExpSystem Instance { get; private set; }

        public event Action<int, int> OnExpChanged;  // (currentExp, expToNext)
        public event Action<int>      OnLevelUp;     // (newLevel) — Part 3 hook

        [SerializeField] private int baseExpPerLevel = 10;

        public int CurrentExp   { get; private set; }
        public int CurrentLevel { get; private set; } = 1;
        public int ExpToNext    => baseExpPerLevel * CurrentLevel;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (InGameManager.Instance != null)
                InGameManager.Instance.OnGameStart += ResetExp;
        }

        public void Add(int amount)
        {
            if (amount <= 0) return;
            CurrentExp += amount;
            OnExpChanged?.Invoke(CurrentExp, ExpToNext);

            while (CurrentExp >= ExpToNext)
            {
                CurrentExp -= ExpToNext;
                CurrentLevel++;
                OnLevelUp?.Invoke(CurrentLevel);
                OnExpChanged?.Invoke(CurrentExp, ExpToNext);
            }
        }

        private void ResetExp()
        {
            CurrentExp   = 0;
            CurrentLevel = 1;
            OnExpChanged?.Invoke(CurrentExp, ExpToNext);
        }

        private void OnDestroy()
        {
            if (InGameManager.Instance != null)
                InGameManager.Instance.OnGameStart -= ResetExp;
            if (Instance == this) Instance = null;
        }
    }
}
