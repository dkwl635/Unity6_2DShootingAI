// Attach to: LobbySettingsPanel GameObject (Canvas child, Lobby scene — initially inactive)
using UnityEngine;
using UnityEngine.UI;
using ShooterGame.Core;

namespace ShooterGame.UI
{
    public class LobbySettingsPanel : MonoBehaviour
    {
        [SerializeField] private Slider _bgmSlider;
        [SerializeField] private Slider _sfxSlider;

        private void Start()
        {
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
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void OnBgmChanged(float value)
        {
            AudioManager.Instance?.SetBgmVolume(value);
        }

        private void OnSfxChanged(float value)
        {
            AudioManager.Instance?.SetSfxVolume(value);
        }

        private void OnDestroy()
        {
            _bgmSlider.onValueChanged.RemoveListener(OnBgmChanged);
            _sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
        }
    }
}
