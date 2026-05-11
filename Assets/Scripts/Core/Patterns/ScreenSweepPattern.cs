// Attach to: ScreenSweep Pattern prefab root
using UnityEngine;
using ShooterGame.Enemy;
using ShooterGame.Utils;

namespace ShooterGame.Core
{
    public class ScreenSweepPattern : PatternBase
    {
        [SerializeField] private float sweepSpeed   = 4f;
        [SerializeField] private float enemySpacing = 1.5f;

        private Vector3 _sweepDir;

        protected override void ArrangeEnemies()
        {
            bool leftToRight = Random.value > 0.5f;
            _sweepDir        = leftToRight ? Vector3.right : Vector3.left;

            float startX = leftToRight
                ? -(Constants.PLAY_HALF_WIDTH + enemySpacing * Config.EnemyCount)
                :   Constants.PLAY_HALF_WIDTH + enemySpacing * Config.EnemyCount;

            float y = Random.Range(-Constants.PLAY_HALF_HEIGHT * 0.5f, Constants.PLAY_HALF_HEIGHT * 0.5f);

            for (int i = 0; i < Config.EnemyCount; i++)
            {
                EnemyBase enemy = GetEnemy();
                float x         = leftToRight
                    ? startX + i * enemySpacing
                    : startX - i * enemySpacing;
                enemy.transform.position = new Vector3(x, y, 0f);
            }
        }

        protected override void UpdateMovement()
        {
            float exitX = Constants.PLAY_HALF_WIDTH + 2f;
            for (int i = ActiveEnemies.Count - 1; i >= 0; i--)
            {
                EnemyBase enemy = ActiveEnemies[i];
                enemy.transform.Translate(_sweepDir * sweepSpeed * Time.deltaTime);

                // Only release when crossing the DESTINATION edge (not spawn edge)
                bool exitedRight = _sweepDir == Vector3.right && enemy.transform.position.x > exitX;
                bool exitedLeft  = _sweepDir == Vector3.left  && enemy.transform.position.x < -exitX;

                if (exitedRight || exitedLeft)
                    ReleaseEnemyManual(enemy);
            }
        }
    }
}
