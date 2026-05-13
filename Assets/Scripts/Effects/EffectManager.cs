// Attach to: EffectManager GameObject (Game scene)
using UnityEngine;
using ShooterGame.Utils;

namespace ShooterGame.Effects
{
    public enum EffectType { Explosion, BulletHit, CoinPickup, PowerPickup }

    public class EffectManager : MonoBehaviour
    {
        public static EffectManager Instance { get; private set; }

        [SerializeField] private PooledEffect _explosionPrefab;
        [SerializeField] private PooledEffect _bulletHitPrefab;
        [SerializeField] private PooledEffect _coinPickupPrefab;
        [SerializeField] private PooledEffect _expPickupPrefab;

        [SerializeField] private int _explosionPoolSize  = 10;
        [SerializeField] private int _bulletHitPoolSize  = 20;
        [SerializeField] private int _coinPickupPoolSize = 15;
        [SerializeField] private int _expPickupPoolSize  = 15;

        private ObjectPool<PooledEffect> _explosionPool;
        private ObjectPool<PooledEffect> _bulletHitPool;
        private ObjectPool<PooledEffect> _coinPickupPool;
        private ObjectPool<PooledEffect> _expPickupPool;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            _explosionPool  = new ObjectPool<PooledEffect>(_explosionPrefab,  _explosionPoolSize,  transform);
            _bulletHitPool  = new ObjectPool<PooledEffect>(_bulletHitPrefab,  _bulletHitPoolSize,  transform);
            _coinPickupPool = new ObjectPool<PooledEffect>(_coinPickupPrefab, _coinPickupPoolSize, transform);
            _expPickupPool  = new ObjectPool<PooledEffect>(_expPickupPrefab,  _expPickupPoolSize,  transform);
        }

        public void Play(EffectType type, Vector3 position)
        {
            ObjectPool<PooledEffect> pool = type switch
            {
                EffectType.Explosion  => _explosionPool,
                EffectType.BulletHit  => _bulletHitPool,
                EffectType.CoinPickup => _coinPickupPool,
                EffectType.PowerPickup  => _expPickupPool,
                _                     => null
            };
            if (pool == null) return;

            PooledEffect effect = pool.Get();
            effect.Play(position, e => pool.Release(e));
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
