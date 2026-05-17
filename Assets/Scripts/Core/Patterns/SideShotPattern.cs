// Attach to: SideShot Pattern prefab root
using System.Collections.Generic;
using UnityEngine;
using ShooterGame.Enemy;
using ShooterGame.Utils;

namespace ShooterGame.Core
{
    public class SideShotPattern : PatternBase
    {
        [SerializeField] private float   entrySpeed   = 5f;          // 진입 속도
        [SerializeField] private float   exitSpeed    = 8f;          // 퇴장 속도
        [SerializeField] private float   stopX        = 2f;          // 첫 번째 기체의 정지 X 절댓값 (가장 안쪽)
        [SerializeField] private float   stepX        = 1.5f;        // 계단 X 간격 (뒤로 갈수록 spawn 쪽에 가까워짐)
        [SerializeField] private float   fireInterval = 1f;          // 기체 간 발사 간격(초)
        [SerializeField] private float[] targetYs     = { 4f, 1f, -2f }; // 각 기체별 Y 위치

        private enum Phase { Entering, Firing }

        private Phase      _phase;
        private float      _fireTimer;
        private int        _nextFireIndex;
        private int        _exitSign;     // +1 = 오른쪽 퇴장, -1 = 왼쪽 퇴장
        private float      _exitBound;
        private GameObject _playerGO;

        private readonly List<EnemyBase>              _ordered    = new List<EnemyBase>();
        private readonly Dictionary<EnemyBase, float> _targetXMap = new Dictionary<EnemyBase, float>();
        private readonly HashSet<EnemyBase>           _exiting    = new HashSet<EnemyBase>();

        protected override void ArrangeEnemies()
        {
            _ordered.Clear();
            _targetXMap.Clear();
            _exiting.Clear();

            _phase         = Phase.Entering;
            _fireTimer     = 0f;
            _nextFireIndex = 0;

            bool fromRight = Random.value > 0.5f;
            _exitSign  = fromRight ? 1 : -1;
            _exitBound = Constants.PLAY_HALF_WIDTH + 2f;

            float spawnX = fromRight ? _exitBound : -_exitBound;
            int   count  = Config.EnemyCount;

            for (int i = 0; i < count; i++)
            {
                // 계단식 X: index 0이 가장 안쪽, 이후 stepX씩 spawn 방향으로 후퇴
                float tX = fromRight
                    ? -(stopX - i * stepX)   // 오른쪽 진입: 음수 방향이 안쪽
                    :  (stopX - i * stepX);  // 왼쪽 진입: 양수 방향이 안쪽

                // Y: targetYs 배열 우선, 부족하면 자동 간격
                float tY = i < targetYs.Length
                    ? targetYs[i]
                    : -(i - count * 0.5f) * 2f;

                EnemyBase enemy = GetEnemy();
                enemy.transform.position = new Vector3(spawnX, tY, 0f);
                _targetXMap[enemy] = tX;
                _ordered.Add(enemy);
            }
        }

        protected override void UpdateMovement()
        {
            switch (_phase)
            {
                case Phase.Entering: UpdateEntering(); break;
                case Phase.Firing:   UpdateFiring();   break;
            }
        }

        // ── Phase 1: 계단식 진입 ──────────────────────────────────

        private void UpdateEntering()
        {
            bool allReached = true;

            foreach (EnemyBase enemy in ActiveEnemies)
            {
                if (!_targetXMap.TryGetValue(enemy, out float tX)) continue;

                Vector3 pos = enemy.transform.position;
                if (Mathf.Abs(pos.x - tX) > 0.05f)
                {
                    allReached = false;
                    float newX = Mathf.MoveTowards(pos.x, tX, entrySpeed * Time.deltaTime);
                    enemy.transform.position = new Vector3(newX, pos.y, 0f);
                }
                else
                {
                    enemy.transform.position = new Vector3(tX, pos.y, 0f);
                }
            }

            if (allReached)
            {
                _phase     = Phase.Firing;
                _fireTimer = fireInterval; // 위치 도달 즉시 첫 발사
                _playerGO  = GameObject.FindWithTag(Constants.TAG_PLAYER);
            }
        }

        // ── Phase 2: 순차 발사 + 개별 퇴장 ──────────────────────

        private void UpdateFiring()
        {
            // 발사된 기체들 퇴장 이동
            for (int i = ActiveEnemies.Count - 1; i >= 0; i--)
            {
                EnemyBase e = ActiveEnemies[i];
                if (!_exiting.Contains(e)) continue;

                Vector3 pos  = e.transform.position;
                float   newX = pos.x + _exitSign * exitSpeed * Time.deltaTime;
                e.transform.position = new Vector3(newX, pos.y, 0f);

                bool exited = _exitSign > 0 ? newX > _exitBound : newX < -_exitBound;
                if (exited) ReleaseEnemyManual(e);
            }

            // 아직 발사할 기체가 없으면 종료
            if (_nextFireIndex >= _ordered.Count) return;

            _fireTimer += Time.deltaTime;
            if (_fireTimer >= fireInterval)
            {
                _fireTimer = 0f;
                FireNext();
            }
        }

        // ── 발사 ─────────────────────────────────────────────────

        private void FireNext()
        {
            while (_nextFireIndex < _ordered.Count)
            {
                EnemyBase enemy = _ordered[_nextFireIndex++];
                if (!ActiveEnemies.Contains(enemy)) continue; // 이미 사망한 기체 건너뜀

                if (EnemyBulletPool.Instance != null)
                {
                    EnemyBullet bullet = EnemyBulletPool.Instance.Get();
                    bullet.transform.position = enemy.transform.position;

                    Vector3 playerPos = _playerGO != null ? _playerGO.transform.position : Vector3.zero;
                    Vector3 dir       = playerPos - enemy.transform.position;
                    bullet.transform.up = dir.sqrMagnitude > 0f ? dir.normalized : Vector3.down;
                    bullet.Initialize(EnemyBulletPool.Instance);
                }

                _exiting.Add(enemy); // 발사 직후 퇴장 시작
                break;
            }
        }
    }
}
