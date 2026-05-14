// Attach to: Player GameObject

using UnityEngine;
using ShooterGame.Core;

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
        private int   _bulletDamage  = 10;
        private int   _missileStage  = 0;   // 0~4

        public float FireInterval  => _fireInterval;
        public int   BulletDamage  => _bulletDamage;
        public int   MissileStage  => _missileStage;

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
            switch (_missileStage)
            {
                case 0:
                    SpawnBullet(0f,     0f,   false);
                    break;
                case 1:
                    SpawnBullet(-0.30f, 0f,   false);
                    SpawnBullet( 0.30f, 0f,   false);
                    break;
                case 2:
                    SpawnBullet(-0.50f, -15f, false);
                    SpawnBullet( 0f,     0f,  false);
                    SpawnBullet( 0.50f,  15f, false);
                    break;
                case 3:
                    SpawnBullet(-0.50f, -15f, false);
                    SpawnBullet( 0f,     0f,  true);   // 중앙만 유도
                    SpawnBullet( 0.50f,  15f, false);
                    break;
                default: // 4+
                    SpawnBullet(-0.40f,  -5f, true);
                    SpawnBullet( 0f,      0f, true);
                    SpawnBullet( 0.40f,   5f, true);
                    break;
            }
            AudioManager.Instance?.PlaySFX(SfxType.PlayerShoot);
        }

        private void SpawnBullet(float xOffset, float zAngle, bool homing)
        {
            Bullet bullet = BulletPool.Instance.Get();
            if (bullet == null) return;

            Vector3 pos = firePoint != null ? firePoint.position : transform.position;
            pos.x += xOffset;
            bullet.transform.position = pos;
            bullet.transform.rotation = Quaternion.Euler(0f, 0f, zAngle);
            bullet.Initialize(BulletPool.Instance);
            bullet.SetDamage(_bulletDamage);
            if (homing) bullet.SetHoming(true);
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

        public void IncreaseMissileStage()
        {
            _missileStage = Mathf.Min(_missileStage + 1, 4);
        }

        private void OnDestroy()
        {
            // No event subscriptions here, but kept as reminder pattern
        }
    }
}
