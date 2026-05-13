// Attach to: UpgradeManager GameObject (Game scene only — no DontDestroyOnLoad)
using System.Collections.Generic;
using UnityEngine;
using ShooterGame.Core;
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

        private const int MAX_HP_CAP = 8;

        private readonly List<UpgradeData> _eligible = new List<UpgradeData>(4);

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (PowerSystem.Instance != null)
                PowerSystem.Instance.OnLevelUp += HandleLevelUp;
        }

        private void HandleLevelUp(int newLevel)
        {
            AudioManager.Instance?.PlaySFX(SfxType.LevelUp);
            UpgradeData[] picks = PickRandom(3);
            levelUpPanel.Show(picks);
        }

        private UpgradeData[] PickRandom(int count)
        {
            _eligible.Clear();
            foreach (var d in upgradePool)
            {
                // 최대 체력이 이미 상한이면 해당 업그레이드를 후보에서 제외
                if (d.Type == UpgradeType.MaxHp && playerStats != null && playerStats.MaxHp >= MAX_HP_CAP)
                    continue;
                _eligible.Add(d);
            }

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
                case UpgradeType.MaxHp:
                    if (playerStats != null && playerStats.MaxHp < MAX_HP_CAP)
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
            if (PowerSystem.Instance != null)
                PowerSystem.Instance.OnLevelUp -= HandleLevelUp;
            if (Instance == this) Instance = null;
        }
    }
}
