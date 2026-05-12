// Attach to: GameOverPanel GameObject (Canvas child, default inactive)
using UnityEngine;
using ShooterGame.Core;

namespace ShooterGame.UI
{
    public class GameOverPanel : MonoBehaviour
    {
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
        }

        private void Show()
        {
            _group.alpha          = 1f;
            _group.interactable   = true;
            _group.blocksRaycasts = true;
            Time.timeScale = 0f;
        }

        private void OnDestroy()
        {
            if (InGameManager.Instance != null)
                InGameManager.Instance.OnGameOver -= Show;
        }
    }
}
