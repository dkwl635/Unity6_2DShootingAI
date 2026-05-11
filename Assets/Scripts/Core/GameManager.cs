// Attach to: GameManager GameObject (DontDestroyOnLoad)
// Responsibility: App-level control only — scene loading, app lifecycle, global init.
// Does NOT manage in-game session state. See InGameManager for that.

using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using ShooterGame.Utils;
using ShooterGame.Meta;

namespace ShooterGame.Core
{
    public class GameManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────
        public static GameManager Instance { get; private set; }

        // ── Events ───────────────────────────────────────────────
        public event Action OnGameSceneLoaded;
        public event Action OnLobbySceneLoaded;

        // ── Scene Names ──────────────────────────────────────────
        private const string SCENE_LOBBY = "Lobby";
        private const string SCENE_GAME  = "Game";

        private void Awake()
        {
            // Singleton duplicate guard
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Mobile performance: lock frame rate once at startup
            Application.targetFrameRate = Constants.TARGET_FRAME_RATE;
        }

        // ── Scene Transitions ────────────────────────────────────

        public void LoadGameScene()
        {
            SceneManager.LoadScene(SCENE_GAME);
            OnGameSceneLoaded?.Invoke();
        }

        public void LoadLobbyScene()
        {
            SceneManager.LoadScene(SCENE_LOBBY);
            OnLobbySceneLoaded?.Invoke();
        }

        // ── App Lifecycle ────────────────────────────────────────

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveManager.Instance?.ForceSave();
        }

        private void OnApplicationQuit()
        {
            SaveManager.Instance?.ForceSave();
        }

        private void OnDestroy()
        {
            // No external event subscriptions — nothing to unsubscribe
        }
    }
}
