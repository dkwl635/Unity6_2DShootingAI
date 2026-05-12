// Attach to: CoinDrop prefab / ExpDrop prefab
using System;
using UnityEngine;
using ShooterGame.Utils;

namespace ShooterGame.Economy
{
    public abstract class DropBase : MonoBehaviour
    {
        [SerializeField] protected float fallSpeed = 2f;

        private Action _onRelease;
        private float  _bottomBound;
        private bool   _released;

        private void Awake()
        {
            _bottomBound = -(Constants.PLAY_HALF_HEIGHT + 1f);
        }

        protected virtual void OnEnable()
        {
            _released = false;
        }

        public void Initialize(Vector3 position, Action onRelease)
        {
            transform.position = position;
            _onRelease = onRelease;
        }

        private void Update()
        {
            if (_released) return;
            transform.Translate(Vector3.down * (fallSpeed * Time.deltaTime));
            if (transform.position.y < _bottomBound)
                ReturnToPool();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_released) return;
            if (other.CompareTag(Constants.TAG_PLAYER))
            {
                OnCollect();
                ReturnToPool();
            }
        }

        protected abstract void OnCollect();

        protected void ReturnToPool()
        {
            if (_released) return;
            _released = true;
            _onRelease?.Invoke();
            _onRelease = null;
        }
    }
}
