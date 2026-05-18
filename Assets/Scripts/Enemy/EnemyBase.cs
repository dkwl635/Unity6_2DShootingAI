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
        private bool              _guaranteePower;

        private SpriteRenderer _renderer;
        private Material       _originalMaterial;
        private float          _flashWait = 0.5f;



        // Static event — DropManager subscribes without a direct reference to EnemyBase instances
        public static event Action<Vector3, int, int, float, int, int, float> OnEnemyDied; // pos, coinValue, coinCount, coinRadius, powerValue, powerCount, powerRadius

        private void Awake()
        {
            _bottomBound = -(Constants.PLAY_HALF_HEIGHT + 1f);
            _renderer    = GetComponent<SpriteRenderer>();
            if (_renderer != null)
                _originalMaterial = _renderer.sharedMaterial;
        }

        protected virtual void OnEnable()
        {
            _released       = false;
            _isFlashing     = false;
            _guaranteePower = false;
            if (_renderer != null && _originalMaterial != null)
                _renderer.sharedMaterial = _originalMaterial;
        }

        public void SetGuaranteePower() => _guaranteePower = true;

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
            TriggerDeathEffects();
            ReturnToPool();
        }

        protected void TriggerDeathEffects()
        {
            EffectManager.Instance?.Play(EffectType.Explosion, transform.position);
            AudioManager.Instance?.PlaySFX(SfxType.EnemyDeath);
            ScoreManager.Instance?.Add(_data.ScoreValue);

            int   droppedCoin        = UnityEngine.Random.value < _data.CoinDropChance  ? _data.CoinDrop  : 0;
            int   droppedCoinCount   = droppedCoin  > 0 ? _data.CoinDropCount  : 0;
            int   droppedPower       = (_guaranteePower || UnityEngine.Random.value < _data.PowerDropChance) ? _data.PowerDrop : 0;
            int   droppedPowerCount  = droppedPower > 0 ? _data.PowerDropCount : 0;
            OnEnemyDied?.Invoke(transform.position,
                droppedCoin,  droppedCoinCount,  _data.CoinDropRadius,
                droppedPower, droppedPowerCount, _data.PowerDropRadius);
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

        protected void ReturnToPool()
        {
            if (_released) return;
            _released = true;
            _releaseCallback?.Invoke(this);
        }
    }
}
