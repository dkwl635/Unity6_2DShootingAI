// Attach to: Enemy Prefab root
using System;
using UnityEngine;
using ShooterGame.Core;
using ShooterGame.Player;
using ShooterGame.Utils;

namespace ShooterGame.Enemy
{
    public class EnemyBase : MonoBehaviour
    {
        protected int   CurrentHp;
        protected float CurrentSpeed;

        private EnemyData         _data;
        private Action<EnemyBase> _releaseCallback;
        private float             _bottomBound;
        private bool              _released;

        private void Awake()
        {
            _bottomBound = -(Constants.PLAY_HALF_HEIGHT + 1f);
        }

        protected virtual void OnEnable()
        {
            _released = false;
        }

        public void Initialize(EnemyData data, float hpMultiplier, float speedMultiplier,
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
            if (CurrentHp <= 0) Die();
        }

        public void ForceReturnToPool()
        {
            if (_released) return;
            _released = true;
            // Caller (PatternBase) handles pool.Release() directly
        }

        protected virtual void Die()
        {
            ScoreManager.Instance?.Add(_data.ScoreValue);
            ReturnToPool();
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

        private void ReturnToPool()
        {
            if (_released) return;
            _released = true;
            _releaseCallback?.Invoke(this);
        }
    }
}
