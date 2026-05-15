// Attach to: InGameManager GameObject (Game scene only — no DontDestroyOnLoad)
// Responsibility: In-game session state only — running flag, elapsed time, game-over trigger.

using System;
using UnityEngine;
using ShooterGame.Economy;
using ShooterGame.Meta;
using ShooterGame.Player;

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

        // ── Permanent Bonus References ───────────────────────────
        [SerializeField] private PlayerStats       _playerStats;
        [SerializeField] private PlayerShooter     _playerShooter;
        [SerializeField] private MagnetEffect      _magnetEffect;
        // 배열 인덱스 = (int)LobbyUpgradeType 순서와 일치
        [SerializeField] private LobbyUpgradeData[] _lobbyUpgrades;

        private void Awake()
        {
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
            ApplyPermanentBonuses();
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
            AudioManager.Instance?.PlayGameBGM();
            OnGameStart?.Invoke();
        }

        public void TriggerGameOver()
        {
            if (!IsGameRunning) return;

            IsGameRunning = false;
            AudioManager.Instance?.StopBGM();
            OnGameOver?.Invoke();

            int finalScore  = ScoreManager.Instance != null ? ScoreManager.Instance.Score : 0;
            int earnedCoins = CoinSystem.Instance  != null ? CoinSystem.Instance.Total   : 0;
            SaveManager.Instance?.TrySaveBestScore(finalScore);
            SaveManager.Instance?.AddCoins(earnedCoins);
        }

        // ── Private ──────────────────────────────────────────────

        private void ApplyPermanentBonuses()
        {
            if (SaveManager.Instance == null || _lobbyUpgrades == null) return;

            int livesLevel  = SaveManager.Instance.GetUpgradeLevel(LobbyUpgradeType.MaxLives);
            int dmgLevel    = SaveManager.Instance.GetUpgradeLevel(LobbyUpgradeType.Damage);
            int atkLevel    = SaveManager.Instance.GetUpgradeLevel(LobbyUpgradeType.AttackSpeed);
            int magnetLevel = SaveManager.Instance.GetUpgradeLevel(LobbyUpgradeType.MagnetRange);

            if (livesLevel > 0 && _playerStats != null)
                _playerStats.ApplyPermanentLivesBonus(
                    Mathf.RoundToInt(_lobbyUpgrades[(int)LobbyUpgradeType.MaxLives].GetTotalGain(livesLevel)));

            if (dmgLevel > 0 && _playerShooter != null)
                _playerShooter.ApplyPermanentDamageBonus(
                    Mathf.RoundToInt(_lobbyUpgrades[(int)LobbyUpgradeType.Damage].GetTotalGain(dmgLevel)));

            if (atkLevel > 0 && _playerShooter != null)
                _playerShooter.ApplyPermanentAtkSpeedBonus(
                    _lobbyUpgrades[(int)LobbyUpgradeType.AttackSpeed].GetTotalGain(atkLevel));

            if (magnetLevel > 0 && _magnetEffect != null)
                _magnetEffect.ApplyPermanentMagnetBonus(
                    _lobbyUpgrades[(int)LobbyUpgradeType.MagnetRange].GetTotalGain(magnetLevel));
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
