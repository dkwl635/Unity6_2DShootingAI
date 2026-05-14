// Attach to: 각 UpgradeSlot child GameObject
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using ShooterGame.Meta;

namespace ShooterGame.UI
{
    public class LobbyUpgradeSlot : MonoBehaviour
    {
        [SerializeField] private Image  _iconImage;
        [SerializeField] private Button _selectButton;

        private LobbyUpgradeType       _type;
        public void Initialize(LobbyUpgradeType type)
        {
            _type = type;
            _selectButton.onClick.AddListener(OnSelectClicked);
        }

        public void Render(LobbyUpgradeData data, int currentLevel)
        {
            if (_iconImage != null)
            {
                _iconImage.sprite  = data.Icon;
                _iconImage.enabled = data.Icon != null;
            }
  
        }

    

        private void OnSelectClicked()
        {
            var popup = LobbyUpgradeInfoPopup.Instance;
            if (popup == null) return;
            if (popup.IsShowing && popup.CurrentType == _type)
                popup.Hide();
            else
                popup.Show(_type);
        }

        private void OnDestroy()
        {
            if (_selectButton != null)
                _selectButton.onClick.RemoveListener(OnSelectClicked);
        }
    }
}
