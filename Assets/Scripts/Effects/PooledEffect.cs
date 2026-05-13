// Attach to: Effect Prefab root (alongside ParticleSystem)
using System;
using System.Collections;
using UnityEngine;

namespace ShooterGame.Effects
{
    public class PooledEffect : MonoBehaviour
    {
        private ParticleSystem           _ps;
        private Action<PooledEffect>     _releaseCallback;
        private Coroutine                _returnRoutine;

        private void Awake()
        {
            _ps = GetComponent<ParticleSystem>();
        }

        private void OnDisable()
        {
            if (_returnRoutine != null)
            {
                StopCoroutine(_returnRoutine);
                _returnRoutine = null;
            }
            _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        public void Play(Vector3 position, Action<PooledEffect> releaseCallback)
        {
            transform.position = position;
            _releaseCallback   = releaseCallback;
            _ps.Play();

            if (_returnRoutine != null) StopCoroutine(_returnRoutine);
            _returnRoutine = StartCoroutine(WaitAndRelease());
        }

        private IEnumerator WaitAndRelease()
        {
            yield return null; // 1 프레임 대기 — Play() 직후 IsAlive() 가 false 를 잘못 반환하는 것 방지
            while (_ps.IsAlive(true))
                yield return null;

            _returnRoutine = null;
            _releaseCallback?.Invoke(this);
        }
    }
}
