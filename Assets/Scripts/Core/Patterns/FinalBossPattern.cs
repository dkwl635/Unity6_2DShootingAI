// Attach to: FinalBossPattern prefab root
using UnityEngine;
using ShooterGame.Enemy;
using ShooterGame.Utils;

namespace ShooterGame.Core
{
    public class FinalBossPattern : PatternBase
    {
        [SerializeField] private float spawnOffsetY = 1f;

        protected override void ArrangeEnemies()
        {
            EnemyBase boss          = GetEnemy();
            boss.transform.position = new Vector3(0f, Constants.PLAY_HALF_HEIGHT + spawnOffsetY, 0f);
        }
    }
}
