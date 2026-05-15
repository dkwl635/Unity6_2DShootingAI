// Attach to: (ScriptableObject — no GameObject attachment needed)
using UnityEngine;

namespace ShooterGame.Enemy
{
    [CreateAssetMenu(fileName = "EnemyData", menuName = "ShooterGame/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [SerializeField] private int   baseHp        = 30;
        [SerializeField] private float moveSpeed     = 3f;
        [SerializeField] private int   scoreValue    = 10;
        [SerializeField] private int   contactDamage = 1;

        [Header("Drops")]
        [SerializeField] private int   coinDrop        = 1;
        [SerializeField] private int   coinDropCount   = 1;
        [SerializeField] [Range(0f, 1f)] private float coinDropChance  = 1f;
        [SerializeField] private float coinDropRadius  = 0.3f;
        [SerializeField] private int   powerDrop        = 5;
        [SerializeField] private int   powerDropCount   = 1;
        [SerializeField] [Range(0f, 1f)] private float powerDropChance = 1f;
        [SerializeField] private float powerDropRadius  = 0.3f;

        public int   BaseHp           => baseHp;
        public float MoveSpeed        => moveSpeed;
        public int   ScoreValue       => scoreValue;
        public int   ContactDamage    => contactDamage;
        public int   CoinDrop         => coinDrop;
        public int   CoinDropCount    => coinDropCount;
        public float CoinDropChance   => coinDropChance;
        public float CoinDropRadius   => coinDropRadius;
        public int   PowerDrop        => powerDrop;
        public int   PowerDropCount   => powerDropCount;
        public float PowerDropChance  => powerDropChance;
        public float PowerDropRadius  => powerDropRadius;
    }
}
