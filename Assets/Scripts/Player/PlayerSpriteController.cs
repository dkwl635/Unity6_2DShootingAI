// Attach to: Player GameObject
using UnityEngine;
using ShooterGame.UI;

namespace ShooterGame.Player
{
    public class PlayerSpriteController : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;   // Sprite child의 SpriteRenderer
        [SerializeField] private Sprite         idleSprite;       // 기본 상태 이미지
        [SerializeField] private Sprite         moveSprite;       // 좌우 이동 이미지
        [SerializeField] private VirtualJoystick joystick;
        [SerializeField] private float           horizontalThreshold = 0.1f;

        private void Update()
        {
            if (joystick == null || spriteRenderer == null) return;

            if (Time.timeScale == 0f)
            {
                spriteRenderer.sprite = idleSprite;
                spriteRenderer.flipX  = false;
                return;
            }

            float horizontal = joystick.IsPressed ? joystick.Direction.x : 0f;

           
            if (Mathf.Abs(horizontal) > horizontalThreshold)
            {
                spriteRenderer.sprite  = moveSprite;
                spriteRenderer.flipX   = horizontal > 0f;  // 우측 이동: 좌 이미지를 플립
            }
            else
            {
                spriteRenderer.sprite  = idleSprite;
                spriteRenderer.flipX   = false;
            }
        }
    }
}
