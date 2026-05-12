// Attach to: Player GameObject
using System.Collections;
using UnityEngine;
using ShooterGame.Economy;

namespace ShooterGame.Player
{
    public class MagnetEffect : MonoBehaviour
    {
        public static MagnetEffect Instance { get; private set; }

        [SerializeField] private float magnetRadius = 1.5f;
        [SerializeField] private float attractSpeed = 8f;
        [SerializeField] private int   maxHits      = 80;

        private Collider2D[]   _hits;
        private WaitForSeconds _detectWait;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance    = this;
            _hits       = new Collider2D[maxHits];
            _detectWait = new WaitForSeconds(0.02f);
        }

        private void OnEnable()  => StartCoroutine(DetectRoutine());
        private void OnDisable() => StopAllCoroutines();

        private IEnumerator DetectRoutine()
        {
            while (true)
            {
                int count = Physics2D.OverlapCircleNonAlloc(
                    transform.position, magnetRadius, _hits);

                for (int i = 0; i < count; i++)
                {
                    if (_hits[i] == null) continue;
                    DropBase drop = _hits[i].GetComponent<DropBase>();
                    drop?.Attract(transform, attractSpeed);
                }

                yield return _detectWait;
            }
        }

        public void IncreaseRadius(float amount) => magnetRadius += amount;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
