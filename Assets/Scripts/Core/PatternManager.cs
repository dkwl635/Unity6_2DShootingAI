// Attach to: PatternManager GameObject (Game scene only — no DontDestroyOnLoad)
using System.Collections.Generic;
using UnityEngine;
using ShooterGame.Enemy;

namespace ShooterGame.Core
{
    public class PatternManager : MonoBehaviour
    {
        public static PatternManager Instance { get; private set; }

        [SerializeField] private List<PatternConfig> patternConfigs;
        [SerializeField] private float               patternInterval = 20f;

        [SerializeField] private ScreenSweepPattern  screenSweepPrefab;
        [SerializeField] private CircleTrapPattern   circleTrapPrefab;
        [SerializeField] private MeteorShowerPattern meteorShowerPrefab;
        [SerializeField] private MiniBossPattern     miniBossPrefab;
        [SerializeField] private PatternConfig       miniBossConfig;

        private PatternBase            _activePattern;
        private bool                   _running;
        private float                  _patternTimer;
        private readonly List<PatternConfig> _eligible = new List<PatternConfig>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (InGameManager.Instance != null)
            {
                InGameManager.Instance.OnGameStart += StartLoop;
                InGameManager.Instance.OnGameOver  += StopLoop;

                if (InGameManager.Instance.IsGameRunning)
                    StartLoop();
            }
            else
            {
                Debug.LogWarning("[PatternManager] InGameManager not found in scene.");
            }

            if (DifficultyManager.Instance != null)
                DifficultyManager.Instance.OnMiniBossSpawn += SpawnMiniBoss;
            else
                Debug.LogWarning("[PatternManager] DifficultyManager not found in scene.");
        }

        private void Update()
        {
            if (!_running || _activePattern != null) return;

            _patternTimer += Time.deltaTime;
            if (_patternTimer >= patternInterval)
            {
                _patternTimer = 0f;
                TrySpawnPattern();
            }
        }

        private void StartLoop()
        {
            _running      = true;
            _patternTimer = 0f;
        }

        private void StopLoop()
        {
            _running = false;
            _activePattern?.ForceComplete();
        }

        private void TrySpawnPattern()
        {
            float elapsed = InGameManager.Instance?.ElapsedTime ?? 0f;
            _eligible.Clear();

            foreach (PatternConfig cfg in patternConfigs)
            {
                if (cfg != null && cfg.Kind != PatternType.MiniBoss && cfg.UnlockTime <= elapsed)
                    _eligible.Add(cfg);
            }

            if (_eligible.Count == 0) return;

            PatternConfig selected = _eligible[Random.Range(0, _eligible.Count)];
            SpawnPattern(selected);
        }

        private void SpawnMiniBoss()
        {
            if (miniBossConfig == null || miniBossPrefab == null) return;
            _activePattern?.ForceComplete();
            SpawnPattern(miniBossConfig, miniBossPrefab);
        }

        private void SpawnPattern(PatternConfig config)
        {
            PatternBase prefab = GetPrefab(config.Kind);
            if (prefab == null) return;
            SpawnPattern(config, prefab);
        }

        private void SpawnPattern(PatternConfig config, PatternBase prefab)
        {
            PatternBase pattern = Instantiate(prefab, transform);
            _activePattern      = pattern;
            pattern.OnPatternComplete += OnPatternDone;
            pattern.StartPattern(config);
        }

        private PatternBase GetPrefab(PatternType kind)
        {
            switch (kind)
            {
                case PatternType.ScreenSweep:  return screenSweepPrefab;
                case PatternType.CircleTrap:   return circleTrapPrefab;
                case PatternType.MeteorShower: return meteorShowerPrefab;
                case PatternType.MiniBoss:     return miniBossPrefab;
                default:                       return null;
            }
        }

        // Also called re-entrantly via StopLoop → ForceComplete → OnPatternComplete callback
        private void OnPatternDone()
        {
            if (_activePattern == null) return;
            _activePattern.OnPatternComplete -= OnPatternDone;
            Destroy(_activePattern.gameObject);
            _activePattern = null;
        }

        private void OnDestroy()
        {
            StopLoop();

            if (InGameManager.Instance != null)
            {
                InGameManager.Instance.OnGameStart -= StartLoop;
                InGameManager.Instance.OnGameOver  -= StopLoop;
            }

            if (DifficultyManager.Instance != null)
                DifficultyManager.Instance.OnMiniBossSpawn -= SpawnMiniBoss;

            if (Instance == this) Instance = null;
        }
    }
}
