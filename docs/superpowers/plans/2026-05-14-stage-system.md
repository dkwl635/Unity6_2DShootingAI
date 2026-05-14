# Stage System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Convert the infinite-loop pattern system into 4-minute stages with a mid-stage mini-boss at 2:00 (patterns continue) and a stage-ending final boss at 4:00 (all else stopped), advancing on boss defeat.

**Architecture:** New `StageManager` owns stage timing and boss spawning. `PatternManager` loses boss logic and gains `PausePatterns()`/`ResumePatterns()`. `EnemySpawner` gains `PauseSpawning()`/`ResumeSpawning()`. `DifficultyManager` loses its boss timer and gains `SetStage(int, int)` for per-stage scaling.

**Tech Stack:** Unity 2022.3 LTS · C# · TextMeshPro · MCP UnityMCP (validate_script, read_console, manage_scene, manage_scriptable_object, manage_prefabs)

**Spec:** `docs/superpowers/specs/2026-05-14-stage-system-design.md`

---

## File Map

| Action | Path |
|--------|------|
| Modify | `Assets/Scripts/Core/DifficultyManager.cs` |
| Modify | `Assets/Scripts/Core/PatternManager.cs` |
| Modify | `Assets/Scripts/Core/EnemySpawner.cs` |
| Create | `Assets/Scripts/Enemy/Patterns/FinalBossEnemy.cs` |
| Create | `Assets/Scripts/Core/Patterns/FinalBossPattern.cs` |
| Create | `Assets/Scripts/UI/StageClearUI.cs` |
| Create | `Assets/Scripts/Core/StageManager.cs` |
| Scene  | Game.unity — StageManager GO, StageClearPanel, prefabs, SOs |

---

## Task 1: DifficultyManager — remove boss timer, add SetStage

**Files:**
- Modify: `Assets/Scripts/Core/DifficultyManager.cs`

- [ ] **Step 1: Overwrite DifficultyManager.cs**

Remove `bossInterval`, `_bossTimer`, `OnMiniBossSpawn`, `ResetBossTimer()`, and all boss-timer Update logic.  
Add `_stageBaseMultiplier` field and `SetStage(int, int)` method.

```csharp
// Attach to: DifficultyManager GameObject (Game scene only — no DontDestroyOnLoad)
using UnityEngine;

namespace ShooterGame.Core
{
    public class DifficultyManager : MonoBehaviour
    {
        public static DifficultyManager Instance { get; private set; }

        [SerializeField] private float baseInterval = 2.0f;
        [SerializeField] private float minInterval  = 0.4f;
        [SerializeField] private float k            = 0.02f;

        [SerializeField] private float speedGain    = 0.01f;
        [SerializeField] private float maxSpeedMult = 3.0f;

        [SerializeField] private float hpGain       = 0.005f;
        [SerializeField] private float maxHpMult    = 5.0f;

        public float SpawnInterval        { get; private set; }
        public float EnemySpeedMultiplier { get; private set; }
        public float EnemyHpMultiplier    { get; private set; }

        private float _stageBaseMultiplier;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            SpawnInterval        = baseInterval;
            EnemySpeedMultiplier = 1f;
            EnemyHpMultiplier    = 1f;
        }

        private void Update()
        {
            if (InGameManager.Instance == null || !InGameManager.Instance.IsGameRunning) return;

            float t = InGameManager.Instance.ElapsedTime;

            SpawnInterval        = Mathf.Clamp(baseInterval * Mathf.Exp(-k * t), minInterval, baseInterval);
            EnemySpeedMultiplier = Mathf.Clamp(1f + _stageBaseMultiplier + speedGain * t, 1f, maxSpeedMult);
            EnemyHpMultiplier    = Mathf.Clamp(1f + _stageBaseMultiplier + hpGain * t,    1f, maxHpMult);
        }

        // Called by StageManager on each stage transition
        // Stage 2 → +0.3 base; each additional full loop → +0.2 on top
        public void SetStage(int stage, int loopCount)
        {
            _stageBaseMultiplier = (stage - 1) * 0.3f + loopCount * 0.2f;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
```

- [ ] **Step 2: Validate via MCP**

```
validate_script path="Assets/Scripts/Core/DifficultyManager.cs"
```
Expected: `"success": true`, no compile errors.

- [ ] **Step 3: Check Unity console**

```
read_console logType="Error"
```
Expected: no DifficultyManager errors.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Core/DifficultyManager.cs
git commit -m "refactor(difficulty): remove boss timer, add SetStage(stage, loopCount) API"
```

---

## Task 2: PatternManager — remove MiniBoss logic, add pause/resume

**Files:**
- Modify: `Assets/Scripts/Core/PatternManager.cs`

- [ ] **Step 1: Overwrite PatternManager.cs**

Remove: `miniBossPrefab` field, `miniBossConfig` field, `SpawnMiniBoss()` method, `OnMiniBossSpawn` subscription in Start/OnDestroy, `MiniBoss` case from `GetPrefab()`, `_debugSpawnMiniBossOnStart` field.  
Add: `PausePatterns()` and `ResumePatterns()` public methods.

```csharp
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

        private PatternBase                 _activePattern;
        private bool                        _running;
        private float                       _patternTimer;
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

        // ── Public API for StageManager ──────────────────────────

        public void PausePatterns()
        {
            _running = false;
            _activePattern?.ForceComplete();
        }

        public void ResumePatterns()
        {
            _running      = true;
            _patternTimer = 0f;
        }

        // ── Private ───────────────────────────────────────────────

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

        private void SpawnPattern(PatternConfig config)
        {
            PatternBase prefab = GetPrefab(config.Kind);
            if (prefab == null) return;
            SpawnPattern(config, prefab);
        }

        private void SpawnPattern(PatternConfig config, PatternBase prefab)
        {
            PatternBase pattern    = Instantiate(prefab, transform);
            _activePattern         = pattern;
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
                default:                       return null;
            }
        }

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

            if (Instance == this) Instance = null;
        }
    }
}
```

- [ ] **Step 2: Validate via MCP**

```
validate_script path="Assets/Scripts/Core/PatternManager.cs"
```
Expected: `"success": true`.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Core/PatternManager.cs
git commit -m "refactor(patterns): remove boss logic, add PausePatterns/ResumePatterns"
```

---

## Task 3: EnemySpawner — add PauseSpawning / ResumeSpawning

**Files:**
- Modify: `Assets/Scripts/Core/EnemySpawner.cs`

- [ ] **Step 1: Insert two public methods after `StopSpawning()`**

Add immediately after the closing brace of `StopSpawning()` (around line 58):

```csharp
        // Temporary pause during boss phase — does not unsubscribe lifecycle events
        public void PauseSpawning()
        {
            if (!_spawning) return;
            _spawning = false;
            if (_spawnCoroutine != null)
            {
                StopCoroutine(_spawnCoroutine);
                _spawnCoroutine = null;
            }
        }

        public void ResumeSpawning()
        {
            StartSpawning();
        }
```

- [ ] **Step 2: Validate via MCP**

```
validate_script path="Assets/Scripts/Core/EnemySpawner.cs"
```
Expected: `"success": true`.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Core/EnemySpawner.cs
git commit -m "feat(spawner): add PauseSpawning/ResumeSpawning for boss phase"
```

---

## Task 4: FinalBossEnemy — 2-phase boss AI

**Files:**
- Create: `Assets/Scripts/Enemy/Patterns/FinalBossEnemy.cs`

- [ ] **Step 1: Create FinalBossEnemy.cs**

`CurrentHp` and `CurrentSpeed` are `protected` fields on `EnemyBase`.  
`_maxHp` is captured lazily on the first `TakeDamage` call.  
Phase 2 triggers once when HP falls at or below 50% of max.

```csharp
// Attach to: FinalBoss Enemy prefab root
using System.Collections;
using UnityEngine;
using ShooterGame.Core;

namespace ShooterGame.Enemy
{
    public class FinalBossEnemy : EnemyBase
    {
        [Header("Movement")]
        [SerializeField] private float bossCenterY    = 5f;
        [SerializeField] private float sweepFrequency = 1.0f;
        [SerializeField] private float sweepAmplitude = 3f;

        [Header("Phase 1")]
        [SerializeField] private int   phase1BulletCount  = 5;
        [SerializeField] private float phase1FireInterval = 1.5f;

        [Header("Phase 2")]
        [SerializeField] private int   phase2BulletCount  = 7;
        [SerializeField] private float phase2FireInterval = 1.0f;
        [SerializeField] private float phase2SpeedBonus   = 1.5f;
        [SerializeField] private float phase2HpThreshold  = 0.5f;

        [Header("Shared")]
        [SerializeField] private float spreadAngle  = 20f;
        [SerializeField] private int   bulletDamage = 8;

        private bool           _reachedCenter;
        private float          _sweepTimer;
        private bool           _isPhase2;
        private int            _maxHp;
        private Coroutine      _shootCoroutine;
        private WaitForSeconds _phase1Wait;
        private WaitForSeconds _phase2Wait;
        private SpriteRenderer _sr;

        protected override void OnEnable()
        {
            base.OnEnable();
            _reachedCenter = false;
            _sweepTimer    = 0f;
            _isPhase2      = false;
            _maxHp         = 0;
            _phase1Wait    = new WaitForSeconds(phase1FireInterval);
            _phase2Wait    = new WaitForSeconds(phase2FireInterval);
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_sr != null)  _sr.color = Color.white;
        }

        private void OnDisable()
        {
            if (_shootCoroutine != null)
            {
                StopCoroutine(_shootCoroutine);
                _shootCoroutine = null;
            }
        }

        public override void TakeDamage(int dmg)
        {
            if (_maxHp == 0) _maxHp = CurrentHp;   // set on first hit
            base.TakeDamage(dmg);
            if (!_isPhase2 && CurrentHp > 0) CheckPhaseTransition();
        }

        protected override void Move()
        {
            if (!_reachedCenter)
            {
                Vector3 target     = new Vector3(transform.position.x, bossCenterY, 0f);
                transform.position = Vector3.MoveTowards(transform.position, target, CurrentSpeed * Time.deltaTime);

                if (Mathf.Abs(transform.position.y - bossCenterY) < 0.05f)
                {
                    _reachedCenter  = true;
                    _shootCoroutine = StartCoroutine(ShootLoop());
                }
            }
            else
            {
                _sweepTimer       += Time.deltaTime;
                float newX         = Mathf.Sin(_sweepTimer * sweepFrequency) * sweepAmplitude;
                transform.position = new Vector3(newX, bossCenterY, 0f);
            }
        }

        private void CheckPhaseTransition()
        {
            if (_maxHp == 0) return;
            if (CurrentHp <= Mathf.RoundToInt(_maxHp * phase2HpThreshold))
                EnterPhase2();
        }

        private void EnterPhase2()
        {
            _isPhase2     = true;
            CurrentSpeed *= phase2SpeedBonus;

            if (_sr != null) _sr.color = new Color(1f, 0.3f, 0.3f);

            if (_shootCoroutine != null) StopCoroutine(_shootCoroutine);
            _shootCoroutine = StartCoroutine(ShootLoop());

            AudioManager.Instance?.PlaySFX(SfxType.EnemyShoot);
        }

        private IEnumerator ShootLoop()
        {
            while (true)
            {
                yield return _isPhase2 ? _phase2Wait : _phase1Wait;
                FireSpread(_isPhase2 ? phase2BulletCount : phase1BulletCount);
            }
        }

        private void FireSpread(int count)
        {
            if (EnemyBulletPool.Instance == null) return;
            float step       = count > 1 ? spreadAngle * 2f / (count - 1) : 0f;
            float startAngle = -spreadAngle;
            for (int i = 0; i < count; i++)
                FireBullet(startAngle + step * i);
            AudioManager.Instance?.PlaySFX(SfxType.EnemyShoot);
        }

        private void FireBullet(float angleOffset)
        {
            EnemyBullet bullet = EnemyBulletPool.Instance.Get();
            if (bullet == null) return;
            bullet.transform.position = transform.position;
            bullet.transform.rotation = Quaternion.Euler(0f, 0f, 180f + angleOffset);
            bullet.Initialize(EnemyBulletPool.Instance, bulletDamage);
        }
    }
}
```

- [ ] **Step 2: Validate via MCP**

```
validate_script path="Assets/Scripts/Enemy/Patterns/FinalBossEnemy.cs"
```
Expected: `"success": true`.

- [ ] **Step 3: Check console**

```
read_console logType="Error"
```
Expected: no FinalBossEnemy errors.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Enemy/Patterns/FinalBossEnemy.cs
git commit -m "feat(enemy): add FinalBossEnemy with 2-phase attack (5→7 way spread, red tint)"
```

---

## Task 5: FinalBossPattern — PatternBase wrapper

**Files:**
- Create: `Assets/Scripts/Core/Patterns/FinalBossPattern.cs`

- [ ] **Step 1: Create FinalBossPattern.cs**

Mirrors `MiniBossPattern` exactly — just spawns the single boss enemy at the top of screen.  
PatternBase completes when the enemy dies (pool release triggers `Complete()`).

```csharp
// Attach to: FinalBossPattern prefab root
using UnityEngine;
using ShooterGame.Enemy;
using ShooterGame.Utils;

namespace ShooterGame.Core
{
    public class FinalBossPattern : PatternBase
    {
        [SerializeField] private float spawnOffsetY = 1f;

        protected override void ArrangeEnemies()
        {
            EnemyBase boss          = GetEnemy();
            boss.transform.position = new Vector3(0f, Constants.PLAY_HALF_HEIGHT + spawnOffsetY, 0f);
        }
    }
}
```

- [ ] **Step 2: Validate via MCP**

```
validate_script path="Assets/Scripts/Core/Patterns/FinalBossPattern.cs"
```
Expected: `"success": true`.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Core/Patterns/FinalBossPattern.cs
git commit -m "feat(patterns): add FinalBossPattern wrapper"
```

---

## Task 6: StageClearUI — popup panel

**Files:**
- Create: `Assets/Scripts/UI/StageClearUI.cs`

- [ ] **Step 1: Create StageClearUI.cs**

```csharp
// Attach to: StageClearPanel (child of HUD Canvas, initially inactive)
using UnityEngine;
using TMPro;

namespace ShooterGame.UI
{
    public class StageClearUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text stageLabel;

        public void ShowStageClear(int stageNum)
        {
            if (stageLabel != null)
                stageLabel.text = $"Stage {stageNum} Clear!";
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
```

- [ ] **Step 2: Validate via MCP**

```
validate_script path="Assets/Scripts/UI/StageClearUI.cs"
```
Expected: `"success": true`.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/UI/StageClearUI.cs
git commit -m "feat(ui): add StageClearUI popup"
```

---

## Task 7: StageManager — stage state machine

**Files:**
- Create: `Assets/Scripts/Core/StageManager.cs`

- [ ] **Step 1: Create StageManager.cs**

Key behaviors:
- Update ticks `_stageTimer` only when not in boss phase
- Mini-boss at `miniBossTime`: instantiates MiniBossPattern in a separate slot from PatternManager's `_activePattern`, so regular patterns keep running
- Final boss at `finalBossTime`: force-kills mini-boss if alive, pauses PatternManager + EnemySpawner, then spawns FinalBossPattern
- On final boss death: `StageTransition()` coroutine shows popup for 3 s, then advances stage and resumes all systems
- Game over: stops the transition coroutine, force-kills both boss slots, hides popup

```csharp
// Attach to: StageManager GameObject (Game scene only — no DontDestroyOnLoad)
using System;
using System.Collections;
using UnityEngine;
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

            if (finalBossPrefab == null || finalBossConfig == null) return;
            _activeFinalBoss                    = Instantiate(finalBossPrefab, transform);
            _activeFinalBoss.OnPatternComplete  += OnFinalBossDone;
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
        }

        private void OnGameStart()
        {
            StopAllCoroutines();
            _stageTimer      = 0f;
            _loopCount       = 0;
            CurrentStage     = 1;
            _miniBossSpawned = false;
            IsInBossPhase    = false;
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
```

- [ ] **Step 2: Validate via MCP**

```
validate_script path="Assets/Scripts/Core/StageManager.cs"
```
Expected: `"success": true`.

- [ ] **Step 3: Check console for all scripts together**

```
read_console logType="Error"
```
Expected: zero compile errors across all new/modified scripts.

- [ ] **Step 4: Commit all scripts**

```bash
git add Assets/Scripts/Core/StageManager.cs
git commit -m "feat(stage): add StageManager — stage timer, boss spawning, transition coroutine"
```

---

## Task 8: Unity Scene Setup

**Context:** All scripts compile. Now create assets and wire the scene in Unity Editor via MCP tools.

### Part A — ScriptableObjects

- [ ] **Step 1: Create EnemyData_FinalBoss asset**

Create via MCP `manage_scriptable_object` or manually in Editor:  
Path: `Assets/ScriptableObjects/Enemies/EnemyData_FinalBoss.asset`  
Type: `EnemyData`  
Values:
- baseHp: 300
- moveSpeed: 2
- scoreValue: 500
- contactDamage: 3
- coinDrop: 20
- powerDrop: 50

- [ ] **Step 2: Create PatternConfig_FinalBoss asset**

Path: `Assets/ScriptableObjects/Patterns/PatternConfig_FinalBoss.asset`  
Type: `PatternConfig`  
Values:
- patternType: `MiniBoss` (excluded from random selection — safe to reuse)
- enemyPrefab: *(assign after prefab is created in Step 4)*
- enemyData: `EnemyData_FinalBoss`
- enemyCount: 1
- unlockTime: 0
- patternDuration: 600 *(10-min safety timeout — boss should die before this)*

### Part B — Prefabs

- [ ] **Step 3: Create FinalBossEnemy prefab**

Duplicate `Assets/Prefabs/Enemies/MiniBossEnemy.prefab`.  
Rename to `FinalBossEnemy.prefab`.  
In the prefab:
- Remove `MiniBossEnemy` component.
- Add `FinalBossEnemy` component.
- All other components (SpriteRenderer, Collider2D, etc.) stay.

- [ ] **Step 4: Assign EnemyData in PatternConfig_FinalBoss**

Open `PatternConfig_FinalBoss.asset`.  
Set `enemyPrefab` → `FinalBossEnemy.prefab`.

- [ ] **Step 5: Create FinalBossPattern prefab**

Duplicate `Assets/Prefabs/Enemies/MiniBossPattern.prefab`.  
Rename to `FinalBossPattern.prefab`.  
In the prefab:
- Remove `MiniBossPattern` component.
- Add `FinalBossPattern` component.
- No other changes needed (PatternBase pool is set at runtime via StartPattern).

### Part C — Game Scene

- [ ] **Step 6: Add StageManager GameObject**

In Game scene, create empty GameObject named `StageManager`.  
Add `StageManager` component.  
Set Inspector fields:
- `miniBossTime`: 120
- `finalBossTime`: 240
- `stageClearSeconds`: 3
- `miniBossPrefab`: MiniBossPattern.prefab
- `finalBossPrefab`: FinalBossPattern.prefab
- `miniBossConfig`: *(the existing MiniBossConfig SO that was previously on PatternManager)*
- `finalBossConfig`: PatternConfig_FinalBoss.asset
- `totalStages`: 2

- [ ] **Step 7: Add StageClearPanel to HUD Canvas**

In Game scene HUD Canvas, add new UI Panel:
- Name: `StageClearPanel`
- Anchor: center
- Size: full-width strip or large centered box
- Add a child `TMP_Text` named `StageLabel` — font size 80, bold, centered, white
- Add `StageClearUI` component on StageClearPanel root
- Wire `stageLabel` → child StageLabel text object
- Set panel to **inactive** (will be shown by StageManager)

- [ ] **Step 8: Wire StageClearUI reference on StageManager**

Select StageManager GO.  
Set `stageClearUI` field → StageClearPanel's `StageClearUI` component.

- [ ] **Step 9: Clear stale references on PatternManager**

Select PatternManager GO in scene.  
The `miniBossConfig` and `miniBossPrefab` fields no longer exist (removed in Task 2) — Unity auto-clears them.  
Verify the PatternManager Inspector looks clean (no missing refs).

### Part D — Quick-Tune Test

- [ ] **Step 10: Temporarily lower test timings**

On StageManager GO in Inspector, set:
- `miniBossTime`: 10
- `finalBossTime`: 20

Enter Play mode.  
Verify:
- At 10 s: mini-boss appears, regular patterns still run
- Mini-boss dies: no disruption, patterns continue
- At 20 s: all patterns stop, only final boss on screen
- Final boss reaches 50% HP: sprite turns red, bullets increase 5→7
- Final boss dies: "Stage 1 Clear!" popup shows for 3 s
- After popup: Stage 2 starts, patterns resume, difficulty is slightly higher
- Repeat cycle verifies Stage 2 → "Stage 2 Clear!" → Stage 1 loop

Use MCP: `read_console logType="Error"` to check for runtime errors.

- [ ] **Step 11: Restore production timings**

Set `miniBossTime`: 120, `finalBossTime`: 240.

- [ ] **Step 12: Save scene and commit**

```bash
git add Assets/Scenes/Game.unity \
        Assets/Prefabs/Enemies/FinalBossEnemy.prefab \
        Assets/Prefabs/Enemies/FinalBossEnemy.prefab.meta \
        Assets/Prefabs/Enemies/FinalBossPattern.prefab \
        Assets/Prefabs/Enemies/FinalBossPattern.prefab.meta \
        Assets/ScriptableObjects/Enemies/EnemyData_FinalBoss.asset \
        Assets/ScriptableObjects/Enemies/EnemyData_FinalBoss.asset.meta \
        Assets/ScriptableObjects/Patterns/PatternConfig_FinalBoss.asset \
        Assets/ScriptableObjects/Patterns/PatternConfig_FinalBoss.asset.meta
git commit -m "feat(stage): wire stage system in Game scene — FinalBoss prefabs, SOs, StageManager GO, StageClearPanel"
```

---

## Verification Checklist (from spec)

After Task 8 is complete, confirm each item passes:

- [ ] At exactly 2:00 (120 s), mini-boss spawns without interrupting running pattern
- [ ] Mini-boss death has no stage effect; patterns continue uninterrupted
- [ ] At exactly 4:00 (240 s), all patterns and spawning stop; only final boss on screen
- [ ] Final boss enters phase 2 at 50% HP — sprite turns red, bullet count 5→7
- [ ] Final boss death shows "Stage N Clear!" popup for 3 seconds
- [ ] After popup: stage increments, difficulty increases (`_stageBaseMultiplier` bumped), patterns resume
- [ ] Player death at any point triggers normal Game Over (existing flow untouched)
- [ ] Game Over during stage transition: popup hides, boss cleaned up, no errors
- [ ] All event listeners unsubscribed in OnDestroy on all modified/new classes
