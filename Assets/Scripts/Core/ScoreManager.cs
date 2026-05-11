// Attach to: ScoreManager GameObject (Game scene only — no DontDestroyOnLoad)
// Responsibility: Score accumulation and change notification only.
// Does NOT save data — that is SaveManager's job.

using System;
using UnityEngine;

namespace ShooterGame.Core
{
    public class ScoreManager : MonoBehaviour
    {
        // ── Singleton (scene-scoped) ──────────────────────────────
        public static ScoreManager Instance { get; private set; }

        // ── Events ───────────────────────────────────────────────
        public event Action<int> OnScoreChanged;

        // ── State ────────────────────────────────────────────────
        public int Score { get; private set; }

        private void Awake()
        {
            // Singleton duplicate guard (scene-scoped)
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Subscribe to game-over to reset for next session
            if (InGameManager.Instance != null)
                InGameManager.Instance.OnGameStart += ResetScore;
        }

        // ── Public API ───────────────────────────────────────────

        public void Add(int amount)
        {
            if (amount <= 0) return;
            Score += amount;
            OnScoreChanged?.Invoke(Score);
        }

        private void ResetScore()
        {
            Score = 0;
            OnScoreChanged?.Invoke(Score);
        }

        private void OnDestroy()
        {
            // Unsubscribe to prevent ghost listeners
            if (InGameManager.Instance != null)
                InGameManager.Instance.OnGameStart -= ResetScore;

            if (Instance == this) Instance = null;
        }
    }
}
