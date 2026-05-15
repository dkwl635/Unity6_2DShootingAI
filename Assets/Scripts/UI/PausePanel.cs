// Attach to: PausePanel GameObject (Canvas child, Game scene)
using UnityEngine;
using UnityEngine.UI;
using ShooterGame.Core;
using ShooterGame.Economy;
using ShooterGame.Meta;

namespace ShooterGame.UI
{
    public class PausePanel : MonoBehaviour
    {
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _lobbyButton;
        [SerializeField] private Slider _bgmSlider;
        [SerializeField] private Slider _sfxSlider;

        private void Start()
        {
            _resumeButton.onClick.AddListener(OnResume);
            _lobbyButton.onClick.AddListener(OnLobby);
            _bgmSlider.onValueChanged.AddListener(OnBgmChanged);
            _sfxSlider.onValueChanged.AddListener(OnSfxChanged);

           
        }

        public void Show()
        {
            if (AudioManager.Instance != null)
            {
                _bgmSlider.SetValueWithoutNotify(AudioManager.Instance.BgmVolume);
                _sfxSlider.SetValueWithoutNotify(AudioManager.Instance.SfxVolume);
            }

            Time.timeScale = 0f;
            gameObject.SetActive(true);
        }

        // ── Button Handlers ───────────────────────────────────────

        private void OnResume()
        {
            AudioManager.Instance?.PlaySFX(SfxType.ButtonClick);
            Hide();
        }

        private void OnLobby()
        {
            AudioManager.Instance?.PlaySFX(SfxType.ButtonClick);

            int earned = CoinSystem.Instance != null ? CoinSystem.Instance.Total : 0;
            SaveManager.Instance?.AddCoins(earned);

            Time.timeScale = 1f;
            GameManager.Instance.LoadLobbyScene();
        }

        // ── Slider Handlers ───────────────────────────────────────

        private void OnBgmChanged(float value)
        {
            AudioManager.Instance?.SetBgmVolume(value);
        }

        private void OnSfxChanged(float value)
        {
            AudioManager.Instance?.SetSfxVolume(value);
        }

        // ── Private ───────────────────────────────────────────────

        private void Hide()
        {
            Time.timeScale = 1f;
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _resumeButton.onClick.RemoveListener(OnResume);
            _lobbyButton.onClick.RemoveListener(OnLobby);
            _bgmSlider.onValueChanged.RemoveListener(OnBgmChanged);
            _sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
        }
    }
}
