// Attach to: (static utility — no GameObject attachment needed)

namespace ShooterGame.Utils
{
    public static class Constants
    {
        // ── Tags ────────────────────────────────────────────────
        public const string TAG_PLAYER = "Player";
        public const string TAG_ENEMY  = "Enemy";
        public const string TAG_COIN   = "Coin";
        public const string TAG_BULLET = "Bullet";

        // ── Layers ──────────────────────────────────────────────
        public const string LAYER_PLAYER = "Player";
        public const string LAYER_ENEMY  = "Enemy";
        public const string LAYER_COIN   = "Coin";
        public const string LAYER_BULLET = "Bullet";

        // ── PlayerPrefs Keys ────────────────────────────────────
        public const string PREF_BEST_SCORE    = "BestScore";
        public const string PREF_TOTAL_COINS  = "TotalCoins";
        public const string PREF_LOBBY_UPGRADE = "LobbyUpgrade_"; // append (int)LobbyUpgradeType

        // ── Object Pool Default Sizes ───────────────────────────
        public const int POOL_SIZE_BULLET = 30;
        public const int POOL_SIZE_ENEMY  = 20;
        public const int POOL_SIZE_COIN   = 40;
        public const int POOL_SIZE_POWER  = 40;
        public const int POOL_SIZE_EFFECT = 20;

        // ── Reference Resolution ─────────────────────────────────
        // All input delta calculations use this fixed base resolution.
        // Ensures consistent movement speed regardless of device screen size.
        public const float REF_WIDTH  = 1080f;
        public const float REF_HEIGHT = 1920f;

        // ── Play Area (World Units, fixed across all devices) ────
        // Camera orthographicSize is derived from these values at runtime.
        // 9:16 portrait baseline — half-extents in world units.
        public const float PLAY_HALF_HEIGHT = 10f;  // top/bottom ±10 world units
        public const float PLAY_HALF_WIDTH  = 5.625f; // 10 * (9/16) = 5.625

        // ── Gameplay ─────────────────────────────────────────────
        public const int   TARGET_FRAME_RATE   = 60;
        public const float SCREEN_BOUND_MARGIN = 0.5f; // world-unit margin for player clamping
        public const float BOSS_CENTER_Y       = 1f;   // center Y position for boss spawn
    }
}
