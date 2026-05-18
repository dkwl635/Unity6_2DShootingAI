// Attach to: BossWarningPanel GameObject (Canvas child, Game scene — initially inactive)
using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;

namespace ShooterGame.UI
{
    public class BossWarningPanel : MonoBehaviour
    {
        public event Action OnComplete; // 패널이 완전히 끝났을 때 발행
        [Header("Drop Animation")]
        [SerializeField] private float _startY       = 19.009f;
        [SerializeField] private float _endY         = 5.946f;
        [SerializeField] private float _dropDuration = 0.6f;
        [SerializeField] private Ease  _dropEase     = Ease.OutBounce;

        [Header("Expand Animation")]
        [SerializeField] private float _targetHeight    = 450f;
        [SerializeField] private float _expandDuration  = 0.4f;
        [SerializeField] private Ease  _expandEase      = Ease.OutBounce;

        [Header("Warning Display")]
        [SerializeField] private GameObject _WarningMark;
        [SerializeField] private GameObject _WarningText;
        [SerializeField] private float      _displayDuration = 2f;   // 깜빡임 총 지속 시간
        [SerializeField] private float      _blinkInterval   = 0.2f; // 깜빡임 간격

        private RectTransform  _rect;
        private WaitForSeconds _blinkWait;
        private Coroutine      _routine;

        private void Awake()
        {
            _rect      = GetComponent<RectTransform>();
            _blinkWait = new WaitForSeconds(_blinkInterval);
        }

        public void Show()
        {
            if (_routine != null) { StopCoroutine(_routine); _routine = null; }
            DOTween.Kill(transform);
            DOTween.Kill(_rect);

            _WarningMark?.SetActive(false);
            _WarningText?.SetActive(false);

            // 높이 0으로 초기화
            if (_rect != null)
                _rect.sizeDelta = new Vector2(_rect.sizeDelta.x, 0f);

            // 시작 위치 세팅 후 활성화
            Vector3 pos = transform.position;
            pos.y = _startY;
            transform.position = pos;
            gameObject.SetActive(true);

            // 1단계: 셔터처럼 낙하
            transform.DOMoveY(_endY, _dropDuration)
                .SetEase(_dropEase)
                .OnComplete(ExpandHeight);
        }

        // 2단계: 높이 0 → _targetHeight (셔터 펼쳐짐)
        private void ExpandHeight()
        {
            if (_rect == null) { ActivateWarnings(); return; }

            _rect.DOSizeDelta(
                    new Vector2(_rect.sizeDelta.x, _targetHeight),
                    _expandDuration)
                .SetEase(_expandEase)
                .OnComplete(ActivateWarnings);
        }

        // 3단계: 경고 오브젝트 활성화 + 깜빡임
        private void ActivateWarnings()
        {
            _routine = StartCoroutine(BlinkThenHide());
        }

        private IEnumerator BlinkThenHide()
        {
            float elapsed = 0f;
            while (elapsed < _displayDuration)
            {
                _WarningMark?.SetActive(true);
                _WarningText?.SetActive(true);
                yield return _blinkWait;

                _WarningMark?.SetActive(false);
                _WarningText?.SetActive(false);
                yield return _blinkWait;

                elapsed += _blinkInterval * 2f;
            }

            gameObject.SetActive(false);
            _routine = null;
            OnComplete?.Invoke();
        }
    }
}
