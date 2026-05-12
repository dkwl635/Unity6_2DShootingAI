// Attach to: LevelUpPanel GameObject (Canvas child, default inactive)
using UnityEngine;
using UnityEngine.UI;
using ShooterGame.Upgrade;

namespace ShooterGame.UI
{
    public class LevelUpPanel : MonoBehaviour
    {
        [SerializeField] private Button[] cardButtons;  // exactly 3
        [SerializeField] private Text[]   nameTexts;    // exactly 3, one per button

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
        }

        public void Hide()
        {
            Time.timeScale = 1f;
            gameObject.SetActive(false);
        }

        private void OnCardSelected(int index)
        {
            UpgradeManager.Instance?.ApplyUpgrade(_currentPicks[index]);
        }
    }
}
