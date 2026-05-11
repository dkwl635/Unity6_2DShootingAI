// Attach to: MeteorShower Pattern prefab root
using System.Collections;
using UnityEngine;
using ShooterGame.Enemy;
using ShooterGame.Utils;

namespace ShooterGame.Core
{
    public class MeteorShowerPattern : PatternBase
    {
        [SerializeField] private float spawnOffsetY    = 1f;
        [SerializeField] private float staggerInterval = 0.5f;

        private WaitForSeconds _staggerWait;

        private void Awake()
        {
            _staggerWait = new WaitForSeconds(staggerInterval);
        }

        protected override void ArrangeEnemies()
        {
            StartCoroutine(StaggeredSpawn());
        }

        private IEnumerator StaggeredSpawn()
        {
            for (int i = 0; i < Config.EnemyCount; i++)
            {
                float x = Random.Range(
                    -Constants.PLAY_HALF_WIDTH + Constants.SCREEN_BOUND_MARGIN,
                     Constants.PLAY_HALF_WIDTH - Constants.SCREEN_BOUND_MARGIN);
                float y          = Constants.PLAY_HALF_HEIGHT + spawnOffsetY;
                EnemyBase meteor = GetEnemy();
                meteor.transform.position = new Vector3(x, y, 0f);
                yield return _staggerWait;
            }
        }
    }
}
