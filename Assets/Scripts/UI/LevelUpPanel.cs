// Attach to: LevelUpPanel GameObject (Canvas child, default active, alpha=0)
using UnityEngine;
using ShooterGame.Upgrade;

namespace ShooterGame.UI
{
    public class LevelUpPanel : MonoBehaviour
    {
        [SerializeField] private LevelUpCard[]   _cards;     // 정확히 3개
        [SerializeField] private VirtualJoystick _joystick;

        private UpgradeData[] _currentPicks;

        public void Show(UpgradeData[] picks)
        {
            _currentPicks = picks;

            for (int i = 0; i < _cards.Length; i++)
            {
                int currentLevel = UpgradeManager.Instance != null
                    ? UpgradeManager.Instance.GetAppliedCount(picks[i].Type)
                    : 0;

                _cards[i].Setup(picks[i], currentLevel);

                int index = i;
                _cards[i].Button.onClick.RemoveAllListeners();
                _cards[i].Button.onClick.AddListener(() => OnCardSelected(index));
            }

            gameObject.SetActive(true);
            Time.timeScale = 0f;

            if (_joystick != null)
            {
                _joystick.ResetInput();
                _joystick.gameObject.SetActive(false);
            }
        }

        public void Hide()
        {
            Time.timeScale = 1f;
            gameObject.SetActive(false);

            if (_joystick != null)
                _joystick.gameObject.SetActive(true);
        }

        private void OnCardSelected(int index)
        {
            UpgradeManager.Instance?.ApplyUpgrade(_currentPicks[index]);
        }
    }
}
