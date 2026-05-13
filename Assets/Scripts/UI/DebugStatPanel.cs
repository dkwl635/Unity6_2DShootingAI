// Attach to: DebugStatPanel GameObject (Canvas child)
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using ShooterGame.Economy;
using ShooterGame.Player;

namespace ShooterGame.UI
{
    public class DebugStatPanel : MonoBehaviour
    {
        [SerializeField] private Text levelText;
        [SerializeField] private Text powerText;
        [SerializeField] private Text hpText;
        [SerializeField] private Text upgradeText;

        [SerializeField] private PlayerStats   playerStats;
        [SerializeField] private PlayerShooter playerShooter;

        [SerializeField] private float refreshInterval = 0.2f;

        private readonly StringBuilder _sb      = new StringBuilder(128);
        private          WaitForSeconds _pollWait;

        private void Awake()
        {
            _pollWait = new WaitForSeconds(refreshInterval);
        }

        private void OnEnable()  => StartCoroutine(PollRoutine());
        private void OnDisable() => StopAllCoroutines();

        private IEnumerator PollRoutine()
        {
            while (true)
            {
                Refresh();
                yield return _pollWait;
            }
        }

        private void Refresh()
        {
            // Level & POWER
            if (PowerSystem.Instance != null)
            {
                levelText.text = $"LV {PowerSystem.Instance.CurrentLevel}";
                powerText.text = $"POWER  {PowerSystem.Instance.CurrentPower} / {PowerSystem.Instance.PowerToNext}";
            }

            // HP
            if (playerStats != null)
                hpText.text = $"HP  {playerStats.CurrentHp} / {playerStats.MaxHp}";

            // Upgrade stats
            _sb.Clear();
            if (playerShooter != null)
            {
                _sb.Append("FireRate  ").Append(playerShooter.FireInterval.ToString("F2")).Append("s\n");
                _sb.Append("Damage    ").Append(playerShooter.BulletDamage).Append("\n");
            }
            if (MagnetEffect.Instance != null)
                _sb.Append("Magnet    ").Append(MagnetEffect.Instance.MagnetRadius.ToString("F1")).Append("m");

            upgradeText.text = _sb.ToString();
        }
    }
}
