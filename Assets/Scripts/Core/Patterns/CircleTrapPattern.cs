// Attach to: CircleTrap Pattern prefab root
using System.Collections.Generic;
using UnityEngine;
using ShooterGame.Enemy;
using ShooterGame.Utils;

namespace ShooterGame.Core
{
    public class CircleTrapPattern : PatternBase
    {
        [SerializeField] private float spawnHalfWidth = 4f;   // 수평 분산 폭
        [SerializeField] private float targetY        = 7.5f; // 하강 목표 Y
        [SerializeField] private float descentSpeed   = 5f;   // 하강 속도
        [SerializeField] private float waitDuration   = 1f;   // 대기 시간(초)
        [SerializeField] private float fireInterval   = 0.5f; // 기체 간 발사 간격
        [SerializeField] private float flySpeed       = 6f;   // 돌진 속도

        private enum Phase { Descending, Waiting, Firing }

        private Phase                          _phase;
        private float                          _timer;
        private int                            _nextFireIndex;
        private GameObject                     _playerGO;
        private readonly List<EnemyBase>       _ordered    = new List<EnemyBase>();
        private readonly HashSet<EnemyBase>    _launched   = new HashSet<EnemyBase>();
        private readonly Dictionary<EnemyBase, Vector3> _dirs = new Dictionary<EnemyBase, Vector3>();

        protected override void ArrangeEnemies()
        {
            _phase         = Phase.Descending;
            _timer         = 0f;
            _nextFireIndex = 0;
            _ordered.Clear();
            _launched.Clear();
            _dirs.Clear();

            int   count = Config.EnemyCount;
            float step  = count > 1 ? spawnHalfWidth * 2f / (count - 1) : 0f;

            for (int i = 0; i < count; i++)
            {
                float     x     = count > 1 ? -spawnHalfWidth + step * i : 0f;
                EnemyBase enemy = GetEnemy();
                enemy.transform.position = new Vector3(x, Constants.PLAY_HALF_HEIGHT + 1f, 0f);
                _ordered.Add(enemy);
            }
        }

        protected override void UpdateMovement()
        {
            switch (_phase)
            {
                case Phase.Descending: UpdateDescending(); break;
                case Phase.Waiting:   UpdateWaiting();    break;
                case Phase.Firing:    UpdateFiring();     break;
            }
        }

        // ── Phase 1: 하강 ─────────────────────────────────────────

        private void UpdateDescending()
        {
            bool allReached = true;

            foreach (EnemyBase enemy in _ordered)
            {
                if (!ActiveEnemies.Contains(enemy)) continue;

                Vector3 pos = enemy.transform.position;
                if (Mathf.Abs(pos.y - targetY) > 0.05f)
                {
                    allReached = false;
                    Vector3 dest = new Vector3(pos.x, targetY, 0f);
                    enemy.transform.position = Vector3.MoveTowards(pos, dest, descentSpeed * Time.deltaTime);
                }
                else
                {
                    // X 고정, Y 정렬
                    enemy.transform.position = new Vector3(pos.x, targetY, 0f);
                }
            }

            if (allReached)
            {
                _phase     = Phase.Waiting;
                _timer     = 0f;
                _playerGO  = GameObject.FindWithTag(Constants.TAG_PLAYER);
            }
        }

        // ── Phase 2: 대기 ─────────────────────────────────────────

        private void UpdateWaiting()
        {
            _timer += Time.deltaTime;
            if (_timer >= waitDuration)
            {
                _phase = Phase.Firing;
                _timer = fireInterval; // 대기 끝나자마자 첫 기체 즉시 발사
            }
        }

        // ── Phase 3: 순차 발사 ────────────────────────────────────

        private void UpdateFiring()
        {
            _timer += Time.deltaTime;
            while (_timer >= fireInterval)
            {
                _timer -= fireInterval;
                LaunchNext();
            }

            // 이미 발사된 기체 이동
            for (int i = ActiveEnemies.Count - 1; i >= 0; i--)
            {
                EnemyBase enemy = ActiveEnemies[i];
                if (_launched.Contains(enemy) && _dirs.TryGetValue(enemy, out Vector3 dir))
                    enemy.transform.position += dir * flySpeed * Time.deltaTime;
            }
        }

        private void LaunchNext()
        {
            while (_nextFireIndex < _ordered.Count)
            {
                EnemyBase enemy = _ordered[_nextFireIndex++];
                if (!ActiveEnemies.Contains(enemy)) continue; // 이미 사망

                // 이 순간의 플레이어 위치를 각자 개별 조준
                Vector3 target = _playerGO != null
                    ? _playerGO.transform.position
                    : Vector3.zero;

                Vector3 raw = target - enemy.transform.position;
                _dirs[enemy] = raw.sqrMagnitude > 0f ? raw.normalized : Vector3.down;
                _launched.Add(enemy);
                break;
            }
        }
    }
}
