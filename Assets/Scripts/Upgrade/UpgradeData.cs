// Attach to: (ScriptableObject — no GameObject attachment needed)
using UnityEngine;

namespace ShooterGame.Upgrade
{
    [CreateAssetMenu(fileName = "UpgradeData", menuName = "ShooterGame/Upgrade Data")]
    public class UpgradeData : ScriptableObject
    {
        [SerializeField] private string      upgradeName = "업그레이드";
        [SerializeField] private UpgradeType type;
        [SerializeField] private float       value  = 1f;
        [SerializeField] private int         weight = 10;

        public string      Name   => upgradeName;
        public UpgradeType Type   => type;
        public float       Value  => value;
        public int         Weight => weight;
    }
}
