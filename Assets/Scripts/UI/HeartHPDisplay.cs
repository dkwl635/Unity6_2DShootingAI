// Attach to: HeartHPDisplay GameObject (child of HUDController)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ShooterGame.Player;

namespace ShooterGame.UI
{
    public class HeartHPDisplay : MonoBehaviour
    {
        [SerializeField] private PlayerStats _playerStats;
        [SerializeField] private Transform   _container;
        [SerializeField] private Sprite      _heartFull;
        [SerializeField] private Sprite      _heartEmpty;
        [SerializeField] private Vector2     _heartSize  = new Vector2(64f, 64f);
        [SerializeField] private Color       _fullColor  = new Color(1f, 0.25f, 0.35f, 1f);
        [SerializeField] private Color       _emptyColor = new Color(0.25f, 0.25f, 0.25f, 0.45f);

        [SerializeField] private float _hitShakeMag      = 6f;
        [SerializeField] private float _hitShakeDuration = 0.18f;

        private readonly List<Image>   _hearts    = new List<Image>();
        private WaitForSecondsRealtime _shakeWait;

        private void Awake()
        {
            _shakeWait = new WaitForSecondsRealtime(0.03f);
        }

        private void Start()
        {
            if (_playerStats == null) return;
            _playerStats.OnLivesChanged += OnLivesChanged;
            _playerStats.OnDied         += OnDied;

            BuildHearts(_playerStats.MaxLives);
            Refresh(_playerStats.Lives, _playerStats.MaxLives);
        }

        // ── Callbacks ────────────────────────────────────────────────

        private void OnLivesChanged(int current, int max)
        {
            if (_hearts.Count != max)
                BuildHearts(max);
            Refresh(current, max);
        }

        private void OnDied()
        {
            StartCoroutine(ShakeContainer());
        }

        // ── Private ──────────────────────────────────────────────────

        private void Refresh(int current, int max)
        {
            for (int i = 0; i < _hearts.Count; i++)
            {
                bool alive = i < current;
                _hearts[i].sprite = alive
                    ? _heartFull
                    : (_heartEmpty != null ? _heartEmpty : _heartFull);
                _hearts[i].color = alive ? _fullColor : _emptyColor;
            }
        }

        private void BuildHearts(int count)
        {
            foreach (var h in _hearts)
                if (h != null) Destroy(h.gameObject);
            _hearts.Clear();

            for (int i = 0; i < count; i++)
            {
                var go = new GameObject($"Heart_{i}", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_container, false);
                var rt = (RectTransform)go.transform;
                rt.sizeDelta = _heartSize;
                var img = go.GetComponent<Image>();
                img.sprite = _heartFull;
                img.color  = _fullColor;
                img.preserveAspect = true;
                _hearts.Add(img);
            }
        }

        private IEnumerator ShakeContainer()
        {
            var rt   = (RectTransform)_container;
            var orig = rt.anchoredPosition;
            int steps = Mathf.RoundToInt(_hitShakeDuration / 0.03f);
            for (int i = 0; i < steps; i++)
            {
                float t = 1f - (float)i / steps;
                float m = _hitShakeMag * t;
                rt.anchoredPosition = orig + new Vector2(
                    Random.Range(-m, m), Random.Range(-m, m));
                yield return _shakeWait;
            }
            rt.anchoredPosition = orig;
        }

        private void OnDestroy()
        {
            if (_playerStats != null)
            {
                _playerStats.OnLivesChanged -= OnLivesChanged;
                _playerStats.OnDied         -= OnDied;
            }
        }
    }
}
