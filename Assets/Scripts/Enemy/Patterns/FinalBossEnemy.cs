// Attach to: FinalBoss Enemy prefab root
using System;
using System.Collections;
using UnityEngine;
using ShooterGame.Core;
using ShooterGame.UI;

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
        [SerializeField] private Sprite phase2Sprite;

        [Header("Phase 2 Flash")]
        [SerializeField] private Color flashColor   = new Color(1f, 0.2f, 0.2f);
        [SerializeField] private float flashFadeIn  = 0.3f;
        [SerializeField] private float flashHold    = 0.15f;
        [SerializeField] private float flashFadeOut = 0.4f;

        [Header("Death")]
        [SerializeField] private Sprite destroyedSprite;
        [SerializeField] private float  deathSinkSpeed = 4f;
        [SerializeField] private float  deathAnimTime  = 1.2f;

        [Header("Shared")]
        [SerializeField] private float spreadAngle  = 20f;


        public static FinalBossEnemy ActiveBoss { get; private set; }

        public int Hp    => CurrentHp;
        public int MaxHp => _maxHp;

        public event Action<int, int> OnHpChanged;  // (current, max)

        private bool           _reachedCenter;
        private float          _sweepTimer;
        private bool           _isPhase2;
        private bool           _isInvincible;
        private bool           _isDying;
        private int            _maxHp;
        private Coroutine      _shootCoroutine;
        private WaitForSeconds _phase1Wait;
        private WaitForSeconds _phase2Wait;
        private SpriteRenderer _sr;
        private Sprite         _originalSprite;

        protected override void OnEnable()
        {
            base.OnEnable();
            ActiveBoss      = this;
            _reachedCenter  = false;
            _sweepTimer     = 0f;
            _isPhase2       = false;
            _isInvincible   = true;   // 하강 중 무적
            _isDying        = false;
            _shootCoroutine = null;
            _phase1Wait     = new WaitForSeconds(phase1FireInterval);
            _phase2Wait     = new WaitForSeconds(phase2FireInterval);
            if (_sr == null)
            {
                _sr = GetComponent<SpriteRenderer>();
                if (_sr != null) _originalSprite = _sr.sprite;
            }
            if (_sr != null)
            {
                _sr.color  = Color.white;
                _sr.sprite = _originalSprite;
            }
        }

        private void OnDisable()
        {
            if (ActiveBoss == this) ActiveBoss = null;
            if (_shootCoroutine != null)
            {
                StopCoroutine(_shootCoroutine);
                _shootCoroutine = null;
            }
        }

        public override void Initialize(EnemyData data, float hpMultiplier, float speedMultiplier,
                                        Action<EnemyBase> releaseCallback)
        {
            base.Initialize(data, hpMultiplier, speedMultiplier, releaseCallback);
            _maxHp = CurrentHp;
            OnHpChanged?.Invoke(CurrentHp, _maxHp);
        }

        public override void TakeDamage(int dmg)
        {
            if (_isInvincible || _isDying) return;
            base.TakeDamage(dmg);
            OnHpChanged?.Invoke(Mathf.Max(0, CurrentHp), _maxHp);
            if (!_isPhase2 && CurrentHp > 0) CheckPhaseTransition();
        }

        protected override void Move()
        {
            if (_isDying) return;

            if (!_reachedCenter)
            {
                Vector3 target     = new Vector3(transform.position.x, bossCenterY, 0f);
                transform.position = Vector3.MoveTowards(transform.position, target, CurrentSpeed * Time.deltaTime);

                if (Mathf.Abs(transform.position.y - bossCenterY) < 0.05f)
                {
                    _reachedCenter  = true;
                    _isInvincible   = false;
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

        // ── Phase 2 ──────────────────────────────────────────────

        private void CheckPhaseTransition()
        {
            if (CurrentHp <= Mathf.RoundToInt(_maxHp * phase2HpThreshold))
                EnterPhase2();
        }

        private void EnterPhase2()
        {
            _isPhase2 = true;
            StartCoroutine(Phase2Transition());
        }

        private IEnumerator Phase2Transition()
        {
            _isInvincible = true;

            if (_shootCoroutine != null)
            {
                StopCoroutine(_shootCoroutine);
                _shootCoroutine = null;
            }

            // 화면 플래시 시작
            ScreenFlashUI.Instance?.Flash(flashColor, flashFadeIn, flashHold, flashFadeOut);

            // 페이드인 피크(절반) 지점에서 스프라이트 교체
            yield return new WaitForSeconds(flashFadeIn);

            if (phase2Sprite != null && _sr != null)
                _sr.sprite = phase2Sprite;
            if (_sr != null) _sr.color = Color.white;

            yield return new WaitForSeconds(flashHold + flashFadeOut);

            // 플래시 끝난 후 속도 강화 + 사격 재개
            CurrentSpeed *= phase2SpeedBonus;
            _isInvincible  = false;
            _shootCoroutine = StartCoroutine(ShootLoop());
        }

        // ── Death ─────────────────────────────────────────────────

        protected override void Die()
        {
            if (_isDying) return;
            _isDying = true;

            TriggerDeathEffects();

            if (_shootCoroutine != null)
            {
                StopCoroutine(_shootCoroutine);
                _shootCoroutine = null;
            }

            StartCoroutine(DeathSink());
        }

        private IEnumerator DeathSink()
        {
            if (destroyedSprite != null && _sr != null)
                _sr.sprite = destroyedSprite;
            if (_sr != null) _sr.color = Color.white;

            float elapsed = 0f;
            while (elapsed < deathAnimTime)
            {
                elapsed            += Time.deltaTime;
                transform.position += Vector3.down * deathSinkSpeed * Time.deltaTime;
                yield return null;
            }

            ReturnToPool();
        }

        // ── Shooting ──────────────────────────────────────────────

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
            bullet.Initialize(EnemyBulletPool.Instance);
        }
    }
}
