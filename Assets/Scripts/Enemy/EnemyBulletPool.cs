// Attach to: EnemyBulletPool GameObject (Game scene)
using UnityEngine;
using ShooterGame.Utils;

namespace ShooterGame.Enemy
{
    public class EnemyBulletPool : MonoBehaviour
    {
        public static EnemyBulletPool Instance { get; private set; }

        [SerializeField] private EnemyBullet bulletPrefab;
        [SerializeField] private int         poolSize = Constants.POOL_SIZE_BULLET;

        private ObjectPool<EnemyBullet> _pool;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _pool = new ObjectPool<EnemyBullet>(bulletPrefab, poolSize, transform);
        }

        public EnemyBullet Get()               => _pool.Get();
        public void        Release(EnemyBullet b) => _pool.Release(b);

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
