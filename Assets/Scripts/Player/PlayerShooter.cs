// Attach to: Player GameObject

using UnityEngine;

namespace ShooterGame.Player
{
    /// <summary>
    /// Auto-fires bullets upward at a fixed interval using the BulletPool.
    /// Attack speed can be upgraded at runtime via SetFireRate().
    /// </summary>
    public class PlayerShooter : MonoBehaviour
    {
        [SerializeField] private float fireRate     = 0.3f;  // seconds between shots
        [SerializeField] private Transform firePoint;         // spawn position (child object above player)

        // Cached WaitForSeconds — never create 'new' inside coroutines
        private float _fireInterval;
        private float _fireTimer;
        private int   _bulletDamage = 10;  // matches Bullet prefab's default damage

        public float FireInterval => _fireInterval;
        public int   BulletDamage => _bulletDamage;

        private void Awake()
        {
            _fireInterval = fireRate;
        }

        private void Update()
        {
            _fireTimer += Time.deltaTime;
            if (_fireTimer >= _fireInterval)
            {
                _fireTimer = 0f;
                Fire();
            }
        }

        private void Fire()
        {
            Bullet bullet = BulletPool.Instance.Get();
            if (bullet == null) return;

            // Place bullet at fire point
            bullet.transform.position = firePoint != null ? firePoint.position : transform.position;
            bullet.transform.rotation = Quaternion.identity;
            bullet.Initialize(BulletPool.Instance);
            bullet.SetDamage(_bulletDamage);
        }

        /// <summary>Call from UpgradeManager to modify attack speed.</summary>
        public void SetFireRate(float newRate)
        {
            _fireInterval = Mathf.Max(0.05f, newRate); // floor to prevent zero-interval
        }

        public void IncreaseFireRate(float amount)
        {
            _fireInterval = Mathf.Max(0.05f, _fireInterval - amount);
        }

        public void IncreaseDamage(int amount)
        {
            _bulletDamage += amount;
        }

        /// <summary>게임 시작 시 InGameManager가 한 번 호출. totalGain = gainPerLevel * level.</summary>
        public void ApplyPermanentDamageBonus(int totalGain)
        {
            if (totalGain <= 0) return;
            IncreaseDamage(totalGain);
        }

        /// <summary>totalReduction = gainPerLevel * level (초 단위 감소량).</summary>
        public void ApplyPermanentAtkSpeedBonus(float totalReduction)
        {
            if (totalReduction <= 0f) return;
            IncreaseFireRate(totalReduction);
        }

        private void OnDestroy()
        {
            // No event subscriptions here, but kept as reminder pattern
        }
    }
}
