// Attach to: Player GameObject (alongside PlayerSpriteController)
using System.Collections;
using UnityEngine;

namespace ShooterGame.Player
{
    public class PlayerHitEffect : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private PlayerStats    _playerStats;

        [SerializeField] private float      _flashDuration        = 0.08f;
        [SerializeField] private float      _blinkInterval        = 0.09f;

        [Header("Spawn FX")]
        [SerializeField] private GameObject _spawnFX; // Player 자식에 배치된 이펙트

        private static readonly Color _transparent = new Color(1f, 1f, 1f, 0f);

        private WaitForSeconds _flashWait;
        private WaitForSeconds _blinkWait;
        private Coroutine      _routine;

        private bool _firstSpawn = true;

        private void Awake()
        {
            _flashWait = new WaitForSeconds(_flashDuration);
            _blinkWait = new WaitForSeconds(_blinkInterval);
        }

        private void Start()
        {
            if (_playerStats != null)
                _playerStats.OnDied += HandleHit;

            // 게임 시작 최초 스폰
            _firstSpawn = true;
            PlaySpawnFX();
        }

        private void HandleHit()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(HitEffectRoutine());
        }

        private void PlaySpawnFX()
        {
            if (_spawnFX == null) return;
            if (_firstSpawn) { _firstSpawn = false; return; }
            _spawnFX.SetActive(false); // 재발동을 위해 일단 끄고
            _spawnFX.SetActive(true);
        }

        private IEnumerator HitEffectRoutine()
        {
            // 1. 스폰 이펙트 발동
            PlaySpawnFX();

            // 2. 흰색 플래시
            _renderer.color = Color.white;
            yield return _flashWait;

            // 3. 무적 시간 동안 깜빡임 (IsInvincible이 false가 되면 자동 종료)
            while (_playerStats.IsInvincible)
            {
                _renderer.color = _transparent;
                yield return _blinkWait;
                _renderer.color = Color.white;
                yield return _blinkWait;
            }

            // 4. 원래 색 복구
            _renderer.color = Color.white;
            _routine = null;
        }

        private void OnDestroy()
        {
            if (_playerStats != null)
                _playerStats.OnDied -= HandleHit;
        }
    }
}
