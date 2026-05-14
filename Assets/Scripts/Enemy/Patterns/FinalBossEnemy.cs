// Attach to: FinalBoss Enemy prefab root
using System.Collections;
using UnityEngine;
using ShooterGame.Core;

namespace ShooterGame.Enemy
{
    public class FinalBossEnemy : EnemyBase
    {
        [Header("Movement")]
        [SerializeField] private float bossCenterY    = 5f;
        [SerializeField] private float sweepFrequency = 1.0f;
        [SerializeField] private float sweepAmplitude = 3f;

        [Header("Phase 1")]
        [SerializeField] private int   phase1BulletCount  = 5;
        [SerializeField] private float phase1FireInterval = 1.5f;

        [Header("Phase 2")]
        [SerializeField] private int   phase2BulletCount  = 7;
        [SerializeField] private float phase2FireInterval = 1.0f;
        [SerializeField] private float phase2SpeedBonus   = 1.5f;
        [SerializeField] private float phase2HpThreshold  = 0.5f;

        [Header("Shared")]
        [SerializeField] private float spreadAngle  = 20f;
        [SerializeField] private int   bulletDamage = 8;

        private bool           _reachedCenter;
        private float          _sweepTimer;
        private bool           _isPhase2;
        private int            _maxHp;
        private Coroutine      _shootCoroutine;
        private WaitForSeconds _phase1Wait;
        private WaitForSeconds _phase2Wait;
        private SpriteRenderer _sr;

        protected override void OnEnable()
        {
            base.OnEnable();
            _reachedCenter = false;
            _sweepTimer    = 0f;
            _isPhase2      = false;
            _maxHp         = 0;
            _phase1Wait    = new WaitForSeconds(phase1FireInterval);
            _phase2Wait    = new WaitForSeconds(phase2FireInterval);
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_sr != null)  _sr.color = Color.white;
        }

        private void OnDisable()
        {
            if (_shootCoroutine != null)
            {
                StopCoroutine(_shootCoroutine);
                _shootCoroutine = null;
            }
        }

        public override void TakeDamage(int dmg)
        {
            if (_maxHp == 0) _maxHp = CurrentHp;   // set on first hit
            base.TakeDamage(dmg);
            if (!_isPhase2 && CurrentHp > 0) CheckPhaseTransition();
        }

        protected override void Move()
        {
            if (!_reachedCenter)
            {
                Vector3 target     = new Vector3(transform.position.x, bossCenterY, 0f);
                transform.position = Vector3.MoveTowards(transform.position, target, CurrentSpeed * Time.deltaTime);

                if (Mathf.Abs(transform.position.y - bossCenterY) < 0.05f)
                {
                    _reachedCenter  = true;
                    _shootCoroutine = StartCoroutine(ShootLoop());
                }
            }
            else
            {
                _sweepTimer       += Time.deltaTime;
                float newX         = Mathf.Sin(_sweepTimer * sweepFrequency) * sweepAmplitude;
                transform.position = new Vector3(newX, bossCenterY, 0f);
            }
        }

        private void CheckPhaseTransition()
        {
            if (_maxHp == 0)
            {
                Debug.LogWarning("[FinalBossEnemy] MaxHp is 0 — check EnemyData_FinalBoss.BaseHp");
                return;
            }
            if (CurrentHp <= Mathf.RoundToInt(_maxHp * phase2HpThreshold))
                EnterPhase2();
        }

        private void EnterPhase2()
        {
            _isPhase2     = true;
            CurrentSpeed *= phase2SpeedBonus;

            if (_sr != null) _sr.color = new Color(1f, 0.3f, 0.3f);

            if (_shootCoroutine != null) StopCoroutine(_shootCoroutine);
            _shootCoroutine = StartCoroutine(ShootLoop());
        }

        private IEnumerator ShootLoop()
        {
            while (true)
            {
                yield return _isPhase2 ? _phase2Wait : _phase1Wait;
                FireSpread(_isPhase2 ? phase2BulletCount : phase1BulletCount);
            }
        }

        private void FireSpread(int count)
        {
            if (EnemyBulletPool.Instance == null) return;
            float step       = count > 1 ? spreadAngle * 2f / (count - 1) : 0f;
            float startAngle = -spreadAngle;
            for (int i = 0; i < count; i++)
                FireBullet(startAngle + step * i);
            AudioManager.Instance?.PlaySFX(SfxType.EnemyShoot);
        }

        private void FireBullet(float angleOffset)
        {
            EnemyBullet bullet = EnemyBulletPool.Instance.Get();
            if (bullet == null) return;
            bullet.transform.position = transform.position;
            bullet.transform.rotation = Quaternion.Euler(0f, 0f, 180f + angleOffset);
            bullet.Initialize(EnemyBulletPool.Instance, bulletDamage);
        }
    }
}
