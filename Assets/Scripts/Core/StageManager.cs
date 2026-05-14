// Attach to: StageManager GameObject (Game scene only — no DontDestroyOnLoad)
using System;
using System.Collections;
using UnityEngine;
using ShooterGame.Enemy;
using ShooterGame.UI;

namespace ShooterGame.Core
{
    public class StageManager : MonoBehaviour
    {
        public static StageManager Instance { get; private set; }

        [Header("Timing")]
        [SerializeField] private float miniBossTime      = 120f;
        [SerializeField] private float finalBossTime     = 240f;
        [SerializeField] private float stageClearSeconds = 3f;

        [Header("Prefabs")]
        [SerializeField] private MiniBossPattern  miniBossPrefab;
        [SerializeField] private FinalBossPattern finalBossPrefab;

        [Header("Configs")]
        [SerializeField] private PatternConfig miniBossConfig;
        [SerializeField] private PatternConfig finalBossConfig;

        [Header("UI")]
        [SerializeField] private StageClearUI stageClearUI;

        [Header("Stage")]
        [SerializeField] private int totalStages = 2;

        public int  CurrentStage  { get; private set; } = 1;
        public bool IsInBossPhase { get; private set; }

        public event Action<int> OnStageComplete;

        private float          _stageTimer;
        private int            _loopCount;
        private bool           _miniBossSpawned;
        private PatternBase    _activeMiniBoss;
        private PatternBase    _activeFinalBoss;
        private WaitForSeconds _stageClearWait;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance        = this;
            _stageClearWait = new WaitForSeconds(stageClearSeconds);
        }

        private void Start()
        {
            if (InGameManager.Instance != null)
            {
                InGameManager.Instance.OnGameStart += OnGameStart;
                InGameManager.Instance.OnGameOver  += OnGameOver;
                if (InGameManager.Instance.IsGameRunning) OnGameStart();
            }
            else
            {
                Debug.LogWarning("[StageManager] InGameManager not found.");
            }
        }

        private void Update()
        {
            if (InGameManager.Instance == null || !InGameManager.Instance.IsGameRunning) return;
            if (IsInBossPhase) return;

            _stageTimer += Time.deltaTime;

            if (!_miniBossSpawned && _stageTimer >= miniBossTime)
            {
                _miniBossSpawned = true;
                SpawnMiniBoss();
            }

            if (_stageTimer >= finalBossTime)
                StartFinalBossPhase();
        }

        // ── Boss Spawning ──────────────────────────────────────────

        private void SpawnMiniBoss()
        {
            if (miniBossPrefab == null || miniBossConfig == null) return;
            _activeMiniBoss                    = Instantiate(miniBossPrefab, transform);
            _activeMiniBoss.OnPatternComplete  += OnMiniBossDone;
            _activeMiniBoss.StartPattern(miniBossConfig);
        }

        private void StartFinalBossPhase()
        {
            if (finalBossPrefab == null || finalBossConfig == null)
            {
                Debug.LogError("[StageManager] finalBossPrefab or finalBossConfig is null — aborting boss phase.");
                return;
            }

            IsInBossPhase = true;

            if (_activeMiniBoss != null)
            {
                _activeMiniBoss.OnPatternComplete -= OnMiniBossDone;
                _activeMiniBoss.ForceComplete();
                Destroy(_activeMiniBoss.gameObject);
                _activeMiniBoss = null;
            }

            PatternManager.Instance?.PausePatterns();
            EnemySpawner.Instance?.PauseSpawning();

            _activeFinalBoss                   = Instantiate(finalBossPrefab, transform);
            _activeFinalBoss.OnPatternComplete += OnFinalBossDone;
            _activeFinalBoss.StartPattern(finalBossConfig);
        }

        // ── Pattern Callbacks ──────────────────────────────────────

        private void OnMiniBossDone()
        {
            if (_activeMiniBoss == null) return;
            _activeMiniBoss.OnPatternComplete -= OnMiniBossDone;
            Destroy(_activeMiniBoss.gameObject);
            _activeMiniBoss = null;
        }

        private void OnFinalBossDone()
        {
            if (_activeFinalBoss == null) return;
            _activeFinalBoss.OnPatternComplete -= OnFinalBossDone;
            Destroy(_activeFinalBoss.gameObject);
            _activeFinalBoss = null;
            StartCoroutine(StageTransition());
        }

        // ── Stage Transition ───────────────────────────────────────

        private IEnumerator StageTransition()
        {
            OnStageComplete?.Invoke(CurrentStage);
            stageClearUI?.ShowStageClear(CurrentStage);

            yield return _stageClearWait;

            stageClearUI?.Hide();

            // Advance stage; wrap back to 1 after totalStages, count loops
            CurrentStage = (CurrentStage % totalStages) + 1;
            if (CurrentStage == 1) _loopCount++;

            DifficultyManager.Instance?.SetStage(CurrentStage, _loopCount);

            _stageTimer      = 0f;
            _miniBossSpawned = false;
            IsInBossPhase    = false;

            PatternManager.Instance?.ResumePatterns();
            EnemySpawner.Instance?.ResumeSpawning();
        }

        // ── Game Over ─────────────────────────────────────────────

        private void OnGameOver()
        {
            StopAllCoroutines();

            if (_activeMiniBoss != null)
            {
                _activeMiniBoss.OnPatternComplete -= OnMiniBossDone;
                _activeMiniBoss.ForceComplete();
                Destroy(_activeMiniBoss.gameObject);
                _activeMiniBoss = null;
            }

            if (_activeFinalBoss != null)
            {
                _activeFinalBoss.OnPatternComplete -= OnFinalBossDone;
                _activeFinalBoss.ForceComplete();
                Destroy(_activeFinalBoss.gameObject);
                _activeFinalBoss = null;
            }

            stageClearUI?.Hide();
            IsInBossPhase = false;
        }

        private void OnGameStart()
        {
            StopAllCoroutines();

            if (_activeMiniBoss != null)
            {
                _activeMiniBoss.OnPatternComplete -= OnMiniBossDone;
                _activeMiniBoss.ForceComplete();
                Destroy(_activeMiniBoss.gameObject);
                _activeMiniBoss = null;
            }

            if (_activeFinalBoss != null)
            {
                _activeFinalBoss.OnPatternComplete -= OnFinalBossDone;
                _activeFinalBoss.ForceComplete();
                Destroy(_activeFinalBoss.gameObject);
                _activeFinalBoss = null;
            }

            stageClearUI?.Hide();
            _stageTimer      = 0f;
            _loopCount       = 0;
            CurrentStage     = 1;
            _miniBossSpawned = false;
            IsInBossPhase    = false;
            // Reset difficulty multiplier so new sessions always start from Stage 1 baseline
            DifficultyManager.Instance?.SetStage(1, 0);
        }

        private void OnDestroy()
        {
            if (InGameManager.Instance != null)
            {
                InGameManager.Instance.OnGameStart -= OnGameStart;
                InGameManager.Instance.OnGameOver  -= OnGameOver;
            }
            if (Instance == this) Instance = null;
        }
    }
}
