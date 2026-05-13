// Attach to: HUDController GameObject (Canvas child, Game scene)
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ShooterGame.Core;
using ShooterGame.Economy;
using ShooterGame.Player;

namespace ShooterGame.UI
{
    public class HUDController : MonoBehaviour
    {
        [Header("Top Bar")]
        [SerializeField] private TMP_Text _timerText;
        [SerializeField] private TMP_Text _coinText;

        [Header("Power Bar")]
        [SerializeField] private Image _powerFill;
        [SerializeField] private TMP_Text _levelText;

        [Header("References")]
        [SerializeField] private PlayerStats _playerStats;

        private readonly StringBuilder _sb = new StringBuilder(16);
        private int _lastSeconds = -1;

        private void Start()
        {
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
        }

        private void Update()
        {
            if (InGameManager.Instance == null || !InGameManager.Instance.IsGameRunning) return;

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
            if (_powerFill != null)
                _powerFill.fillAmount = toNext > 0 ? (float)current / toNext : 0f;
        }

        private void OnLevelUp(int level)
        {
            if (_levelText == null) return;
            _sb.Clear();
            _sb.Append("LV ").Append(level);
            _levelText.text = _sb.ToString();
        }

        private void OnDestroy()
        {
            if (ScoreManager.Instance != null)
                ScoreManager.Instance.OnScoreChanged -= OnScoreChanged;
            if (CoinSystem.Instance != null)
                CoinSystem.Instance.OnCoinChanged -= OnCoinChanged;
           
            if (PowerSystem.Instance != null)
            {
                PowerSystem.Instance.OnPowerChanged -= OnPowerChanged;
                PowerSystem.Instance.OnLevelUp      -= OnLevelUp;
            }
        }
    }
}
