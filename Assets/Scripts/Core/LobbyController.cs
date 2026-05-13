// Attach to: LobbyController GameObject (Lobby scene)
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShooterGame.Core
{
    public class LobbyController : MonoBehaviour
    {
        private void Start()
        {
            AudioManager.Instance?.PlayLobbyBGM();
        }

        public void OnPlayButtonClicked()
        {
            AudioManager.Instance?.PlaySFX(SfxType.ButtonClick);
            GameManager.Instance.LoadGameScene();
        }
    }
}
