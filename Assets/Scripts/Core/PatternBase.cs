// Attach to: Pattern prefab root (instantiated at runtime by PatternManager)
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ShooterGame.Enemy;
using ShooterGame.Utils;

namespace ShooterGame.Core
{
    public abstract class PatternBase : MonoBehaviour
    {
        public event Action OnPatternComplete;

        protected PatternConfig Config { get; private set; }

        private ObjectPool<EnemyBase>      _pool;
        protected readonly List<EnemyBase> ActiveEnemies = new List<EnemyBase>();
        private bool           _completed;
        private Coroutine      _durationCoroutine;
        private WaitForSeconds _durationWait;

        public void StartPattern(PatternConfig config)
        {
            Config        = config;
            _pool         = new ObjectPool<EnemyBase>(config.EnemyPrefab, config.EnemyCount, transform);
            _durationWait = new WaitForSeconds(config.PatternDuration);

            ArrangeEnemies();
            _durationCoroutine = StartCoroutine(DurationTimer());
        }

        protected abstract void ArrangeEnemies();
        protected virtual void UpdateMovement() { }

        private void Update()
        {
            if (Config == null) return;
            UpdateMovement();
        }

        protected EnemyBase GetEnemy()
        {
            float hpMult    = DifficultyManager.Instance?.EnemyHpMultiplier    ?? 1f;
            float speedMult = DifficultyManager.Instance?.EnemySpeedMultiplier ?? 1f;

            EnemyBase enemy = _pool.Get();
            enemy.Initialize(Config.EnemyData, hpMult, speedMult, OnEnemyReleased);
            ActiveEnemies.Add(enemy);
            return enemy;
        }

        // Called by enemy's own death/offscreen path via _releaseCallback
        private void OnEnemyReleased(EnemyBase enemy)
        {
            if (!ActiveEnemies.Contains(enemy)) return;
            ActiveEnemies.Remove(enemy);
            _pool.Release(enemy);
            if (ActiveEnemies.Count == 0) Complete();
        }

        // Called by subclasses for manual bounds checks (ScreenSweep X, CircleTrap center)
        protected void ReleaseEnemyManual(EnemyBase enemy)
        {
            if (!ActiveEnemies.Contains(enemy)) return;
            enemy.ForceReturnToPool();
            ActiveEnemies.Remove(enemy);
            _pool.Release(enemy);
            if (ActiveEnemies.Count == 0) Complete();
        }

        // Called by PatternManager when boss overrides or game ends
        public void ForceComplete()
        {
            StopAllCoroutines();
            List<EnemyBase> snapshot = new List<EnemyBase>(ActiveEnemies);
            ActiveEnemies.Clear();
            foreach (EnemyBase e in snapshot)
            {
                e.ForceReturnToPool();
                _pool.Release(e);
            }
            Complete();
        }

        private IEnumerator DurationTimer()
        {
            yield return _durationWait;
            if (!_completed) ForceComplete();
        }

        private void Complete()
        {
            if (_completed) return;
            _completed = true;
            if (_durationCoroutine != null)
            {
                StopCoroutine(_durationCoroutine);
                _durationCoroutine = null;
            }
            OnPatternComplete?.Invoke();
        }

        private void OnDestroy()
        {
            _pool?.ReleaseAll(ActiveEnemies);
        }
    }
}
