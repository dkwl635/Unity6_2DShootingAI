// Attach to: Bullet Prefab

using UnityEngine;

namespace ShooterGame.Player
{
    /// <summary>
    /// Moves upward each frame. Returns itself to the pool when it exits the screen.
    /// </summary>
    public class Bullet : MonoBehaviour
    {
        [SerializeField] private float moveSpeed  = 12f;
        [SerializeField] private int   damage     = 10;

        // Cached camera — assigned once via Initialize()
        private Camera     _cam;
        private BulletPool _pool;

        // Screen top boundary in world Y (cached, recalculated on init)
        private float _topBound;

        /// <summary>Called by PlayerShooter immediately after Get() from pool.</summary>
        public void Initialize(BulletPool pool)
        {
            _pool = pool;
            _cam  = Camera.main;

            // Cache the screen top boundary in world space
            _topBound = _cam.ViewportToWorldPoint(new Vector3(0.5f, 1.1f, _cam.nearClipPlane)).y;
        }

        private void Update()
        {
            // Move upward
            transform.Translate(Vector3.up * moveSpeed * Time.deltaTime);

            // Return to pool when off-screen
            if (transform.position.y > _topBound)
                ReturnToPool();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Damage is handled by EnemyBase (Day 2) — just return to pool on hit
            if (other.CompareTag(Utils.Constants.TAG_ENEMY))
                ReturnToPool();
        }

        public int GetDamage() => damage;

        public void SetDamage(int newDamage) => damage = newDamage;

        private void ReturnToPool()
        {
            _pool?.Release(this);
        }
    }
}
