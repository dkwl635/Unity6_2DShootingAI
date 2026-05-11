// Attach to: (ScriptableObject — no GameObject attachment needed)
using UnityEngine;

namespace ShooterGame.Enemy
{
    [CreateAssetMenu(fileName = "EnemyData", menuName = "ShooterGame/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [SerializeField] public int   baseHp        = 30;
        [SerializeField] public float moveSpeed     = 3f;
        [SerializeField] public int   scoreValue    = 10;
        [SerializeField] public int   contactDamage = 1;
    }
}
