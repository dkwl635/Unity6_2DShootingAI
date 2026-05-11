// Attach to: BackgroundScroller GameObject (contains two child SpriteRenderer GameObjects)

using UnityEngine;

namespace ShooterGame.Core
{
    /// <summary>
    /// Infinite background scroll using two sprites that leapfrog each other.
    /// Place two background sprites as children (bg0, bg1) stacked vertically.
    /// </summary>
    public class BackgroundScroller : MonoBehaviour
    {
        [SerializeField] private Transform bg0;
        [SerializeField] private Transform bg1;

        [SerializeField] private float scrollSpeed = 3f;

        // Height of one background tile in world units (set to match sprite height)
        [SerializeField] private float tileHeight = 20f;

        private void Update()
        {
            // Move both tiles downward
            Vector3 delta = Vector3.down * scrollSpeed * Time.deltaTime;
            bg0.Translate(delta);
            bg1.Translate(delta);

            // When a tile scrolls fully off the bottom, jump it above the other tile
            if (bg0.position.y < -tileHeight)
                bg0.position = new Vector3(bg0.position.x, bg1.position.y + tileHeight, bg0.position.z);

            if (bg1.position.y < -tileHeight)
                bg1.position = new Vector3(bg1.position.x, bg0.position.y + tileHeight, bg1.position.z);
        }
    }
}
