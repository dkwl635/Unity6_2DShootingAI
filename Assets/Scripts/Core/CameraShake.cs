// Attach to: Main Camera GameObject (alongside CameraSetup)
using System.Collections;
using UnityEngine;
using ShooterGame.Core;

namespace ShooterGame.Core
{
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        [SerializeField] private float _hitDuration    = 0.15f;
        [SerializeField] private float _hitMagnitude   = 0.08f;
        [SerializeField] private float _deathDuration  = 0.40f;
        [SerializeField] private float _deathMagnitude = 0.20f;

        private Vector3    _originalPos;
        private Coroutine  _shakeRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void Start()
        {
            _originalPos = transform.localPosition;

            if (InGameManager.Instance != null)
                InGameManager.Instance.OnGameOver += ShakeOnDeath;
        }

        // ── Public API ───────────────────────────────────────────

        public void ShakeHit() => Shake(_hitDuration, _hitMagnitude);

        public void Shake(float duration, float magnitude)
        {
            if (_shakeRoutine != null) StopCoroutine(_shakeRoutine);
            _shakeRoutine = StartCoroutine(ShakeRoutine(duration, magnitude));
        }

        // ── Private ──────────────────────────────────────────────

        private void ShakeOnDeath() => Shake(_deathDuration, _deathMagnitude);

        private IEnumerator ShakeRoutine(float duration, float magnitude)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float current = Mathf.Lerp(magnitude, 0f, t); // fade-out

                float x = Random.Range(-1f, 1f) * current;
                float y = Random.Range(-1f, 1f) * current;
                transform.localPosition = _originalPos + new Vector3(x, y, 0f);

                elapsed += Time.unscaledDeltaTime; // works even when timeScale == 0
                yield return null;
            }

            transform.localPosition = _originalPos;
            _shakeRoutine = null;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (InGameManager.Instance != null)
                InGameManager.Instance.OnGameOver -= ShakeOnDeath;
        }
    }
}
