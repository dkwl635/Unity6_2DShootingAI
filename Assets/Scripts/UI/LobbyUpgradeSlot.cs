// Attach to: 각 UpgradeSlot child GameObject
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using ShooterGame.Meta;
using System;
using Unity.VisualScripting;

namespace ShooterGame.UI
{
    public class LobbyUpgradeSlot : MonoBehaviour
    {
        [SerializeField] private Image  _iconImage;
        [SerializeField] private Button _selectButton;

        public event Action<LobbyUpgradeType> OnButtonClicked;

        private LobbyUpgradeType       _type;
        public void Initialize(LobbyUpgradeType type)
        {
            _type = type;
            _selectButton.onClick.AddListener(OnSelectClicked);
            OnButtonClicked = null;
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
            OnButtonClicked?.Invoke(_type);
       
        }

        private void OnDestroy()
        {
            if (_selectButton != null)
                _selectButton.onClick.RemoveListener(OnSelectClicked);

            OnButtonClicked = null;
        }
    }
}
