// Attach to: TitlePanel GameObject (Canvas child, Lobby scene)
using UnityEngine;
using UnityEngine.UI;

namespace ShooterGame.UI
{
    public class TitlePanel : MonoBehaviour
    {
        [SerializeField] private GameObject _lobbyPanel;
        [SerializeField] private Button     _anywhereButton;  // 화면 전체를 덮는 투명 버튼

        private void Start()
        {
            _anywhereButton.onClick.AddListener(OnTouched);
        }

        private void OnTouched()
        {
            _lobbyPanel?.SetActive(true);
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_anywhereButton != null)
                _anywhereButton.onClick.RemoveListener(OnTouched);
        }
    }
}
