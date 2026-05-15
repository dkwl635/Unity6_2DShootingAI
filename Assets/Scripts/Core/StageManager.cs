// Attach to: StageManager GameObject (Game scene only — no DontDestroyOnLoad)
using System;
using System.Collections;
using UnityEngine;
using ShooterGame.Economy;
using ShooterGame.Enemy;
using ShooterGame.UI;

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

        [Header("UI")]
        [SerializeField] private StageClearPanel _stageClearPanel;

        public int  CurrentStage  { get; private set; } = 1;
        public bool IsInBossPhase { get; private set; }

        public event Action<int> OnStageComplete;
        public event Action      OnFinalBossPhaseStart;
        public event Action      OnFinalBossPhaseEnd;

        private float       _stageTimer;
        private bool        _miniBossSpawned;
        private PatternBase _activeMiniBoss;
        private PatternBase _activeFinalBoss;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
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
                // Full loop clear — show panel (다시하기 = 씬 재로드, 나가기 = 로비)
                if (_stageClearPanel != null)
                {
                    float elapsed = InGameManager.Instance != null ? InGameManager.Instance.ElapsedTime : 0f;
                    int   coins   = CoinSystem.Instance    != null ? CoinSystem.Instance.Total          : 0;
                    _stageClearPanel.Show(CurrentStage, elapsed, coins);
                }

                // 패널에서 버튼을 누르면 씬 전환이 일어나므로 여기서 대기할 필요 없음
                yield break;
            }
            else
            {
                // Intermediate stage — advance immediately, no panel
                CurrentStage++;
            }

            OnFinalBossPhaseEnd?.Invoke();
            DifficultyManager.Instance?.SetStage(CurrentStage);

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

            if (_stageClearPanel != null)
            {
                _stageClearPanel.Hide();
            }
            if (IsInBossPhase) OnFinalBossPhaseEnd?.Invoke();
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

            if (_stageClearPanel != null)
            {               
                _stageClearPanel.Hide();
            }
            _stageTimer  = 0f;
            CurrentStage = 1;
            _miniBossSpawned = false;
            IsInBossPhase    = false;
            DifficultyManager.Instance?.SetStage(1);
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
