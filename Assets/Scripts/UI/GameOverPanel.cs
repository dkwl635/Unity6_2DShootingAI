// Attach to: GameOverPanel GameObject (Canvas child, default inactive)
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ShooterGame.Core;

namespace ShooterGame.UI
{
    public class GameOverPanel : MonoBehaviour
    {
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _lobbyButton;

        private CanvasGroup _group;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha          = 0f;
            _group.interactable   = false;
            _group.blocksRaycasts = false;
        }

        private void Start()
        {
            if (InGameManager.Instance != null)
                InGameManager.Instance.OnGameOver += Show;

            _restartButton?.onClick.AddListener(OnRestart);
            _lobbyButton?.onClick.AddListener(OnLobby);
        }

        private void Show()
        {
            _group.alpha          = 1f;
            _group.interactable   = true;
            _group.blocksRaycasts = true;
            Time.timeScale = 0f;
        }

        private void OnRestart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Game");
        }

        private void OnLobby()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Lobby");
        }

        private void OnDestroy()
        {
            if (InGameManager.Instance != null)
                InGameManager.Instance.OnGameOver -= Show;

            _restartButton?.onClick.RemoveListener(OnRestart);
            _lobbyButton?.onClick.RemoveListener(OnLobby);
        }
    }
}
