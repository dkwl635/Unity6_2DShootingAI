// Attach to: JoystickZone GameObject (large transparent panel covering the input area, child of Canvas)
// background and handle are children of JoystickZone or separate visual children.

using UnityEngine;
using UnityEngine.EventSystems;

namespace ShooterGame.UI
{
    /// <summary>
    /// Floating virtual joystick — the background circle snaps to wherever the finger first touches.
    /// The script must be on a large transparent RectTransform (the touch zone).
    /// background / handle are purely visual and are moved at runtime.
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        // ── Inspector ─────────────────────────────────────────────
        [SerializeField] private RectTransform background;  // outer circle (visual only)
        [SerializeField] private RectTransform handle;      // inner movable knob

        [Tooltip("How far the handle can move from the center (in UI pixels).")]
        [SerializeField] private float handleRange = 80f;

        [Tooltip("Fraction of handleRange that counts as dead zone (0~1).")]
        [SerializeField] private float deadZone = 0.1f;

        // ── Public Output ─────────────────────────────────────────
        /// <summary>Normalized direction [-1, 1] on each axis. Zero when not pressed.</summary>
        public Vector2 Direction { get; private set; }

        /// <summary>Magnitude [0, 1] — how far from center the handle is.</summary>
        public float Magnitude { get; private set; }

        public bool IsPressed { get; private set; }

        // ── Private ───────────────────────────────────────────────
        private Canvas        _canvas;
        private RectTransform _parentRect;
        private Vector2       _bgCenter;   // background center in screen pixels

        private void Awake()
        {
            _canvas     = GetComponentInParent<Canvas>();
            _parentRect = background.parent as RectTransform;

            // Hide background until the player touches
            background.gameObject.SetActive(false);
            handle.gameObject.SetActive(false);
        }

        // ── Pointer Events ────────────────────────────────────────

        public void OnPointerDown(PointerEventData eventData)
        {
            IsPressed = true;

            // Move background to the exact touch start position
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _parentRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            {
                background.anchoredPosition = localPoint;
            }

            background.gameObject.SetActive(true);
            handle.gameObject.SetActive(true);

            // Recalculate center after move
            _bgCenter = RectTransformUtility.WorldToScreenPoint(
                _canvas.worldCamera, background.position);

            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Raw offset from background center to finger in screen pixels
            Vector2 rawOffset = eventData.position - _bgCenter;

            // Scale by canvas scale factor so layout-independent
            float scaleFactor = _canvas.scaleFactor > 0f ? _canvas.scaleFactor : 1f;
            Vector2 localOffset = rawOffset / scaleFactor;

            float distance = localOffset.magnitude;

            // Clamp handle within handleRange
            Vector2 clampedOffset = distance > handleRange
                ? localOffset.normalized * handleRange
                : localOffset;

            // Handle is a sibling of background, so offset must be relative to background's position
            handle.anchoredPosition = background.anchoredPosition + clampedOffset;

            // Calculate normalized magnitude and apply dead zone
            float normalizedMag = Mathf.Clamp01(distance / handleRange);
            if (normalizedMag < deadZone)
            {
                Direction = Vector2.zero;
                Magnitude = 0f;
            }
            else
            {
                // Remap magnitude from [deadZone, 1] → [0, 1]
                Magnitude = (normalizedMag - deadZone) / (1f - deadZone);
                Direction = localOffset.normalized;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            ResetInput();
        }

        public void ResetInput()
        {
            IsPressed = false;
            Direction = Vector2.zero;
            Magnitude = 0f;
            handle.anchoredPosition     = Vector2.zero;
            background.gameObject.SetActive(false);
            handle.gameObject.SetActive(false);
        }
    }
}
