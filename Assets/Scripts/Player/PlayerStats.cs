// Attach to: Player GameObject
using System;
using System.Collections;
using UnityEngine;
using ShooterGame.Core;

namespace ShooterGame.Player
{
    public class PlayerStats : MonoBehaviour
    {
        [SerializeField] private int   maxHp              = 3;
        [SerializeField] private float invincibleDuration = 1.5f;

        public event Action<int, int> OnHpChanged; // (currentHp, maxHp)

        public int  CurrentHp    { get; private set; }
        public int  MaxHp        { get; private set; }
        public bool IsInvincible { get; private set; }

        private WaitForSeconds _invincibleWait;

        private void Awake()
        {
            MaxHp     = maxHp;
            CurrentHp = maxHp;
            _invincibleWait = new WaitForSeconds(invincibleDuration);
        }

        private void Start()
        {
            if (InGameManager.Instance != null)
                InGameManager.Instance.OnGameStart += ResetHp;
        }

        public void TakeDamage(int dmg)
        {
            if (IsInvincible || dmg <= 0) return;

            CurrentHp = Mathf.Max(0, CurrentHp - dmg);
            OnHpChanged?.Invoke(CurrentHp, maxHp);

            if (CurrentHp <= 0)
            {
                InGameManager.Instance?.TriggerGameOver();
                return;
            }

            StartCoroutine(InvincibilityRoutine());
        }

        private IEnumerator InvincibilityRoutine()
        {
            IsInvincible = true;
            yield return _invincibleWait;
            IsInvincible = false;
        }

        public void IncreaseMaxHp(int amount)
        {
            maxHp     += amount;
            MaxHp      = maxHp;
            CurrentHp  = Mathf.Min(CurrentHp + amount, maxHp);
            OnHpChanged?.Invoke(CurrentHp, maxHp);
        }

        /// <summary>게임 시작 시 InGameManager가 한 번 호출. totalGain = gainPerLevel * level.</summary>
        public void ApplyPermanentHpBonus(int totalGain)
        {
            if (totalGain <= 0) return;
            IncreaseMaxHp(totalGain);
        }

        private void ResetHp()
        {
            MaxHp        = maxHp;
            CurrentHp    = maxHp;
            IsInvincible = false;
            OnHpChanged?.Invoke(CurrentHp, maxHp);
        }

        private void OnDestroy()
        {
            if (InGameManager.Instance != null)
                InGameManager.Instance.OnGameStart -= ResetHp;
        }
    }
}
