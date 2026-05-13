// Attach to: 각 UpgradeSlot child GameObject
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using ShooterGame.Meta;

namespace ShooterGame.UI
{
    public class LobbyUpgradeSlot : MonoBehaviour
    {
        [SerializeField] private Text   _nameText;
        [SerializeField] private Text   _levelText;
        [SerializeField] private Text   _costText;
        [SerializeField] private Button _buyButton;
        [SerializeField] private Text   _buyButtonText;

        private LobbyUpgradeType       _type;
        private readonly StringBuilder _sb = new StringBuilder(16);

        public void Initialize(LobbyUpgradeType type)
        {
            _type = type;
            _buyButton.onClick.AddListener(OnBuyClicked);
        }

        public void Render(LobbyUpgradeData data, int currentLevel, int totalCoins)
        {
            _nameText.text  = data.DisplayName;
            _levelText.text = BuildLevelDots(currentLevel, data.MaxLevel);

            bool isMax = currentLevel >= data.MaxLevel;
            if (isMax)
            {
                _costText.text          = "";
                _buyButtonText.text     = "MAX";
                _buyButton.interactable = false;
            }
            else
            {
                int cost                = data.GetCostForLevel(currentLevel);
                _costText.text          = cost + " 코인";
                _buyButtonText.text     = "구매";
                _buyButton.interactable = totalCoins >= cost;
            }
        }

        private string BuildLevelDots(int current, int max)
        {
            _sb.Clear();
            for (int i = 0; i < max; i++)
                _sb.Append(i < current ? "●" : "○");
            return _sb.ToString();
        }

        private void OnBuyClicked()
        {
            LobbyUpgradeManager.Instance?.TryPurchase(_type);
        }

        private void OnDestroy()
        {
            if (_buyButton != null)
                _buyButton.onClick.RemoveListener(OnBuyClicked);
        }
    }
}
