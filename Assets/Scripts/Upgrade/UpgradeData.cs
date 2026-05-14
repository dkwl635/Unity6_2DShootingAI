// Attach to: (ScriptableObject — no GameObject attachment needed)
using UnityEngine;

namespace ShooterGame.Upgrade
{
    [CreateAssetMenu(fileName = "UpgradeData", menuName = "ShooterGame/Upgrade Data")]
    public class UpgradeData : ScriptableObject
    {
        [SerializeField] private string      upgradeName = "업그레이드";
        [SerializeField] private Sprite      icon;
        [SerializeField] private UpgradeType type;
        [SerializeField] private float       value    = 1f;
        [SerializeField] private int         weight   = 10;
        [SerializeField] private int         maxLevel = 0;  // 0 = 박스 표시 안 함

        public string      Name     => upgradeName;
        public Sprite      Icon     => icon;
        public UpgradeType Type     => type;
        public float       Value    => value;
        public int         Weight   => weight;
        public int         MaxLevel => maxLevel;
    }
}
