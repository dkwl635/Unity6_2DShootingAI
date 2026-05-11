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

        public int   BaseHp        => baseHp;
        public float MoveSpeed     => moveSpeed;
        public int   ScoreValue    => scoreValue;
        public int   ContactDamage => contactDamage;
    }
}
