// Attach to: StageClearPanel GameObject (Canvas child, Game scene — initially inactive)
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ShooterGame.Core;
using ShooterGame.Economy;
using ShooterGame.Meta;

namespace ShooterGame.UI
{
    public class StageClearPanel : MonoBehaviour
    {
        [Header("Labels")]
        [SerializeField] private TMP_Text _stageText;
        [SerializeField] private TMP_Text _timeText;
        [SerializeField] private TMP_Text _coinText;

        [Header("Buttons")]
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _lobbyButton;

        private readonly StringBuilder _sb = new StringBuilder(32);

        private void Start()
        {
            _continueButton?.onClick.AddListener(HandleContinue);
            _lobbyButton?.onClick.AddListener(HandleLobby);
          
        }

        public void Show(int stage, float elapsedTime, int coins)
        {
            if (_stageText != null)
                _stageText.text = $"Stage {stage} Clear!";

            if (_timeText != null)
            {
                int total = (int)elapsedTime;
                int m = total / 60;
                int s = total % 60;
                _sb.Clear();
                _sb.Append("플레이 시간   ");
                if (m < 10) _sb.Append('0');
                _sb.Append(m).Append(':');
                if (s < 10) _sb.Append('0');
                _sb.Append(s);
                _timeText.text = _sb.ToString();
            }

            if (_coinText != null)
            {
                _sb.Clear();
                _sb.Append("획득 코인   ").Append(coins);
                _coinText.text = _sb.ToString();
            }

            Time.timeScale = 0f;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            Time.timeScale = 1f;
            gameObject.SetActive(false);
        }

        // ── Button Handlers ───────────────────────────────────────

        private void HandleContinue()
        {
            AudioManager.Instance?.PlaySFX(SfxType.ButtonClick);
            Time.timeScale = 1f;
            GameManager.Instance.LoadGameScene();
        }

        private void HandleLobby()
        {
            AudioManager.Instance?.PlaySFX(SfxType.ButtonClick);
            int earned = CoinSystem.Instance != null ? CoinSystem.Instance.Total : 0;
            SaveManager.Instance?.AddCoins(earned);
            Time.timeScale = 1f;
            GameManager.Instance.LoadLobbyScene();
        }

        private void OnDestroy()
        {
            _continueButton?.onClick.RemoveListener(HandleContinue);
            _lobbyButton?.onClick.RemoveListener(HandleLobby);
        }
    }
}
