// Attach to: UpgradeManager GameObject (Game scene only — no DontDestroyOnLoad)
using System.Collections.Generic;
using UnityEngine;
using ShooterGame.Economy;
using ShooterGame.Player;
using ShooterGame.UI;

namespace ShooterGame.Upgrade
{
    public class UpgradeManager : MonoBehaviour
    {
        public static UpgradeManager Instance { get; private set; }

        [SerializeField] private List<UpgradeData> upgradePool;
        [SerializeField] private LevelUpPanel      levelUpPanel;
        [SerializeField] private PlayerShooter     playerShooter;
        [SerializeField] private PlayerStats       playerStats;

        private readonly List<UpgradeData> _eligible = new List<UpgradeData>(4);

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (ExpSystem.Instance != null)
                ExpSystem.Instance.OnLevelUp += HandleLevelUp;
        }

        private void HandleLevelUp(int newLevel)
        {
            UpgradeData[] picks = PickRandom(3);
            levelUpPanel.Show(picks);
        }

        private UpgradeData[] PickRandom(int count)
        {
            _eligible.Clear();
            _eligible.AddRange(upgradePool);

            UpgradeData[] result = new UpgradeData[count];
            for (int i = 0; i < count && _eligible.Count > 0; i++)
            {
                int totalWeight = 0;
                foreach (UpgradeData d in _eligible) totalWeight += d.Weight;

                int roll        = Random.Range(0, totalWeight);
                int accumulated = 0;
                for (int j = 0; j < _eligible.Count; j++)
                {
                    accumulated += _eligible[j].Weight;
                    if (roll < accumulated)
                    {
                        result[i] = _eligible[j];
                        _eligible.RemoveAt(j);
                        break;
                    }
                }
            }
            return result;
        }

        public void ApplyUpgrade(UpgradeData data)
        {
            switch (data.Type)
            {
                case UpgradeType.AttackSpeed:
                    playerShooter.IncreaseFireRate(data.Value);
                    break;
                case UpgradeType.Damage:
                    playerShooter.IncreaseDamage((int)data.Value);
                    break;
                case UpgradeType.Shield:
                    playerStats.IncreaseMaxHp((int)data.Value);
                    break;
                case UpgradeType.Magnet:
                    MagnetEffect.Instance?.IncreaseRadius(data.Value);
                    break;
            }
            levelUpPanel.Hide();
        }

        private void OnDestroy()
        {
            if (ExpSystem.Instance != null)
                ExpSystem.Instance.OnLevelUp -= HandleLevelUp;
            if (Instance == this) Instance = null;
        }
    }
}
