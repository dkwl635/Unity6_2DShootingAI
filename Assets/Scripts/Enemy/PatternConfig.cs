// Attach to: (ScriptableObject — no GameObject attachment needed)
using UnityEngine;

namespace ShooterGame.Enemy
{
    public enum PatternType { ScreenSweep, CircleTrap, MeteorShower, MiniBoss }

    [CreateAssetMenu(fileName = "PatternConfig", menuName = "ShooterGame/Pattern Config")]
    public class PatternConfig : ScriptableObject
    {
        [SerializeField] private PatternType patternType    = PatternType.ScreenSweep;
        [SerializeField] private EnemyBase   enemyPrefab;
        [SerializeField] private EnemyData   enemyData;
        [SerializeField] private int         enemyCount      = 5;
        [SerializeField] private float       unlockTime      = 0f;
        [SerializeField] private float       patternDuration = 8f;

        public PatternType Kind            => patternType;
        public EnemyBase   EnemyPrefab     => enemyPrefab;
        public EnemyData   EnemyData       => enemyData;
        public int         EnemyCount      => enemyCount;
        public float       UnlockTime      => unlockTime;
        public float       PatternDuration => patternDuration;
    }
}
