// Attach to: CircleTrap Pattern prefab root
using UnityEngine;
using ShooterGame.Enemy;
using ShooterGame.Utils;

namespace ShooterGame.Core
{
    public class CircleTrapPattern : PatternBase
    {
        [SerializeField] private float spawnRadius   = 4f;
        [SerializeField] private float closeSpeed    = 2f;
        [SerializeField] private float releaseRadius = 0.5f;

        protected override void ArrangeEnemies()
        {
            float step = 360f / Config.EnemyCount;
            for (int i = 0; i < Config.EnemyCount; i++)
            {
                float angle     = i * step * Mathf.Deg2Rad;
                float x         = Mathf.Cos(angle) * spawnRadius;
                float y         = Mathf.Sin(angle) * spawnRadius;
                EnemyBase enemy = GetEnemy();
                enemy.transform.position = new Vector3(x, y, 0f);
            }
        }

        protected override void UpdateMovement()
        {
            for (int i = ActiveEnemies.Count - 1; i >= 0; i--)
            {
                EnemyBase enemy = ActiveEnemies[i];
                enemy.transform.position = Vector3.MoveTowards(
                    enemy.transform.position, Vector3.zero, closeSpeed * Time.deltaTime);

                if (enemy.transform.position.magnitude < releaseRadius)
                    ReleaseEnemyManual(enemy);
            }
        }
    }
}
