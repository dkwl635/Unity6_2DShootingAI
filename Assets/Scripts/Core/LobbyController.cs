// Attach to: LobbyController GameObject (Lobby scene)
using UnityEngine;
using DG.Tweening;
using ShooterGame.Meta;
using ShooterGame.UI;

namespace ShooterGame.Core
{
    public class LobbyController : MonoBehaviour
    {
        [SerializeField] private TitlePanel _titlePanel;
        [SerializeField] private GameObject _lobbyPanel;
        [SerializeField] private float      _zoomTargetSize = 5f;
        [SerializeField] private float      _zoomDuration   = 0.6f;
        [SerializeField] private Ease       _zoomEase       = Ease.InOutSine;

        private Camera _cam;

        private void Awake()
        {
            _cam = Camera.main;
        }

        private void Start()
        {
            AudioManager.Instance?.PlayLobbyBGM();

            bool showTitle = GameManager.Instance == null || !GameManager.Instance.HasVisitedLobby;

            if (showTitle)
            {
                if (_titlePanel != null)
                {
                    _titlePanel.OnTitleTouched += StartTitleTransition;
                    _titlePanel.gameObject.SetActive(true);
                }
                if (_lobbyPanel != null)
                    _lobbyPanel.gameObject.SetActive(false);
            }
            else
            {
                if (_titlePanel != null)
                    _titlePanel.gameObject.SetActive(false);

                if (_cam != null)
                    _cam.orthographicSize = _zoomTargetSize;
                if (_lobbyPanel != null)
                    _lobbyPanel.gameObject.SetActive(true);
            }
        }

        private void OnDestroy()
        {
            if (_titlePanel != null)
                _titlePanel.OnTitleTouched -= StartTitleTransition;
        }

        private void StartTitleTransition()
        {
            GameManager.Instance?.MarkLobbyVisited();
            _titlePanel?.gameObject.SetActive(false);

            DOTween.To(
                    () => _cam.orthographicSize,
                    x  => _cam.orthographicSize = x,
                    _zoomTargetSize,
                    _zoomDuration)
                .SetEase(_zoomEase)
                .OnComplete(() => _lobbyPanel?.SetActive(true));
        }

        public void OnPlayButtonClicked()
        {
            AudioManager.Instance?.PlaySFX(SfxType.ButtonClick);
            GameManager.Instance.LoadGameScene();
        }
    }
}
