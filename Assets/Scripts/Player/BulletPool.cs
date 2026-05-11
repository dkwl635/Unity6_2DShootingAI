// Attach to: BulletPool GameObject

using UnityEngine;
using ShooterGame.Utils;

namespace ShooterGame.Player
{
    /// <summary>
    /// Singleton wrapper around ObjectPool<Bullet>.
    /// Provides a global Get / Release interface for bullets.
    /// </summary>
    public class BulletPool : MonoBehaviour
    {
        public static BulletPool Instance { get; private set; }

        [SerializeField] private Bullet bulletPrefab;
        [SerializeField] private int    poolSize = Constants.POOL_SIZE_BULLET;

        private ObjectPool<Bullet> _pool;

        private void Awake()
        {
            // Singleton duplicate guard
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _pool = new ObjectPool<Bullet>(bulletPrefab, poolSize, transform);
        }

        public Bullet Get()    => _pool.Get();
        public void Release(Bullet b) => _pool.Release(b);
    }
}
