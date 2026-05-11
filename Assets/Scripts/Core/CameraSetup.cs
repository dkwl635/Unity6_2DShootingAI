// Attach to: Main Camera GameObject
// Responsibility: Lock the camera viewport to a fixed 9:16 aspect ratio.
// Regions outside the play area are filled with solid black (letterbox / pillarbox).
//
// How it works:
//   - Target aspect = PLAY_HALF_WIDTH / PLAY_HALF_HEIGHT (9:16 = 0.5625)
//   - Calculate the viewport rect so only the 9:16 region is rendered.
//   - Camera background = black → exposed regions appear black automatically.
//   - orthographicSize is fixed to PLAY_HALF_HEIGHT (constant world units).

using UnityEngine;
using ShooterGame.Utils;

namespace ShooterGame.Core
{
    [RequireComponent(typeof(Camera))]
    public class CameraSetup : MonoBehaviour
    {
        // Cached — set once in Awake
        private Camera _cam;

        // Target aspect ratio (9:16 portrait)
        private const float TARGET_ASPECT = Constants.PLAY_HALF_WIDTH / Constants.PLAY_HALF_HEIGHT;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            _cam.orthographic     = true;
            _cam.orthographicSize = Constants.PLAY_HALF_HEIGHT;
            _cam.backgroundColor  = Color.black;
            _cam.clearFlags       = CameraClearFlags.SolidColor;

            ApplyLetterbox();
        }

        private void ApplyLetterbox()
        {
            float deviceAspect = (float)Screen.width / Screen.height;

            if (Mathf.Approximately(deviceAspect, TARGET_ASPECT))
            {
                // Perfect match — full screen
                _cam.rect = new Rect(0f, 0f, 1f, 1f);
                return;
            }

            if (deviceAspect > TARGET_ASPECT)
            {
                // Device is WIDER than 9:16 → pillarbox (black bars on left & right)
                float normalizedWidth = TARGET_ASPECT / deviceAspect;
                float xOffset         = (1f - normalizedWidth) * 0.5f;
                _cam.rect = new Rect(xOffset, 0f, normalizedWidth, 1f);
            }
            else
            {
                // Device is NARROWER than 9:16 → letterbox (black bars on top & bottom)
                float normalizedHeight = deviceAspect / TARGET_ASPECT;
                float yOffset          = (1f - normalizedHeight) * 0.5f;
                _cam.rect = new Rect(0f, yOffset, 1f, normalizedHeight);
            }

            Debug.Log($"[CameraSetup] device={deviceAspect:F4} target={TARGET_ASPECT:F4} rect={_cam.rect}");
        }

#if UNITY_EDITOR
        // Re-apply when Game View is resized in editor
        private float _lastAspect;

        private void Update()
        {
            float current = (float)Screen.width / Screen.height;
            if (Mathf.Abs(current - _lastAspect) > 0.001f)
            {
                _lastAspect = current;
                ApplyLetterbox();
            }
        }
#endif
    }
}
