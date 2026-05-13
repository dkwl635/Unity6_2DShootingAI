// Attach to: LobbyUpgradeManager GameObject (Lobby scene only — no DontDestroyOnLoad)
using System;
using UnityEngine;

namespace ShooterGame.Meta
{
    public class LobbyUpgradeManager : MonoBehaviour
    {
        public static LobbyUpgradeManager Instance { get; private set; }

        // 배열 인덱스 = (int)LobbyUpgradeType 순서와 일치해야 함
        [SerializeField] private LobbyUpgradeData[] _upgrades;

        public event Action OnUpgradeChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public LobbyUpgradeData GetData(LobbyUpgradeType type) => _upgrades[(int)type];

        public int GetCurrentLevel(LobbyUpgradeType type)
            => SaveManager.Instance.GetUpgradeLevel(type);

        /// <summary>구매 시도. 성공하면 true 반환 + OnUpgradeChanged 발행.</summary>
        public bool TryPurchase(LobbyUpgradeType type)
        {
            LobbyUpgradeData data = _upgrades[(int)type];
            int currentLevel      = GetCurrentLevel(type);

            if (currentLevel >= data.MaxLevel) return false;

            int cost = data.GetCostForLevel(currentLevel);
            if (SaveManager.Instance.TotalCoins < cost) return false;

            SaveManager.Instance.SpendCoins(cost);
            SaveManager.Instance.SetUpgradeLevel(type, currentLevel + 1);
            OnUpgradeChanged?.Invoke();
            return true;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
