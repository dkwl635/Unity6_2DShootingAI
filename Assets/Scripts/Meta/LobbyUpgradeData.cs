// Attach to: (ScriptableObject — no GameObject attachment needed)
using UnityEngine;

namespace ShooterGame.Meta
{
    [CreateAssetMenu(fileName = "LobbyUpgrade", menuName = "ShooterGame/Lobby Upgrade Data")]
    public class LobbyUpgradeData : ScriptableObject
    {
        [SerializeField] private LobbyUpgradeType _upgradeType;
        [SerializeField] private string           _displayName    = "업그레이드";
        [SerializeField] private int              _maxLevel       = 5;
        [SerializeField] private int              _baseCost       = 100;
        [SerializeField] private float            _costMultiplier = 2f;
        [SerializeField] private float            _gainPerLevel   = 1f;

        public LobbyUpgradeType UpgradeType  => _upgradeType;
        public string           DisplayName  => _displayName;
        public int              MaxLevel     => _maxLevel;
        public float            GainPerLevel => _gainPerLevel;

        /// <summary>currentLevel은 현재 레벨 (0 = 아무것도 안 산 상태).</summary>
        public int GetCostForLevel(int currentLevel)
            => Mathf.RoundToInt(_baseCost * Mathf.Pow(_costMultiplier, currentLevel));

        /// <summary>level 레벨 구매 후 누적 스탯 증가량.</summary>
        public float GetTotalGain(int level)
            => _gainPerLevel * level;
    }
}
