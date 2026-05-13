// Attach to: LobbyController GameObject (Lobby scene)
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShooterGame.Core
{
    public class LobbyController : MonoBehaviour
    {
        public void OnPlayButtonClicked()
        {
            SceneManager.LoadScene("Game");
        }
    }
}
