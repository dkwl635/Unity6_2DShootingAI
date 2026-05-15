// Attach to: Player GameObject
using UnityEngine;
using ShooterGame.UI;
using ShooterGame.Utils;

namespace ShooterGame.Player
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float          maxMoveSpeed = 10f;
        [SerializeField] private VirtualJoystick joystick;

        private Camera      _cam;
        private PlayerStats _playerStats;
        private float       _minX, _maxX, _minY, _maxY;

        private void Awake()
        {
            _cam         = Camera.main;
            _playerStats = GetComponent<PlayerStats>();
        }

        private void Start()
        {
            CalculateBounds();
            if (_playerStats != null)
                _playerStats.OnDied += Respawn;
        }

        private void Update()
        {
            if (joystick == null || !joystick.IsPressed) return;
            ApplyMovement(joystick.Direction, joystick.Magnitude);
        }

        // ── Respawn ───────────────────────────────────────────────

        private void Respawn()
        {
            if (_playerStats.Lives <= 0) return;
            transform.position = new Vector3(0f, _minY, 0f);
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

        private void CalculateBounds()
        {
            float margin = Constants.SCREEN_BOUND_MARGIN;

            _minX = -Constants.PLAY_HALF_WIDTH  + margin;
            _maxX =  Constants.PLAY_HALF_WIDTH  - margin;
            _minY = -Constants.PLAY_HALF_HEIGHT + margin;
            _maxY =  Constants.PLAY_HALF_HEIGHT - margin;
        }

        private void OnDestroy()
        {
            if (_playerStats != null)
                _playerStats.OnDied -= Respawn;
        }
    }
}
