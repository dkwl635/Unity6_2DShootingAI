// Attach to: InGameManager GameObject (Game scene only — no DontDestroyOnLoad)
// Responsibility: In-game session state only — running flag, elapsed time, game-over trigger.
// Communicates with ScoreManager and SaveManager via events, not direct calls.

using System;
using UnityEngine;
using ShooterGame.Meta;

namespace ShooterGame.Core
{
    public class InGameManager : MonoBehaviour
    {
        // ── Singleton (scene-scoped) ──────────────────────────────
        public static InGameManager Instance { get; private set; }

        // ── Events ───────────────────────────────────────────────
        public event Action OnGameStart;
        public event Action OnGameOver;

        // ── State ────────────────────────────────────────────────
        public bool  IsGameRunning { get; private set; }
        public float ElapsedTime   { get; private set; }

        private void Awake()
        {
            // Singleton duplicate guard (scene-scoped)
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            // NOTE: intentionally no DontDestroyOnLoad — destroyed with Game scene
        }

        private void Start()
        {
            StartGame();
        }

        private void Update()
        {
            if (!IsGameRunning) return;
            ElapsedTime += Time.deltaTime;
        }

        // ── Public API ───────────────────────────────────────────

        public void StartGame()
        {
            ElapsedTime   = 0f;
            IsGameRunning = true;
            OnGameStart?.Invoke();
        }

        public void TriggerGameOver()
        {
            if (!IsGameRunning) return;

            IsGameRunning = false;
            OnGameOver?.Invoke();

            // Save best score via SaveManager
            int finalScore = ScoreManager.Instance != null ? ScoreManager.Instance.Score : 0;
            SaveManager.Instance?.TrySaveBestScore(finalScore);
        }

        private void OnDestroy()
        {
            // Clear singleton ref when scene unloads
            if (Instance == this) Instance = null;
        }
    }
}
