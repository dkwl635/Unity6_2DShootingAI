// Attach to: Player GameObject

using UnityEngine;
using ShooterGame.UI;
using ShooterGame.Utils;

namespace ShooterGame.Player
{
    /// <summary>
    /// Moves the player based on VirtualJoystick input.
    /// Reads Direction and Magnitude from the joystick each frame,
    /// applies speed scaling, and clamps to screen bounds.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float maxMoveSpeed = 10f;

        [SerializeField] private VirtualJoystick joystick;

        // Cached camera — never access Camera.main in Update
        private Camera _cam;

        // Screen boundary in world units
        private float _minX, _maxX, _minY, _maxY;

        private void Awake()
        {
            _cam = Camera.main;
        }

        private void Start()
        {
            CalculateBounds();
        }

        private void Update()
        {
            if (joystick == null || !joystick.IsPressed) return;

            ApplyMovement(joystick.Direction, joystick.Magnitude);
        }

        // ── Movement ──────────────────────────────────────────────

        private void ApplyMovement(Vector2 direction, float magnitude)
        {
            float speed = maxMoveSpeed * magnitude;

            Vector3 newPos = transform.position
                + new Vector3(direction.x, direction.y, 0f) * speed * Time.deltaTime;

            newPos.x = Mathf.Clamp(newPos.x, _minX, _maxX);
            newPos.y = Mathf.Clamp(newPos.y, _minY, _maxY);

            transform.position = newPos;
        }

        // ── Bounds ────────────────────────────────────────────────
        // Fixed world-unit play area derived from Constants.
        // Same on every device — does NOT depend on camera or screen size.

        private void CalculateBounds()
        {
            float margin = Constants.SCREEN_BOUND_MARGIN;

            _minX = -Constants.PLAY_HALF_WIDTH  + margin;
            _maxX =  Constants.PLAY_HALF_WIDTH  - margin;
            _minY = -Constants.PLAY_HALF_HEIGHT + margin;
            _maxY =  Constants.PLAY_HALF_HEIGHT - margin;
        }
    }
}
