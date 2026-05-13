// Attach to: SaveManager GameObject (DontDestroyOnLoad)
// Responsibility: All persistent data read/write only (PlayerPrefs → JSON migration later).
// Does NOT contain game logic — only data access.

using UnityEngine;
using ShooterGame.Utils;

namespace ShooterGame.Meta
{
    public class SaveManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────
        public static SaveManager Instance { get; private set; }

        // ── Cached Data ──────────────────────────────────────────
        public int BestScore  { get; private set; }
        public int TotalCoins { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAll();
        }

        // ── Public API ───────────────────────────────────────────

        /// <summary>Updates best score only if the new score is higher.</summary>
        public void TrySaveBestScore(int score)
        {
            if (score <= BestScore) return;
            BestScore = score;
            PlayerPrefs.SetInt(Constants.PREF_BEST_SCORE, BestScore);
            PlayerPrefs.Save();
        }

        /// <summary>Adds coins to the running total and persists immediately.</summary>
        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            TotalCoins += amount;
            PlayerPrefs.SetInt(Constants.PREF_TOTAL_COINS, TotalCoins);
            PlayerPrefs.Save();
        }

        /// <summary>코인 차감 — 잔액이 부족하면 0으로 클램프.</summary>
        public void SpendCoins(int cost)
        {
            if (cost <= 0) return;
            TotalCoins = Mathf.Max(0, TotalCoins - cost);
            PlayerPrefs.SetInt(Constants.PREF_TOTAL_COINS, TotalCoins);
            PlayerPrefs.Save();
        }

        public int GetUpgradeLevel(LobbyUpgradeType type)
            => PlayerPrefs.GetInt(Constants.PREF_LOBBY_UPGRADE + (int)type, 0);

        public void SetUpgradeLevel(LobbyUpgradeType type, int level)
        {
            PlayerPrefs.SetInt(Constants.PREF_LOBBY_UPGRADE + (int)type, level);
            PlayerPrefs.Save();
        }

        /// <summary>Force-save all dirty data (called on app pause/quit).</summary>
        public void ForceSave()
        {
            PlayerPrefs.SetInt(Constants.PREF_BEST_SCORE,  BestScore);
            PlayerPrefs.SetInt(Constants.PREF_TOTAL_COINS, TotalCoins);
            PlayerPrefs.Save();
        }

        // ── Private ──────────────────────────────────────────────

        private void LoadAll()
        {
            BestScore  = PlayerPrefs.GetInt(Constants.PREF_BEST_SCORE,  0);
            TotalCoins = PlayerPrefs.GetInt(Constants.PREF_TOTAL_COINS, 0);
            // upgrade levels are read on-demand via GetUpgradeLevel()
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
