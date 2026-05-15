// Attach to: Player GameObject
using System;
using System.Collections;
using UnityEngine;
using ShooterGame.Core;

namespace ShooterGame.Player
{
    public class PlayerStats : MonoBehaviour
    {
        [SerializeField] private int   maxLives                 = 3;
        [SerializeField] private float respawnInvincibleDuration = 2f;

        public event Action<int, int> OnLivesChanged; // (currentLives, maxLives)
        public event Action           OnDied;          // fires whenever a life is lost

        public int  Lives        { get; private set; }
        public int  MaxLives     { get; private set; }
        public int  BaseMaxLives { get; private set; }
        public bool IsInvincible { get; private set; }

        private WaitForSeconds _respawnWait;

        private void Awake()
        {
            MaxLives     = maxLives;
            BaseMaxLives = maxLives;
            Lives        = maxLives;
            _respawnWait = new WaitForSeconds(respawnInvincibleDuration);
        }

        private void Start()
        {
            if (InGameManager.Instance != null)
                InGameManager.Instance.OnGameStart += ResetLives;
        }

        public void TakeDamage(int dmg)
        {
            if (IsInvincible || dmg <= 0) return;

            Lives = Mathf.Max(0, Lives - 1);
            OnLivesChanged?.Invoke(Lives, MaxLives);
            OnDied?.Invoke();
            CameraShake.Instance?.ShakeHit();
            AudioManager.Instance?.PlaySFX(SfxType.PlayerHit);

            if (Lives <= 0)
            {
                AudioManager.Instance?.PlaySFX(SfxType.GameOver);
                InGameManager.Instance?.TriggerGameOver();
                return;
            }

            StartCoroutine(RespawnInvincibilityRoutine());
        }

        private IEnumerator RespawnInvincibilityRoutine()
        {
            IsInvincible = true;
            yield return _respawnWait;
            IsInvincible = false;
        }

        public void IncreaseMaxLives(int amount)
        {
            maxLives  += amount;
            MaxLives   = maxLives;
            Lives      = Mathf.Min(Lives + amount, maxLives);
            OnLivesChanged?.Invoke(Lives, MaxLives);
        }

        public void ApplyPermanentLivesBonus(int totalGain)
        {
            if (totalGain <= 0) return;
            IncreaseMaxLives(totalGain);
            BaseMaxLives = MaxLives;
        }

        private void ResetLives()
        {
            MaxLives     = maxLives;
            Lives        = maxLives;
            IsInvincible = false;
            OnLivesChanged?.Invoke(Lives, MaxLives);
        }

        private void OnDestroy()
        {
            if (InGameManager.Instance != null)
                InGameManager.Instance.OnGameStart -= ResetLives;
        }
    }
}
