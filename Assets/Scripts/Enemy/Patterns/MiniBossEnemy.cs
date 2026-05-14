// Attach to: MiniBoss Enemy prefab root
using System.Collections;
using UnityEngine;
using ShooterGame.Core;

namespace ShooterGame.Enemy
{
    public class MiniBossEnemy : EnemyBase
    {
        [Header("Movement")]
        [SerializeField] private float bossCenterY    = 5f;
        [SerializeField] private float sweepFrequency = 1.5f;
        [SerializeField] private float sweepAmplitude = 3f;

        [Header("Shooting")]
        [SerializeField] private float fireInterval  = 2f;
        [SerializeField] private float spreadAngle   = 20f;

        private bool           _reachedCenter;
        private float          _sweepTimer;
        private Coroutine      _shootCoroutine;
        private WaitForSeconds _fireWait;

        protected override void OnEnable()
        {
            base.OnEnable();
            _reachedCenter  = false;
            _sweepTimer     = 0f;
            _fireWait       = new WaitForSeconds(fireInterval);
        }

        private void OnDisable()
        {
            if (_shootCoroutine != null)
            {
                StopCoroutine(_shootCoroutine);
                _shootCoroutine = null;
            }
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

        private IEnumerator ShootLoop()
        {
            while (true)
            {
                yield return _fireWait;
                FireSpread();
            }
        }

        private void FireSpread()
        {
            if (EnemyBulletPool.Instance == null) return;

            FireBullet(0f);              // center — straight down
            FireBullet(-spreadAngle);    // left diagonal
            FireBullet( spreadAngle);    // right diagonal

            AudioManager.Instance?.PlaySFX(SfxType.EnemyShoot);
        }

        private void FireBullet(float angleOffset)
        {
            EnemyBullet bullet = EnemyBulletPool.Instance.Get();
            if (bullet == null) return;

            bullet.transform.position = transform.position;
            // z = 180 → straight down; ±offset tilts left/right
            bullet.transform.rotation = Quaternion.Euler(0f, 0f, 180f + angleOffset);
            bullet.Initialize(EnemyBulletPool.Instance);
        }
    }
}
