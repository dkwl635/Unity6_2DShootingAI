// Attach to: Bullet Prefab

using UnityEngine;
using ShooterGame.Effects;
using ShooterGame.Enemy;

namespace ShooterGame.Player
{
    public class Bullet : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 12f;
        [SerializeField] private int   damage    = 10;

        [Header("Homing")]
        [SerializeField] private float _turnSpeed = 220f;  // degrees/sec

        private Camera     _cam;
        private BulletPool _pool;
        private float      _topBound;
        private float      _bottomBound;
        private float      _halfWidth;

        // ── Homing state ─────────────────────────────────────────────
        private bool      _homing;
        private Transform _target;
        private float     _targetRefreshTimer;
        private int       _enemyLayerMask;

        private static readonly Collider2D[] _searchBuffer = new Collider2D[32];

        // ── Pool interface ────────────────────────────────────────────

        public void Initialize(BulletPool pool)
        {
            _pool           = pool;
            _cam            = Camera.main;
            _topBound       = _cam.ViewportToWorldPoint(new Vector3(0.5f, 1.1f, _cam.nearClipPlane)).y;
            _bottomBound    = _cam.ViewportToWorldPoint(new Vector3(0.5f, -0.1f, _cam.nearClipPlane)).y;
            _halfWidth      = _cam.ViewportToWorldPoint(new Vector3(1.1f, 0.5f, _cam.nearClipPlane)).x;
            _enemyLayerMask = LayerMask.GetMask(Utils.Constants.LAYER_ENEMY);
            _homing         = false;
            _target         = null;
            _targetRefreshTimer = 0f;
        }

        public void SetHoming(bool homing)
        {
            _homing = homing;
            if (homing) RefreshTarget();
        }

        public int  GetDamage()            => damage;
        public void SetDamage(int d)       => damage = d;

        // ── Update ────────────────────────────────────────────────────

        private void Update()
        {
            if (_homing)
                UpdateHoming();
            else
                UpdateStraight();
        }

        private void UpdateStraight()
        {
            transform.Translate(Vector3.up * moveSpeed * Time.deltaTime);
            if (transform.position.y > _topBound) ReturnToPool();
        }

        private void UpdateHoming()
        {
            _targetRefreshTimer -= Time.deltaTime;
            if (_targetRefreshTimer <= 0f ||
                (_target != null && !_target.gameObject.activeInHierarchy))
            {
                RefreshTarget();
                _targetRefreshTimer = 0.2f;
            }

            if (_target != null && _target.gameObject.activeInHierarchy)
            {
                Vector2 toTarget   = (Vector2)_target.position - (Vector2)transform.position;
                float   targetAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg - 90f;
                float   current    = transform.eulerAngles.z;
                float   next       = Mathf.MoveTowardsAngle(current, targetAngle,
                                         _turnSpeed * Time.deltaTime);
                transform.rotation = Quaternion.Euler(0f, 0f, next);
            }

            transform.Translate(Vector3.up * moveSpeed * Time.deltaTime);

            Vector3 p = transform.position;
            if (p.y > _topBound || p.y < _bottomBound || Mathf.Abs(p.x) > _halfWidth)
                ReturnToPool();
        }

        private void RefreshTarget()
        {
            int count = Physics2D.OverlapCircleNonAlloc(
                transform.position, 30f, _searchBuffer, _enemyLayerMask);
            Transform best     = null;
            float     bestDist = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                float d = Vector2.SqrMagnitude(
                    _searchBuffer[i].transform.position - transform.position);
                if (d < bestDist) { bestDist = d; best = _searchBuffer[i].transform; }
            }
            _target = best;
        }

        // ── Collision ─────────────────────────────────────────────────

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag(Utils.Constants.TAG_ENEMY))
            {
                EffectManager.Instance?.Play(EffectType.BulletHit, transform.position);
                other.GetComponent<EnemyBase>()?.TakeDamage(damage);
                ReturnToPool();
            }
        }

        private void ReturnToPool() => _pool?.Release(this);
    }
}
