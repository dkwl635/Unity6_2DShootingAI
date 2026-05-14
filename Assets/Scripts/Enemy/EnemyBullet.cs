// Attach to: EnemyBullet Prefab
using UnityEngine;
using ShooterGame.Player;
using ShooterGame.Utils;

namespace ShooterGame.Enemy
{
    public class EnemyBullet : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 7f;

        private EnemyBulletPool _pool;
        private int             _damage;
        private float           _bottomBound;
        private bool            _released;

        private static Camera _cam;

        public void Initialize(EnemyBulletPool pool, int damage)
        {
            _pool    = pool;
            _damage  = damage;
            _released = false;

            if (_cam == null) _cam = Camera.main;
            _bottomBound = _cam.ViewportToWorldPoint(
                new Vector3(0.5f, -0.1f, _cam.nearClipPlane)).y;
        }

        private void Update()
        {
            transform.Translate(Vector3.up * moveSpeed * Time.deltaTime);
            if (transform.position.y < _bottomBound)
                ReturnToPool();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_released) return;
            if (other.CompareTag(Constants.TAG_PLAYER))
            {
                other.GetComponent<PlayerStats>()?.TakeDamage(_damage);
                ReturnToPool();
            }
        }

        private void ReturnToPool()
        {
            if (_released) return;
            _released = true;
            _pool?.Release(this);
        }
    }
}
