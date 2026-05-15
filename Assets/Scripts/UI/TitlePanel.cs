// Attach to: TitlePanel GameObject (Canvas child, Lobby scene)
using System;
using UnityEngine;
using UnityEngine.UI;

namespace ShooterGame.UI
{
    public class TitlePanel : MonoBehaviour
    {
        [SerializeField] private Button _anywhereButton;

        public event Action OnTitleTouched;

        private void Start()
        {
            _anywhereButton.onClick.AddListener(HandleButtonClick);
        }

        private void HandleButtonClick()
        {
            OnTitleTouched?.Invoke();
        }

        private void OnDestroy()
        {
            if (_anywhereButton != null)
                _anywhereButton.onClick.RemoveListener(HandleButtonClick);
        }
    }
}
