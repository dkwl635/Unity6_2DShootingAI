// Attach to: AudioManager GameObject (DontDestroyOnLoad)
using UnityEngine;
using ShooterGame.Utils;

namespace ShooterGame.Core
{
    public enum SfxType
    {
        PlayerShoot,
        EnemyDeath,
        CoinPickup,
        PowerPickup,
        LevelUp,
        PlayerHit,
        GameOver,
        ButtonClick,
        EnemyShoot,
    }

    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("BGM")]
        [SerializeField] private AudioClip  _bgmGame;
        [SerializeField] private AudioClip  _bgmLobby;
        [SerializeField] [Range(0f, 1f)] private float _bgmVolume  = 0.4f;

        [Header("SFX Clips")]
        [SerializeField] private AudioClip _sfxPlayerShoot;
        [SerializeField] private AudioClip _sfxEnemyDeath;
        [SerializeField] private AudioClip _sfxCoinPickup;
        [SerializeField] private AudioClip _sfxPowerPickup;
        [SerializeField] private AudioClip _sfxLevelUp;
        [SerializeField] private AudioClip _sfxPlayerHit;
        [SerializeField] private AudioClip _sfxGameOver;
        [SerializeField] private AudioClip _sfxButtonClick;
        [SerializeField] private AudioClip _sfxEnemyShoot;

        [Header("SFX Pool")]
        [SerializeField] [Range(0f, 1f)] private float _sfxVolume   = 0.7f;
        [SerializeField] private int _sfxPoolSize = 8;

        private AudioSource   _bgmSource;
        private AudioSource[] _sfxPool;
        private int           _sfxIndex;

        public float BgmVolume => _bgmVolume;
        public float SfxVolume => _sfxVolume;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _bgmVolume = PlayerPrefs.GetFloat(Constants.PREF_BGM_VOLUME, _bgmVolume);
            _sfxVolume = PlayerPrefs.GetFloat(Constants.PREF_SFX_VOLUME, _sfxVolume);

            BuildBgmSource();
            BuildSfxPool();
        }

        // ── BGM ─────────────────────────────────────────────────────

        public void PlayGameBGM()  => PlayBGM(_bgmGame);
        public void PlayLobbyBGM() => PlayBGM(_bgmLobby);

        public void StopBGM()
        {
            if (_bgmSource.isPlaying) _bgmSource.Stop();
        }

        public void SetBgmVolume(float volume)
        {
            _bgmVolume = Mathf.Clamp01(volume);
            _bgmSource.volume = _bgmVolume;
            PlayerPrefs.SetFloat(Constants.PREF_BGM_VOLUME, _bgmVolume);
        }

        public void SetSfxVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(Constants.PREF_SFX_VOLUME, _sfxVolume);
        }

        // ── SFX ─────────────────────────────────────────────────────

        public void PlaySFX(SfxType type)
        {
            AudioClip clip = GetClip(type);
            if (clip == null) return;

            AudioSource src = NextSfxSource();
            src.clip   = clip;
            src.volume = _sfxVolume;
            src.Play();
        }

        // ── Private ──────────────────────────────────────────────────

        private void PlayBGM(AudioClip clip)
        {
            if (clip == null) return;
            if (_bgmSource.clip == clip && _bgmSource.isPlaying) return;

            _bgmSource.clip   = clip;
            _bgmSource.volume = _bgmVolume;
            _bgmSource.Play();
        }

        private AudioSource NextSfxSource()
        {
            AudioSource src = _sfxPool[_sfxIndex];
            _sfxIndex = (_sfxIndex + 1) % _sfxPool.Length;
            return src;
        }

        private AudioClip GetClip(SfxType type) => type switch
        {
            SfxType.PlayerShoot  => _sfxPlayerShoot,
            SfxType.EnemyDeath   => _sfxEnemyDeath,
            SfxType.CoinPickup   => _sfxCoinPickup,
            SfxType.PowerPickup  => _sfxPowerPickup,
            SfxType.LevelUp      => _sfxLevelUp,
            SfxType.PlayerHit    => _sfxPlayerHit,
            SfxType.GameOver     => _sfxGameOver,
            SfxType.ButtonClick  => _sfxButtonClick,
            SfxType.EnemyShoot   => _sfxEnemyShoot,
            _                    => null
        };

        private void BuildBgmSource()
        {
            _bgmSource        = gameObject.AddComponent<AudioSource>();
            _bgmSource.loop   = true;
            _bgmSource.volume = _bgmVolume;
            _bgmSource.playOnAwake = false;
        }

        private void BuildSfxPool()
        {
            _sfxPool = new AudioSource[_sfxPoolSize];
            for (int i = 0; i < _sfxPoolSize; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.loop        = false;
                src.playOnAwake = false;
                _sfxPool[i]     = src;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
