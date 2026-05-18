// Attach to: Player GameObject
using System.Collections;
using UnityEngine;
using ShooterGame.Economy;

namespace ShooterGame.Player
{
    public class MagnetEffect : MonoBehaviour
    {
        public static MagnetEffect Instance { get; private set; }

        [SerializeField] private float     magnetRadius   = 1.5f;
        [SerializeField] private float     attractSpeed   = 8f;
        [SerializeField] private int       maxHits        = 80;
        [SerializeField] private float     detectInterval = 0.02f;
        [SerializeField] private LayerMask _dropLayerMask; // Inspector에서 Coin + Power 레이어 체크

        [Header("FX")]
        [SerializeField] private Transform _radiusFX;
        [SerializeField] private float     _baseRadius = 1.5f; // scale 1일 때의 반경 (magnetRadius 기본값과 맞추기)

        private Collider2D[]   _hits;
        private WaitForSeconds _detectWait;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance    = this;
            _hits       = new Collider2D[maxHits];
            _detectWait = new WaitForSeconds(detectInterval);
            UpdateFXScale();
        }

        private void OnEnable()  => StartCoroutine(DetectRoutine());
        private void OnDisable() => StopAllCoroutines();

        private IEnumerator DetectRoutine()
        {
            while (true)
            {
                int count = Physics2D.OverlapCircleNonAlloc(
                    transform.position, magnetRadius, _hits, _dropLayerMask);

                for (int i = 0; i < count; i++)
                {
                    if (_hits[i] == null) continue;
                    DropBase drop = _hits[i].GetComponent<DropBase>();
                    drop?.Attract(transform, attractSpeed);
                }

                yield return _detectWait;
            }
        }

        public float MagnetRadius => magnetRadius;

        public void IncreaseRadius(float amount)
        {
            magnetRadius = Mathf.Max(0f, magnetRadius + amount);
            UpdateFXScale();
        }

        public void ApplyPermanentMagnetBonus(float totalGain)
        {
            if (totalGain <= 0f) return;
            IncreaseRadius(totalGain);
        }

        private void UpdateFXScale()
        {
            if (_radiusFX == null || _baseRadius <= 0f) return;
            _radiusFX.localScale = Vector3.one * (magnetRadius / _baseRadius);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0f, 0.8f, 1f, 0.3f);
            Gizmos.DrawSphere(transform.position, magnetRadius);
            Gizmos.color = new Color(0f, 0.8f, 1f, 1f);
            Gizmos.DrawWireSphere(transform.position, magnetRadius);
        }
#endif
    }
}
