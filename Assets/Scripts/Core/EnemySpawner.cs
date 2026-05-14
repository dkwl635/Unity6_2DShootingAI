// Attach to: EnemySpawner GameObject (Game scene only — no DontDestroyOnLoad)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ShooterGame.Enemy;
using ShooterGame.Utils;

namespace ShooterGame.Core
{
    public class EnemySpawner : MonoBehaviour
    {
        public static EnemySpawner Instance { get; private set; }

        [SerializeField] private EnemyBase enemyPrefab;
        [SerializeField] private EnemyData enemyData;
        [SerializeField] private float     spawnOffsetY = 1f;

        private ObjectPool<EnemyBase> _pool;
        private List<EnemyBase>       _activeEnemies = new List<EnemyBase>();
        private Coroutine             _spawnCoroutine;
        private bool                  _spawning;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _pool = new ObjectPool<EnemyBase>(enemyPrefab, Constants.POOL_SIZE_ENEMY, transform);
        }

        private void Start()
        {
            if (InGameManager.Instance != null)
            {
                InGameManager.Instance.OnGameStart += StartSpawning;
                InGameManager.Instance.OnGameOver  += StopSpawning;

                if (InGameManager.Instance.IsGameRunning)
                    StartSpawning();
            }
        }

        private void StartSpawning()
        {
            if (_spawning) return;
            _spawning       = true;
            _spawnCoroutine = StartCoroutine(SpawnLoop());
        }

        private void StopSpawning()
        {
            _spawning = false;
            if (_spawnCoroutine != null)
            {
                StopCoroutine(_spawnCoroutine);
                _spawnCoroutine = null;
            }
        }

        // Temporary pause during boss phase — does not unsubscribe lifecycle events
        public void PauseSpawning()
        {
            if (!_spawning) return;
            _spawning = false;
            if (_spawnCoroutine != null)
            {
                StopCoroutine(_spawnCoroutine);
                _spawnCoroutine = null;
            }
        }

        public void ResumeSpawning()
        {
            StartSpawning();
        }

        private IEnumerator SpawnLoop()
        {
            float timer = 0f;
            while (_spawning)
            {
                timer += Time.deltaTime;
                float interval = DifficultyManager.Instance != null
                    ? DifficultyManager.Instance.SpawnInterval
                    : 2f;

                if (timer >= interval)
                {
                    SpawnEnemy();
                    timer = 0f;
                }
                yield return null;
            }
        }

        private void SpawnEnemy()
        {
            float x = Random.Range(
                -Constants.PLAY_HALF_WIDTH  + Constants.SCREEN_BOUND_MARGIN,
                 Constants.PLAY_HALF_WIDTH  - Constants.SCREEN_BOUND_MARGIN);
            float y = Constants.PLAY_HALF_HEIGHT + spawnOffsetY;

            EnemyBase enemy = _pool.Get();
            enemy.transform.position = new Vector3(x, y, 0f);

            float hpMult    = DifficultyManager.Instance?.EnemyHpMultiplier    ?? 1f;
            float speedMult = DifficultyManager.Instance?.EnemySpeedMultiplier ?? 1f;

            enemy.Initialize(enemyData, hpMult, speedMult, ReleaseEnemy);
            _activeEnemies.Add(enemy);
        }

        private void ReleaseEnemy(EnemyBase enemy)
        {
            _activeEnemies.Remove(enemy);
            _pool.Release(enemy);
        }

        private void OnDestroy()
        {
            StopSpawning();
            _pool.ReleaseAll(_activeEnemies);

            if (InGameManager.Instance != null)
            {
                InGameManager.Instance.OnGameStart -= StartSpawning;
                InGameManager.Instance.OnGameOver  -= StopSpawning;
            }

            if (Instance == this) Instance = null;
        }
    }
}
