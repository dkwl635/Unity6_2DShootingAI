// Attach to: LevelUpPanel GameObject (Canvas child, default inactive)
using UnityEngine;
using UnityEngine.UI;
using ShooterGame.Upgrade;

namespace ShooterGame.UI
{
    public class LevelUpPanel : MonoBehaviour
    {
        [SerializeField] private Button[]        cardButtons;  // exactly 3
        [SerializeField] private Text[]          nameTexts;    // exactly 3, one per button
        [SerializeField] private VirtualJoystick _joystick;

        private UpgradeData[] _currentPicks;

        public void Show(UpgradeData[] picks)
        {
            _currentPicks = picks;
            for (int i = 0; i < cardButtons.Length; i++)
            {
                int index = i;  // closure capture — must be local variable
                nameTexts[i].text = picks[i].Name;
                cardButtons[i].onClick.RemoveAllListeners();
                cardButtons[i].onClick.AddListener(() => OnCardSelected(index));
            }
            gameObject.SetActive(true);
            Time.timeScale = 0f;

            // 터치 중이라도 조이스틱 상태 초기화 후 비활성화
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
