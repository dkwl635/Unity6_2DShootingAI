// Attach to: CoinDrop prefab / PowerDrop prefab
using System;
using UnityEngine;
using ShooterGame.Core;
using ShooterGame.Effects;
using ShooterGame.Utils;

namespace ShooterGame.Economy
{
    public abstract class DropBase : MonoBehaviour
    {
        [SerializeField] protected float fallSpeed = 2f;

        [SerializeField] private float attractDelay = 0.4f;  // 스폰 직후 흡인 면역 시간

        private Action _onRelease;
        private float  _bottomBound;
        private bool   _released;

        private Transform _attractTarget;
        private float     _attractSpeed;
        private float     _spawnTime;

     
        private void Awake()
        {
            _bottomBound = -(Constants.PLAY_HALF_HEIGHT + 1f);
        }

        protected virtual void OnEnable()
        {
            _released      = false;
            _attractTarget = null;
            _spawnTime     = Time.time;
      
        }

        public void Initialize(Vector3 position, Action onRelease)
        {
            transform.position = position;
            _onRelease = onRelease;
        }

        private void Update()
        {
            if (_released) return;

            if (_attractTarget != null)
                transform.position = Vector3.MoveTowards(
                    transform.position, _attractTarget.position, _attractSpeed * Time.deltaTime);
            else
                transform.Translate(Vector3.down * (fallSpeed * Time.deltaTime));

            if (transform.position.y < _bottomBound)
                ReturnToPool();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_released) return;
            if (other.CompareTag(Constants.TAG_PLAYER))
            {
                EffectManager.Instance?.Play(PickupEffect, transform.position);
                AudioManager.Instance?.PlaySFX(PickupSfx);
                OnCollect();
                ReturnToPool();
            }
        }

        public void Attract(Transform target, float speed)
        {
            if (Time.time < _spawnTime + attractDelay) return;
            _attractTarget = target;
            _attractSpeed  = speed;
        }

        protected abstract void OnCollect();
        protected virtual EffectType PickupEffect => EffectType.CoinPickup;
        protected virtual SfxType   PickupSfx    => SfxType.CoinPickup;

        protected void ReturnToPool()
        {
            if (_released) return;
            _released      = true;
            _attractTarget = null;         
            _onRelease?.Invoke();
            _onRelease = null;
        }
    }
}
