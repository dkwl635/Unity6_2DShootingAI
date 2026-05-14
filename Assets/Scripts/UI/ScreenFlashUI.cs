// Attach to: ScreenFlashPanel (child of HUD Canvas, full-screen, Raycast Target OFF)
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ShooterGame.UI
{
    public class ScreenFlashUI : MonoBehaviour
    {
        public static ScreenFlashUI Instance { get; private set; }

        [SerializeField] private Image _flashImage;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (_flashImage != null)
                _flashImage.color = Color.clear;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Flash(Color color, float fadeIn, float hold, float fadeOut)
        {
            StopAllCoroutines();
            StartCoroutine(FlashRoutine(color, fadeIn, hold, fadeOut));
        }

        private IEnumerator FlashRoutine(Color color, float fadeIn, float hold, float fadeOut)
        {
            float t = 0f;
            while (t < fadeIn)
            {
                t += Time.deltaTime;
                SetAlpha(color, Mathf.Clamp01(t / fadeIn));
                yield return null;
            }
            SetAlpha(color, 1f);

            yield return new WaitForSeconds(hold);

            t = 0f;
            while (t < fadeOut)
            {
                t += Time.deltaTime;
                SetAlpha(color, Mathf.Clamp01(1f - t / fadeOut));
                yield return null;
            }
            SetAlpha(color, 0f);
        }

        private void SetAlpha(Color color, float alpha)
        {
            if (_flashImage == null) return;
            _flashImage.color = new Color(color.r, color.g, color.b, alpha);
        }
    }
}
