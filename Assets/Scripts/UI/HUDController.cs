// Attach to: HUDController GameObject (Canvas child, Game scene)
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ShooterGame.Core;
using ShooterGame.Economy;
using ShooterGame.Enemy;
using ShooterGame.Player;
using ShooterGame.Upgrade;

namespace ShooterGame.UI
{
    public class HUDController : MonoBehaviour
    {
        [Header("Top Bar")]
        [SerializeField] private TMP_Text _timerText;
        [SerializeField] private TMP_Text _coinText;

        [Header("Power Bar")]
        [SerializeField] private GameObject _powerFillBox;
        [SerializeField] private Image      _powerFill;
        [SerializeField] private TMP_Text   _levelText;
        [SerializeField] private float      _powerLerpSpeed = 6f;

        [Header("Boss HP Bar")]
        [SerializeField] private GameObject _bossHpBox;
        [SerializeField] private Image      _bossHpFill;
        [SerializeField] private float      _bossHpLerpSpeed = 6f;


        [Header("Panels")]
        [SerializeField] private GameOverPanel    _gameOverPanel;
        [SerializeField] private StageClearPanel  _stageClearPanel;
        [SerializeField] private LevelUpPanel     _levelUpPanel;
        [SerializeField] private BossWarningPanel _bossWarningPanel;
        [SerializeField] private StageStartPanel  _stageStartPanel;

        [Header("Pause")]
        [SerializeField] private Button     _pauseButton;
        [SerializeField] private PausePanel _pausePanel;

        [Header("References")]
        [SerializeField] private PlayerStats _playerStats;

        private readonly StringBuilder _sb = new StringBuilder(16);
        private int             _lastSeconds = -1;
        private FinalBossEnemy  _trackedBoss;
        private float           _powerFillTarget;
        private float           _bossHpTarget;

        private void Start()
        {
            if (_pauseButton != null)
                _pauseButton.onClick.AddListener(OnPauseClicked);

            if (InGameManager.Instance != null)
                InGameManager.Instance.OnGameOver += OnGameOverHandler;

            if (StageManager.Instance != null)
                StageManager.Instance.OnLoopClear += OnLoopClearHandler;

            if (UpgradeManager.Instance != null)
            {
                UpgradeManager.Instance.OnLevelUpReady   += OnLevelUpReadyHandler;
                UpgradeManager.Instance.OnUpgradeApplied += OnUpgradeAppliedHandler;
            }

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.OnScoreChanged += OnScoreChanged;
                OnScoreChanged(ScoreManager.Instance.Score);
            }

            if (CoinSystem.Instance != null)
            {
                CoinSystem.Instance.OnCoinChanged += OnCoinChanged;
                OnCoinChanged(CoinSystem.Instance.Total);
            }

         
            if (PowerSystem.Instance != null)
            {
                PowerSystem.Instance.OnPowerChanged += OnPowerChanged;
                PowerSystem.Instance.OnLevelUp      += OnLevelUp;
                OnPowerChanged(PowerSystem.Instance.CurrentPower, PowerSystem.Instance.PowerToNext);
                OnLevelUp(PowerSystem.Instance.CurrentLevel);
            }

            if (StageManager.Instance != null)
            {
                StageManager.Instance.OnStageStart          += OnStageStartHandler;
                StageManager.Instance.OnBossWarning         += OnBossWarningHandler;
                StageManager.Instance.OnFinalBossPhaseStart += OnBossPhaseStart;
                StageManager.Instance.OnFinalBossPhaseEnd   += OnBossPhaseEnd;
            }

            if (_bossWarningPanel != null)
                _bossWarningPanel.OnComplete += ShowBossHpBar;

            _bossHpBox?.SetActive(false);
        }

        private void Update()
        {
            if (InGameManager.Instance == null || !InGameManager.Instance.IsGameRunning) return;

            // Power bar lerp
            if (_powerFill != null)
                _powerFill.fillAmount = Mathf.Lerp(
                    _powerFill.fillAmount, _powerFillTarget,
                    Time.deltaTime * _powerLerpSpeed);

            // Boss HP bar lerp
            if (_bossHpFill != null && _bossHpBox != null && _bossHpBox.activeSelf)
                _bossHpFill.fillAmount = Mathf.Lerp(
                    _bossHpFill.fillAmount, _bossHpTarget,
                    Time.deltaTime * _bossHpLerpSpeed);

            // Timer (초 단위로만 갱신)
            int seconds = (int)InGameManager.Instance.ElapsedTime;
            if (seconds == _lastSeconds) return;
            _lastSeconds = seconds;

            if (_timerText == null) return;
            int m = seconds / 60;
            int s = seconds % 60;
            _sb.Clear();
            if (m < 10) _sb.Append('0');
            _sb.Append(m).Append(':');
            if (s < 10) _sb.Append('0');
            _sb.Append(s);
            _timerText.text = _sb.ToString();
        }

        // ── Boss HP Bar ──────────────────────────────────────────

        private void OnStageStartHandler(int stage)
        {
            _stageStartPanel?.Show(stage);
        }

        private void OnBossWarningHandler()
        {
            _powerFillBox?.SetActive(false);
            _bossWarningPanel?.Show();
        }

        private void OnBossPhaseStart()
        {
            // 보스 HP 추적은 즉시 시작, HP바 표시는 경고 패널 완료 후 ShowBossHpBar()에서
            _trackedBoss = FinalBossEnemy.ActiveBoss;
            if (_trackedBoss != null)
                _trackedBoss.OnHpChanged += UpdateBossHp;
        }

        private void ShowBossHpBar()
        {
            _powerFillBox?.SetActive(false);
            int hp    = _trackedBoss?.Hp    ?? 0;
            int maxHp = _trackedBoss?.MaxHp ?? 1;
            _bossHpTarget = maxHp > 0 ? (float)hp / maxHp : 0f;
            if (_bossHpFill != null) _bossHpFill.fillAmount = _bossHpTarget; // 첫 표시는 즉시
            _bossHpBox?.SetActive(true);
        }

        private void OnBossPhaseEnd()
        {
            if (_trackedBoss != null)
            {
                _trackedBoss.OnHpChanged -= UpdateBossHp;
                _trackedBoss = null;
            }
            _bossHpBox?.SetActive(false);
            _powerFillBox?.SetActive(true);
        }

        private void UpdateBossHp(int current, int max)
        {
            _bossHpTarget = max > 0 ? (float)current / max : 0f;
        }

        // ── Event Handlers ───────────────────────────────────────

        private void OnScoreChanged(int score)
        {
           //점수는 Top UI 표기X
        }

        private void OnCoinChanged(int total)
        {
            if (_coinText == null) return;
            _sb.Clear();
            _sb.Append(total);
            _coinText.text = _sb.ToString();
        }

        private void OnPowerChanged(int current, int toNext)
        {
            _powerFillTarget = toNext > 0 ? (float)current / toNext : 0f;
        }

        private void OnLevelUp(int level)
        {
            if (_levelText == null) return;
            _sb.Clear();
            _sb.Append("LV ").Append(level);
            _levelText.text = _sb.ToString();
        }

        // ── UI Panel Handlers ────────────────────────────────────

        private void OnGameOverHandler()
        {
            _stageClearPanel?.Hide(); // dismiss if showing at time of death

            int   stage   = StageManager.Instance != null ? StageManager.Instance.CurrentStage : 0;
            float elapsed = InGameManager.Instance != null ? InGameManager.Instance.ElapsedTime : 0f;
            int   coins   = CoinSystem.Instance   != null ? CoinSystem.Instance.Total           : 0;
            _gameOverPanel?.Show(stage, elapsed, coins);
        }

        private void OnLoopClearHandler(int stage, float elapsed, int coins)
        {
            _stageClearPanel?.Show(stage, elapsed, coins);
        }

        private void OnLevelUpReadyHandler(UpgradeData[] picks)
        {
            _levelUpPanel?.Show(picks);
        }

        private void OnUpgradeAppliedHandler()
        {
            _levelUpPanel?.Hide();
        }

        private void OnPauseClicked()
        {
            _pausePanel?.Show();
        }

        private void OnDestroy()
        {
            if (_pauseButton != null)
                _pauseButton.onClick.RemoveListener(OnPauseClicked);

            if (InGameManager.Instance != null)
                InGameManager.Instance.OnGameOver -= OnGameOverHandler;

            if (StageManager.Instance != null)
                StageManager.Instance.OnLoopClear -= OnLoopClearHandler;

            if (UpgradeManager.Instance != null)
            {
                UpgradeManager.Instance.OnLevelUpReady   -= OnLevelUpReadyHandler;
                UpgradeManager.Instance.OnUpgradeApplied -= OnUpgradeAppliedHandler;
            }

            if (ScoreManager.Instance != null)
                ScoreManager.Instance.OnScoreChanged -= OnScoreChanged;
            if (CoinSystem.Instance != null)
                CoinSystem.Instance.OnCoinChanged -= OnCoinChanged;

            if (PowerSystem.Instance != null)
            {
                PowerSystem.Instance.OnPowerChanged -= OnPowerChanged;
                PowerSystem.Instance.OnLevelUp      -= OnLevelUp;
            }

            if (StageManager.Instance != null)
            {
                StageManager.Instance.OnStageStart          -= OnStageStartHandler;
                StageManager.Instance.OnBossWarning         -= OnBossWarningHandler;
                StageManager.Instance.OnFinalBossPhaseStart -= OnBossPhaseStart;
                StageManager.Instance.OnFinalBossPhaseEnd   -= OnBossPhaseEnd;
            }

            if (_bossWarningPanel != null)
                _bossWarningPanel.OnComplete -= ShowBossHpBar;

            if (_trackedBoss != null)
                _trackedBoss.OnHpChanged -= UpdateBossHp;
        }
    }
}
