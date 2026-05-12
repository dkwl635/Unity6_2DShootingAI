// Attach to: CoinSystem GameObject (Game scene only — no DontDestroyOnLoad)
using System;
using UnityEngine;
using ShooterGame.Core;

namespace ShooterGame.Economy
{
    public class CoinSystem : MonoBehaviour
    {
        public static CoinSystem Instance { get; private set; }

        public event Action<int> OnCoinChanged;

        public int Total { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (InGameManager.Instance != null)
                InGameManager.Instance.OnGameStart += ResetCoins;
        }

        public void Add(int amount)
        {
            if (amount <= 0) return;
            Total += amount;
            OnCoinChanged?.Invoke(Total);
        }

        private void ResetCoins()
        {
            Total = 0;
            OnCoinChanged?.Invoke(Total);
        }

        private void OnDestroy()
        {
            if (InGameManager.Instance != null)
                InGameManager.Instance.OnGameStart -= ResetCoins;
            if (Instance == this) Instance = null;
        }
    }
}
