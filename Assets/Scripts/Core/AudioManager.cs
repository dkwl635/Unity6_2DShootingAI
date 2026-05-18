// Attach to: AudioManager GameObject (DontDestroyOnLoad)
using System.Collections;
using UnityEngine;
using ShooterGame.Utils;

namespace ShooterGame.Core
{
    public enum SfxType
    {
        PlayerShoot,
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
        private Coroutine     _fadeCoroutine;

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
            PreloadAllClips();
        }

        // ── BGM ─────────────────────────────────────────────────────

        public void PlayGameBGM()  => PlayBGM(_bgmGame);
        public void PlayLobbyBGM() => PlayBGM(_bgmLobby);

        // 보스별 고유 클립을 직접 전달 — 보스 프리팹이 자신의 클립을 소유
        public void PlayBgmClip(AudioClip clip) => PlayBGM(clip);

        public void PlaySfxClip(AudioClip clip)
        {
            if (clip == null) return;
            AudioSource src = NextSfxSource();
            src.clip   = clip;
            src.volume = _sfxVolume;
            src.Play();
        }

        // fadeDuration: 페이드아웃 + 페이드인 각각의 시간
        public void CrossfadeToGameBgm(float fadeDuration = 1.5f)
            => CrossfadeToBgm(_bgmGame, fadeDuration);

        public void CrossfadeToBgm(AudioClip clip, float fadeDuration = 1.5f)
        {
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(CrossfadeRoutine(clip, fadeDuration));
        }

        public void StopBGM()
        {
            CancelFade();
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
            CancelFade();
            _bgmSource.clip   = clip;
            _bgmSource.volume = _bgmVolume;
            _bgmSource.Play();
        }

        private void CancelFade()
        {
            if (_fadeCoroutine == null) return;
            StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = null;
            _bgmSource.volume = _bgmVolume;
        }

        private IEnumerator CrossfadeRoutine(AudioClip clip, float fadeDuration)
        {
            // 페이드 아웃
            float startVol = _bgmSource.volume;
            for (float t = 0f; t < fadeDuration; t += Time.deltaTime)
            {
                _bgmSource.volume = Mathf.Lerp(startVol, 0f, t / fadeDuration);
                yield return null;
            }
            _bgmSource.volume = 0f;
            _bgmSource.Stop();

            if (clip != null)
            {
                _bgmSource.clip = clip;
                _bgmSource.Play();
                // 페이드 인
                for (float t = 0f; t < fadeDuration; t += Time.deltaTime)
                {
                    _bgmSource.volume = Mathf.Lerp(0f, _bgmVolume, t / fadeDuration);
                    yield return null;
                }
                _bgmSource.volume = _bgmVolume;
            }

            _fadeCoroutine = null;
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
            SfxType.CoinPickup   => _sfxCoinPickup,
            SfxType.PowerPickup  => _sfxPowerPickup,
            SfxType.LevelUp      => _sfxLevelUp,
            SfxType.PlayerHit    => _sfxPlayerHit,
            SfxType.GameOver     => _sfxGameOver,
            SfxType.ButtonClick  => _sfxButtonClick,
            SfxType.EnemyShoot   => _sfxEnemyShoot,
            _                    => null
        };

        private void PreloadAllClips()
        {
            // Load all clips at app startup (AudioManager lives in DontDestroyOnLoad / Lobby)
            // so audio data is ready before the Game scene starts — eliminates the 1-second hitch
            // caused by on-demand streaming when PlayGameBGM() fires.
            TryLoad(_bgmGame);
            TryLoad(_bgmLobby);
            TryLoad(_sfxPlayerShoot);
            TryLoad(_sfxCoinPickup);
            TryLoad(_sfxPowerPickup);
            TryLoad(_sfxLevelUp);
            TryLoad(_sfxPlayerHit);
            TryLoad(_sfxGameOver);
            TryLoad(_sfxButtonClick);
            TryLoad(_sfxEnemyShoot);
        }

        private static void TryLoad(AudioClip clip)
        {
            if (clip != null && clip.loadState == AudioDataLoadState.Unloaded)
                clip.LoadAudioData();
        }

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
