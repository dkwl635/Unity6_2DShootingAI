// Attach to: FinalBoss Enemy prefab root
using System;
using System.Collections;
using UnityEngine;
using ShooterGame.Core;
using ShooterGame.Player;
using ShooterGame.UI;
using ShooterGame.Utils;

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
        [SerializeField] private Sprite phase2Sprite;

        [Header("Phase 2 Flash")]
        [SerializeField] private Color flashColor   = new Color(1f, 0.2f, 0.2f);
        [SerializeField] private float flashFadeIn  = 0.3f;
        [SerializeField] private float flashHold    = 0.15f;
        [SerializeField] private float flashFadeOut = 0.4f;

        [Header("Phase 2 Shake")]
        [SerializeField] private float phase2ShakeDuration  = 0.5f;
        [SerializeField] private float phase2ShakeMagnitude = 0.3f;

        [Header("Death")]
        [SerializeField] private Sprite destroyedSprite;
        [SerializeField] private float  deathSinkSpeed = 4f;
        [SerializeField] private float  deathAnimTime  = 1.2f;

        [Header("Shared")]
        [SerializeField] private float spreadAngle  = 20f;

        [Header("Aimed Shot")]
        [SerializeField] private float phase1AimedInterval = 3.0f;
        [SerializeField] private float phase1AimDuration   = 1.0f;
        [SerializeField] private float phase2AimedInterval = 2.0f;
        [SerializeField] private float phase2AimDuration   = 0.6f;

        [Header("Aim Line")]
        [SerializeField] private LineRenderer _aimLine;
        [SerializeField] private Color        _aimLineColor    = new Color(1f, 0.1f, 0.1f, 1f);
        [SerializeField] private float        _aimLineWidth    = 0.04f;
        [SerializeField] private float        _aimFlickerSpeed = 10f;

        [Header("Laser")]
        [SerializeField] private LineRenderer _laserLine;
        [SerializeField] private Color        _laserColor      = new Color(1f, 0.6f, 0.1f, 1f);
        [SerializeField] private float        _laserWidth      = 0.25f;
        [SerializeField] private float        _laserPreDelay    = 0.2f;
        [SerializeField] private float        _laserDuration    = 1f;
        [SerializeField] private float        _laserTickInterval  = 0.2f;
        [SerializeField] private float        _laserSweepAngle    = 60f;
        [SerializeField] private int          _laserDamage        = 3;
        [SerializeField] private float        _laserLength     = 20f;
        [SerializeField] private LayerMask    _playerLayerMask;

        [Header("Audio")]
        [SerializeField] private AudioClip _bossBgm;

        [Header("VFX")]
        [SerializeField] private ParticleSystem[] _dieParticleSystems;
        [SerializeField] private float            _deathParticleInterval = 0.5f; // 이펙트 발동 간격
        [SerializeField] private float            _deathParticleSpread   = 1.2f; // 보스 주변 산포 반경

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
        private Coroutine      _aimedCoroutine;
        private WaitForSeconds _phase1Wait;
        private WaitForSeconds _phase2Wait;
        private WaitForSeconds _phase1AimedWait;
        private WaitForSeconds _phase2AimedWait;
        private WaitForSeconds _laserPreWait;
        private Transform      _playerTransform;
        private SpriteRenderer _sr;
        private Sprite         _originalSprite;

        protected override void OnEnable()
        {
            base.OnEnable();
            AudioManager.Instance?.PlayBgmClip(_bossBgm);
            ActiveBoss      = this;
            _reachedCenter  = false;
            _sweepTimer     = 0f;
            _isPhase2       = false;
            _isInvincible   = true;   // 하강 중 무적
            _isDying        = false;
            _shootCoroutine  = null;
            _aimedCoroutine  = null;
            _playerTransform = null;
            _phase1Wait      = new WaitForSeconds(phase1FireInterval);
            _phase2Wait      = new WaitForSeconds(phase2FireInterval);
            _phase1AimedWait = new WaitForSeconds(phase1AimedInterval);
            _phase2AimedWait = new WaitForSeconds(phase2AimedInterval);
            _laserPreWait    = new WaitForSeconds(_laserPreDelay);
            if (_aimLine != null)
            {
                _aimLine.positionCount = 2;
                _aimLine.useWorldSpace = false; // 로컬 공간 — position[0]=zero가 항상 보스 위치
                _aimLine.startWidth    = _aimLineWidth;
                _aimLine.endWidth      = _aimLineWidth * 0.3f;
                _aimLine.SetPosition(0, Vector3.zero);
                _aimLine.enabled       = false;
            }
            if (_laserLine != null)
            {
                _laserLine.positionCount = 2;
                _laserLine.useWorldSpace = true;
                _laserLine.enabled       = false;
            }
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
            StopShootingCoroutines();
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

            if (!_isPhase2 && CurrentHp - dmg <= 0)
            {
                // 1페이즈 HP 0 도달 — 실제 사망 대신 2페이즈 전환
                CurrentHp = 0;
                OnHpChanged?.Invoke(0, _maxHp);
                EnterPhase2();
                return;
            }

            base.TakeDamage(dmg);
            OnHpChanged?.Invoke(Mathf.Max(0, CurrentHp), _maxHp);
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
                    _aimedCoroutine = StartCoroutine(LaserAttackLoop());
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

        private void EnterPhase2()
        {
            _isPhase2 = true;
            StartCoroutine(Phase2Transition());
        }

        private IEnumerator Phase2Transition()
        {
            _isInvincible = true;
            StopShootingCoroutines();

            // HP 100% 회복 + 탄막 제거 + 쉐이크 + 플래시
            CurrentHp = _maxHp;
            OnHpChanged?.Invoke(CurrentHp, _maxHp);
            EnemyBulletPool.Instance?.ReleaseAll();
            CameraShake.Instance?.Shake(phase2ShakeDuration, phase2ShakeMagnitude);
            ScreenFlashUI.Instance?.Flash(flashColor, flashFadeIn, flashHold, flashFadeOut);

            // 페이드인 피크(절반) 지점에서 스프라이트 교체
            yield return new WaitForSeconds(flashFadeIn);

            if (phase2Sprite != null && _sr != null)
                _sr.sprite = phase2Sprite;
            if (_sr != null) _sr.color = Color.white;

            yield return new WaitForSeconds(flashHold + flashFadeOut);

            // 플래시 끝난 후 속도 강화 + 사격 재개
            CurrentSpeed    *= phase2SpeedBonus;
            _isInvincible    = false;
            _shootCoroutine  = StartCoroutine(ShootLoop());
            _aimedCoroutine  = StartCoroutine(LaserAttackLoop());
        }

        // ── Death ─────────────────────────────────────────────────

        protected override void Die()
        {
            if (_isDying) return;
            _isDying = true;

            TriggerDeathEffects();
            StopShootingCoroutines();
            EnemyBulletPool.Instance?.ReleaseAll();

            StartCoroutine(DeathSink());
            StartCoroutine(DeathParticleRoutine());
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

        private IEnumerator DeathParticleRoutine()
        {
            if (_dieParticleSystems == null || _dieParticleSystems.Length == 0) yield break;

            var    wait    = new WaitForSeconds(_deathParticleInterval);
            float  elapsed = 0f;
            int    total   = _dieParticleSystems.Length;

            while (elapsed < deathAnimTime)
            {
                // 배열 크기를 넘지 않는 범위에서 2~4개 랜덤 선택
                int playCount = Mathf.Min(UnityEngine.Random.Range(2, 5), total);

                // Fisher-Yates 셔플로 중복 없이 인덱스 선택
                int[] indices = new int[total];
                for (int i = 0; i < total; i++) indices[i] = i;
                for (int i = total - 1; i > 0; i--)
                {
                    int j = UnityEngine.Random.Range(0, i + 1);
                    (indices[i], indices[j]) = (indices[j], indices[i]);
                }

                for (int i = 0; i < playCount; i++)
                {
                    ParticleSystem ps = _dieParticleSystems[indices[i]];
                    if (ps == null) continue;                  
                    ps.Play();
                }

                yield return wait;
                elapsed += _deathParticleInterval;
            }
        }

        // ── Shooting ──────────────────────────────────────────────

        private void StopShootingCoroutines()
        {
            if (_shootCoroutine != null) { StopCoroutine(_shootCoroutine); _shootCoroutine = null; }
            if (_aimedCoroutine != null) { StopCoroutine(_aimedCoroutine); _aimedCoroutine = null; }
            if (_aimLine   != null) _aimLine.enabled   = false;
            if (_laserLine != null) _laserLine.enabled = false;
        }

        private IEnumerator ShootLoop()
        {
            while (true)
            {
                yield return _isPhase2 ? _phase2Wait : _phase1Wait;
                FireSpread(_isPhase2 ? phase2BulletCount : phase1BulletCount);
            }
        }

        private IEnumerator LaserAttackLoop()
        {
            while (true)
            {
                yield return _isPhase2 ? _phase2AimedWait : _phase1AimedWait;
                yield return StartCoroutine(LaserAttack());
            }
        }

        private IEnumerator LaserAttack()
        {
            float aimDuration = _isPhase2 ? phase2AimDuration : phase1AimDuration;

            if (_playerTransform == null)
            {
                GameObject p = GameObject.FindWithTag(Constants.TAG_PLAYER);
                if (p != null) _playerTransform = p.transform;
            }

            // ── Aim phase: flickering line points at sweep start position ─
            if (_aimLine != null) _aimLine.enabled = true;
            for (float t = 0f; t < aimDuration; t += Time.deltaTime)
            {
                if (_aimLine != null && _playerTransform != null)
                {
                    bool    isLeft     = _playerTransform.position.x < 0f;
                    float   startAngle = isLeft ? 270f - _laserSweepAngle : 270f + _laserSweepAngle;
                    Vector2 sweepDir   = AngleToDir(startAngle);
                    _aimLine.SetPosition(1, (Vector3)sweepDir * _laserLength);

                    float alpha = (Mathf.Sin(t * _aimFlickerSpeed) + 1f) * 0.5f;
                    Color c     = _aimLineColor;
                    c.a = alpha;
                    _aimLine.startColor = c;
                    c.a = alpha * 0.2f;
                    _aimLine.endColor = c;
                }
                yield return null;
            }
            if (_aimLine != null) _aimLine.enabled = false;

            if (_playerTransform == null) yield break;

            // ── Lock sweep area based on final player position ────────────
            bool  playerLeft      = _playerTransform.position.x < 0f;
            float sweepStartAngle = playerLeft ? 270f - _laserSweepAngle : 270f + _laserSweepAngle;
            float sweepEndAngle   = 270f; // always sweep toward straight down (center)

            // ── Pre-fire warning: solid line at sweep start so player can dodge ─
            if (_aimLine != null)
            {
                _aimLine.SetPosition(1, (Vector3)AngleToDir(sweepStartAngle) * _laserLength);
                _aimLine.startColor = _aimLineColor;
                Color endC = _aimLineColor; endC.a = 0f;
                _aimLine.endColor = endC;
                _aimLine.enabled  = true;
            }
            yield return _laserPreWait;
            if (_aimLine != null) _aimLine.enabled = false;

            // ── Fire phase: sweep laser across player's area ──────────────
            if (_laserLine != null)
            {
                _laserLine.startWidth = _laserWidth;
                _laserLine.endWidth   = _laserWidth;
                _laserLine.startColor = _laserColor;
                _laserLine.endColor   = _laserColor;
                _laserLine.enabled    = true;
            }

            AudioManager.Instance?.PlaySFX(SfxType.EnemyShoot);

            float elapsed     = 0f;
            float damageTimer = _laserTickInterval;

            while (elapsed < _laserDuration)
            {
                float   angle = Mathf.LerpAngle(sweepStartAngle, sweepEndAngle, elapsed / _laserDuration);
                Vector2 dir   = AngleToDir(angle);

                if (_laserLine != null)
                {
                    _laserLine.SetPosition(0, transform.position);
                    _laserLine.SetPosition(1, (Vector2)transform.position + dir * _laserLength);
                }

                damageTimer += Time.deltaTime;
                if (damageTimer >= _laserTickInterval)
                {
                    damageTimer -= _laserTickInterval;
                    RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, _laserLength, _playerLayerMask);
                    if (hit.collider != null)
                    {
                        PlayerStats stats = hit.collider.GetComponent<PlayerStats>();
                        stats?.TakeDamage(_laserDamage);
                    }
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (_laserLine != null) _laserLine.enabled = false;
        }

        private static Vector2 AngleToDir(float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
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
