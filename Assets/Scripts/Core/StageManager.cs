// Attach to: StageManager GameObject (Game scene only — no DontDestroyOnLoad)
using System;
using System.Collections;
using UnityEngine;
using ShooterGame.Economy;
using ShooterGame.Enemy;

namespace ShooterGame.Core
{
    public class StageManager : MonoBehaviour
    {
        public static StageManager Instance { get; private set; }

        [Header("Timing")]
        [SerializeField] private float miniBossTime  = 10f;
        [SerializeField] private float finalBossTime = 20f;

        [Header("Loop")]
        [SerializeField] private int _stagesPerLoop = 3;

        [Header("Prefabs")]
        [SerializeField] private MiniBossPattern  miniBossPrefab;
        [SerializeField] private FinalBossPattern finalBossPrefab;

        [Header("Configs")]
        [SerializeField] private PatternConfig miniBossConfig;
        [SerializeField] private PatternConfig finalBossConfig;

        public int  CurrentStage  { get; private set; } = 1;
        public bool IsInBossPhase { get; private set; }

        public event Action<int>             OnStageStart;
        public event Action<int>             OnStageComplete;
        public event Action                  OnFinalBossPhaseStart;
        public event Action                  OnFinalBossPhaseEnd;
        public event Action                  OnBossWarning;        // 보스 등장 1초 전
        public event Action<int, float, int> OnLoopClear;          // stage, elapsedTime, coins

        [SerializeField] private float _bossWarningLeadTime    = 1f;  // 보스 등장 몇 초 전에 경고할지
        [SerializeField] private float _clearResultDelay       = 2f;  // 보스 처치 후 결과창 표시 딜레이
        [SerializeField] private float _stageTransitionDelay   = 2f;  // 중간 스테이지 전환 딜레이
        [SerializeField] private float _bgmRestoreDelay        = 5f;  // 보스 처치 후 원래 BGM 복귀 딜레이

        private float       _stageTimer;
        private bool        _miniBossSpawned;
        private bool        _bossWarningFired;
        private PatternBase _activeMiniBoss;
        private PatternBase _activeFinalBoss;
        private WaitForSeconds _clearResultWait;
        private WaitForSeconds _stageTransitionWait;
        private WaitForSeconds _bgmRestoreWait;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _clearResultWait     = new WaitForSeconds(_clearResultDelay);
            _stageTransitionWait = new WaitForSeconds(_stageTransitionDelay);
            _bgmRestoreWait      = new WaitForSeconds(_bgmRestoreDelay);
        }

        private void Start()
        {
            if (InGameManager.Instance != null)
            {
                InGameManager.Instance.OnPreGameStart += OnPreGameStart;
                InGameManager.Instance.OnGameStart    += OnGameStart;
                InGameManager.Instance.OnGameOver     += OnGameOver;
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

            if (!_bossWarningFired && _stageTimer >= finalBossTime - _bossWarningLeadTime)
            {
                _bossWarningFired = true;
                OnBossWarning?.Invoke();
            }

            if (_stageTimer >= finalBossTime)
                StartFinalBossPhase();
        }

        // ── Boss Spawning ──────────────────────────────────────────

        private void SpawnMiniBoss()
        {
            if (miniBossPrefab == null || miniBossConfig == null) return;
            _activeMiniBoss                   = Instantiate(miniBossPrefab, transform);
            _activeMiniBoss.OnPatternComplete += OnMiniBossDone;
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
            OnFinalBossPhaseStart?.Invoke();
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

            bool isLoopClear = (CurrentStage % _stagesPerLoop == 0);

            if (isLoopClear)
            {
                yield return _clearResultWait;
                float elapsed = InGameManager.Instance != null ? InGameManager.Instance.ElapsedTime : 0f;
                int   coins   = CoinSystem.Instance    != null ? CoinSystem.Instance.Total          : 0;
                OnLoopClear?.Invoke(CurrentStage, elapsed, coins);
                yield break;
            }
            else
            {
                CurrentStage++;
                StartCoroutine(RestoreBgmAfterDelay());
                OnStageStart?.Invoke(CurrentStage);
                yield return _stageTransitionWait;
            }

            OnFinalBossPhaseEnd?.Invoke();
            DifficultyManager.Instance?.SetStage(CurrentStage);

            _stageTimer       = 0f;
            _miniBossSpawned  = false;
            _bossWarningFired = false;
            IsInBossPhase     = false;

            PatternManager.Instance?.ResumePatterns();
            EnemySpawner.Instance?.ResumeSpawning();
        }

        private IEnumerator RestoreBgmAfterDelay()
        {
            yield return _bgmRestoreWait;
            AudioManager.Instance?.PlayGameBGM();
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

            if (IsInBossPhase) OnFinalBossPhaseEnd?.Invoke();
            IsInBossPhase = false;
        }

        private void OnPreGameStart()
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

            _stageTimer       = 0f;
            CurrentStage      = 1;
            _miniBossSpawned  = false;
            _bossWarningFired = false;
            IsInBossPhase     = false;
            DifficultyManager.Instance?.SetStage(1);
            OnStageStart?.Invoke(CurrentStage); // 딜레이 전에 패널 표시
        }

        private void OnGameStart() { }

        private void OnDestroy()
        {
            if (InGameManager.Instance != null)
            {
                InGameManager.Instance.OnPreGameStart -= OnPreGameStart;
                InGameManager.Instance.OnGameStart    -= OnGameStart;
                InGameManager.Instance.OnGameOver     -= OnGameOver;
            }
            if (Instance == this) Instance = null;
        }
    }
}
