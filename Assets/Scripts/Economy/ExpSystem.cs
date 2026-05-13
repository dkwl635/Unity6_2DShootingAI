// Attach to: PowerSystem GameObject (Game scene only — no DontDestroyOnLoad)
// Level-up curve: powerToNext = basePowerPerLevel * currentLevel
using System;
using UnityEngine;
using ShooterGame.Core;

namespace ShooterGame.Economy
{
    public class PowerSystem : MonoBehaviour
    {
        public static PowerSystem Instance { get; private set; }

        public event Action<int, int> OnPowerChanged;  // (currentPower, powerToNext)
        public event Action<int>      OnLevelUp;       // (newLevel)

        [SerializeField] private int basePowerPerLevel = 10;

        public int CurrentPower { get; private set; }
        public int CurrentLevel { get; private set; } = 1;
        public int PowerToNext  => basePowerPerLevel * CurrentLevel;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (InGameManager.Instance != null)
                InGameManager.Instance.OnGameStart += ResetPower;
        }

        public void Add(int amount)
        {
            if (amount <= 0) return;
            CurrentPower += amount;
            OnPowerChanged?.Invoke(CurrentPower, PowerToNext);

            while (CurrentPower >= PowerToNext)
            {
                CurrentPower -= PowerToNext;
                CurrentLevel++;
                OnLevelUp?.Invoke(CurrentLevel);
                OnPowerChanged?.Invoke(CurrentPower, PowerToNext);
            }
        }

        private void ResetPower()
        {
            CurrentPower = 0;
            CurrentLevel = 1;
            OnPowerChanged?.Invoke(CurrentPower, PowerToNext);
        }

        private void OnDestroy()
        {
            if (InGameManager.Instance != null)
                InGameManager.Instance.OnGameStart -= ResetPower;
            if (Instance == this) Instance = null;
        }
    }
}
