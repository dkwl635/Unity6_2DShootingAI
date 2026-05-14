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
        public event Action           OnHit;       // fires whenever damage is actually taken

        public int  CurrentHp    { get; private set; }
        public int  MaxHp        { get; private set; }
        public int  BaseMaxHp    { get; private set; }  // 로비 보너스 적용 후 기준값
        public bool IsInvincible { get; private set; }

        private WaitForSeconds _invincibleWait;

        private void Awake()
        {
            MaxHp     = maxHp;
            BaseMaxHp = maxHp;
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
            OnHit?.Invoke();
            CameraShake.Instance?.ShakeHit();
            AudioManager.Instance?.PlaySFX(SfxType.PlayerHit);

            if (CurrentHp <= 0)
            {
                AudioManager.Instance?.PlaySFX(SfxType.GameOver);
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
            BaseMaxHp = MaxHp;  // 로비 보너스 반영 후 기준 고정
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
