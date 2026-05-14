// Attach to: Enemy Prefab root
using System;
using System.Collections;
using UnityEngine;
using ShooterGame.Core;
using ShooterGame.Effects;
using ShooterGame.Player;
using ShooterGame.Utils;

namespace ShooterGame.Enemy
{
    public class EnemyBase : MonoBehaviour
    {
        protected int   CurrentHp;
        protected float CurrentSpeed;

        [SerializeField] private Material hitFlashMaterial;
        [SerializeField] private float    flashDuration = 0.08f;

        private EnemyData         _data;
        private Action<EnemyBase> _releaseCallback;
        private float             _bottomBound;
        private bool              _released;
        private bool              _isFlashing;

        private SpriteRenderer _renderer;
        private Material       _originalMaterial;
        private WaitForSeconds _flashWait;

        // Static event — DropManager subscribes without a direct reference to EnemyBase instances
        public static event Action<Vector3, int, int> OnEnemyDied;

        private void Awake()
        {
            _bottomBound = -(Constants.PLAY_HALF_HEIGHT + 1f);
            _renderer    = GetComponent<SpriteRenderer>();
            _flashWait   = new WaitForSeconds(flashDuration);
            if (_renderer != null)
                _originalMaterial = _renderer.sharedMaterial;
        }

        protected virtual void OnEnable()
        {
            _released   = false;
            _isFlashing = false;
            if (_renderer != null && _originalMaterial != null)
                _renderer.sharedMaterial = _originalMaterial;
        }

        public virtual void Initialize(EnemyData data, float hpMultiplier, float speedMultiplier,
                                       Action<EnemyBase> releaseCallback)
        {
            _data            = data;
            _releaseCallback = releaseCallback;
            CurrentHp        = Mathf.RoundToInt(data.BaseHp * hpMultiplier);
            CurrentSpeed     = data.MoveSpeed * speedMultiplier;
        }

        protected virtual void Update()
        {
            Move();
            if (transform.position.y < _bottomBound)
                ReturnToPool();
        }

        protected virtual void Move() { }

        public virtual void TakeDamage(int dmg)
        {
            if (dmg <= 0 || _released) return;
            CurrentHp -= dmg;
            if (CurrentHp <= 0) { Die(); return; }
            if (!_isFlashing) StartCoroutine(HitFlash());
        }

        private IEnumerator HitFlash()
        {
            _isFlashing = true;
            if (_renderer != null && hitFlashMaterial != null)
                _renderer.sharedMaterial = hitFlashMaterial;

            yield return _flashWait;

            if (_renderer != null && _originalMaterial != null)
                _renderer.sharedMaterial = _originalMaterial;
            _isFlashing = false;
        }

        public void ForceReturnToPool()
        {
            if (_released) return;
            _released = true;
            // Caller (PatternBase) handles pool.Release() directly
        }

        protected virtual void Die()
        {
            EffectManager.Instance?.Play(EffectType.Explosion, transform.position);
            AudioManager.Instance?.PlaySFX(SfxType.EnemyDeath);
            ScoreManager.Instance?.Add(_data.ScoreValue);
            OnEnemyDied?.Invoke(transform.position, _data.CoinDrop, _data.PowerDrop);
            ReturnToPool();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_released) return;
            if (other.CompareTag(Constants.TAG_PLAYER))
            {
                PlayerStats stats = other.GetComponent<PlayerStats>();
                stats?.TakeDamage(_data.ContactDamage);
                Die();
            }
        }

        private void ReturnToPool()
        {
            if (_released) return;
            _released = true;
            _releaseCallback?.Invoke(this);
        }
    }
}
