# Day 3 Enemy Pattern Variety — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add ScreenSweep, CircleTrap, MeteorShower, and MiniBoss enemy patterns via a PatternBase/PatternManager system that runs in parallel with the existing LinearDescent EnemySpawner.

**Architecture:** PatternBase is an abstract MonoBehaviour that owns an ObjectPool of enemies, positions them via `ArrangeEnemies()`, and optionally drives their movement via `UpdateMovement()`. PatternManager is a scene-scoped singleton that selects patterns on a 20-second timer and force-spawns the MiniBoss every 120 seconds via a DifficultyManager event.

**Tech Stack:** Unity 2022.3 LTS, C#, ObjectPool\<T\>, ScriptableObjects, C# Action events

---

## File Map

| Action | Path | Responsibility |
|--------|------|----------------|
| Modify | `Assets/Scripts/Utils/Constants.cs` | Add `BOSS_CENTER_Y` constant |
| Modify | `Assets/Scripts/Enemy/EnemyBase.cs` | `TakeDamage` virtual, `OnEnable` protected virtual, add `ForceReturnToPool()` |
| Create | `Assets/Scripts/Enemy/PatternConfig.cs` | `PatternType` enum + PatternConfig ScriptableObject |
| Create | `Assets/Scripts/Core/PatternBase.cs` | Abstract base: pool, `GetEnemy`, `ForceComplete`, `OnPatternComplete` event |
| Create | `Assets/Scripts/Core/Patterns/ScreenSweepPattern.cs` | Horizontal sweep |
| Create | `Assets/Scripts/Core/Patterns/CircleTrapPattern.cs` | Circle → center |
| Create | `Assets/Scripts/Enemy/Patterns/MeteorEnemy.cs` | Indestructible, falls down |
| Create | `Assets/Scripts/Core/Patterns/MeteorShowerPattern.cs` | Staggered meteor spawn |
| Create | `Assets/Scripts/Enemy/Patterns/MiniBossEnemy.cs` | Two-phase boss movement |
| Create | `Assets/Scripts/Core/Patterns/MiniBossPattern.cs` | Spawns boss at top-center |
| Modify | `Assets/Scripts/Core/DifficultyManager.cs` | Add `OnMiniBossSpawn` event + 120s boss timer |
| Create | `Assets/Scripts/Core/PatternManager.cs` | Scene singleton, 20s pattern loop, boss override |

---

## Task 1: Constants + EnemyBase modifications

**Files:**
- Modify: `Assets/Scripts/Utils/Constants.cs`
- Modify: `Assets/Scripts/Enemy/EnemyBase.cs`

These two small changes unlock everything else: the boss Y-target constant and the three EnemyBase extensibility hooks (virtual TakeDamage for MeteorEnemy, protected virtual OnEnable for MiniBossEnemy state reset, ForceReturnToPool for PatternBase's ForceComplete).

- [ ] **Step 1: Add BOSS_CENTER_Y to Constants.cs**

Open `Assets/Scripts/Utils/Constants.cs`. Add one line inside the `Gameplay` section:

```csharp
// ── Gameplay ─────────────────────────────────────────────
public const int   TARGET_FRAME_RATE   = 60;
public const float SCREEN_BOUND_MARGIN = 0.5f;
public const float BOSS_CENTER_Y       = 1f;   // ← add this
```

- [ ] **Step 2: Make TakeDamage virtual in EnemyBase.cs**

Open `Assets/Scripts/Enemy/EnemyBase.cs`. Change the signature of `TakeDamage`:

```csharp
// Before:
public void TakeDamage(int dmg)

// After:
public virtual void TakeDamage(int dmg)
```

- [ ] **Step 3: Make OnEnable protected virtual in EnemyBase.cs**

In the same file, change:

```csharp
// Before:
private void OnEnable()
{
    _released = false;
}

// After:
protected virtual void OnEnable()
{
    _released = false;
}
```

- [ ] **Step 4: Add ForceReturnToPool() to EnemyBase.cs**

Add this public method immediately after the existing `TakeDamage` method:

```csharp
public void ForceReturnToPool()
{
    if (_released) return;
    _released = true;
    // Caller (PatternBase) handles pool.Release() directly
}
```

- [ ] **Step 5: Verify in Unity Editor**

Open Unity. Confirm zero compile errors in the Console. No Play Mode test needed — this is API surface only.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Utils/Constants.cs Assets/Scripts/Enemy/EnemyBase.cs
git commit -m "feat(day3): extend EnemyBase API and add BOSS_CENTER_Y constant"
```

---

## Task 2: PatternConfig ScriptableObject

**Files:**
- Create: `Assets/Scripts/Enemy/PatternConfig.cs`

PatternConfig is the data contract every pattern reads at runtime. The `Kind` property uses a deliberately different name from the `PatternType` type to avoid C# ambiguity.

- [ ] **Step 1: Create the file**

Create `Assets/Scripts/Enemy/PatternConfig.cs` with the full content below:

```csharp
// Attach to: (ScriptableObject — no GameObject attachment needed)
using UnityEngine;

namespace ShooterGame.Enemy
{
    public enum PatternType { ScreenSweep, CircleTrap, MeteorShower, MiniBoss }

    [CreateAssetMenu(fileName = "PatternConfig", menuName = "ShooterGame/Pattern Config")]
    public class PatternConfig : ScriptableObject
    {
        [SerializeField] private PatternType patternType    = PatternType.ScreenSweep;
        [SerializeField] private EnemyBase   enemyPrefab;
        [SerializeField] private EnemyData   enemyData;
        [SerializeField] private int         enemyCount      = 5;
        [SerializeField] private float       unlockTime      = 0f;
        [SerializeField] private float       patternDuration = 8f;

        public PatternType Kind            => patternType;
        public EnemyBase   EnemyPrefab     => enemyPrefab;
        public EnemyData   EnemyData       => enemyData;
        public int         EnemyCount      => enemyCount;
        public float       UnlockTime      => unlockTime;
        public float       PatternDuration => patternDuration;
    }
}
```

- [ ] **Step 2: Verify in Unity Editor**

Zero compile errors. Confirm `Assets > Create > ShooterGame > Pattern Config` appears in the right-click menu (you don't need to create the asset yet — that's Task 10).

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Enemy/PatternConfig.cs
git commit -m "feat(day3): add PatternConfig ScriptableObject and PatternType enum"
```

---

## Task 3: PatternBase (abstract)

**Files:**
- Create: `Assets/Scripts/Core/PatternBase.cs`

PatternBase is the engine of every pattern. Key design points:
- `StartPattern()` is called by PatternManager after Instantiate — creates the pool, positions enemies, starts the duration clock.
- `GetEnemy()` is the only way subclasses spawn enemies — it handles pool.Get(), Initialize(), and count tracking.
- `ReleaseEnemyManual()` lets subclasses manually return an individual enemy (e.g., off-screen X check in ScreenSweep, center-reach check in CircleTrap).
- `ForceComplete()` clears everything and fires `OnPatternComplete`. It calls `ForceReturnToPool()` on each live enemy before calling `pool.Release()` so EnemyBase's `_released` flag is set — preventing any late bullet or trigger callbacks from double-releasing.
- `_completed` flag ensures `OnPatternComplete` fires exactly once even if both the natural path and ForceComplete race.

- [ ] **Step 1: Create the file**

Create `Assets/Scripts/Core/PatternBase.cs`:

```csharp
// Attach to: Pattern prefab root (instantiated at runtime by PatternManager)
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ShooterGame.Enemy;
using ShooterGame.Utils;

namespace ShooterGame.Core
{
    public abstract class PatternBase : MonoBehaviour
    {
        public event Action OnPatternComplete;

        protected PatternConfig Config { get; private set; }

        private ObjectPool<EnemyBase>      _pool;
        protected readonly List<EnemyBase> ActiveEnemies = new List<EnemyBase>();
        private bool           _completed;
        private Coroutine      _durationCoroutine;
        private WaitForSeconds _durationWait;

        public void StartPattern(PatternConfig config)
        {
            Config        = config;
            _pool         = new ObjectPool<EnemyBase>(config.EnemyPrefab, config.EnemyCount, transform);
            _durationWait = new WaitForSeconds(config.PatternDuration);

            ArrangeEnemies();
            _durationCoroutine = StartCoroutine(DurationTimer());
        }

        protected abstract void ArrangeEnemies();
        protected virtual void UpdateMovement() { }

        private void Update()
        {
            if (Config == null) return;
            UpdateMovement();
        }

        protected EnemyBase GetEnemy()
        {
            float hpMult    = DifficultyManager.Instance?.EnemyHpMultiplier    ?? 1f;
            float speedMult = DifficultyManager.Instance?.EnemySpeedMultiplier ?? 1f;

            EnemyBase enemy = _pool.Get();
            enemy.Initialize(Config.EnemyData, hpMult, speedMult, OnEnemyReleased);
            ActiveEnemies.Add(enemy);
            return enemy;
        }

        // Called by enemy's own death/offscreen path via _releaseCallback
        private void OnEnemyReleased(EnemyBase enemy)
        {
            if (!ActiveEnemies.Contains(enemy)) return;
            ActiveEnemies.Remove(enemy);
            _pool.Release(enemy);
            if (ActiveEnemies.Count == 0) Complete();
        }

        // Called by subclasses for manual bounds checks (ScreenSweep X, CircleTrap center)
        protected void ReleaseEnemyManual(EnemyBase enemy)
        {
            if (!ActiveEnemies.Contains(enemy)) return;
            enemy.ForceReturnToPool();
            ActiveEnemies.Remove(enemy);
            _pool.Release(enemy);
            if (ActiveEnemies.Count == 0) Complete();
        }

        // Called by PatternManager when boss overrides or game ends
        public void ForceComplete()
        {
            StopAllCoroutines();
            List<EnemyBase> snapshot = new List<EnemyBase>(ActiveEnemies);
            ActiveEnemies.Clear();
            foreach (EnemyBase e in snapshot)
            {
                e.ForceReturnToPool();
                _pool.Release(e);
            }
            Complete();
        }

        private IEnumerator DurationTimer()
        {
            yield return _durationWait;
            if (!_completed) ForceComplete();
        }

        private void Complete()
        {
            if (_completed) return;
            _completed = true;
            if (_durationCoroutine != null)
            {
                StopCoroutine(_durationCoroutine);
                _durationCoroutine = null;
            }
            OnPatternComplete?.Invoke();
        }

        private void OnDestroy()
        {
            _pool?.ReleaseAll(ActiveEnemies);
        }
    }
}
```

- [ ] **Step 2: Create the Patterns subfolder**

In Unity's Project window: right-click `Assets/Scripts/Core` → Create → Folder → name it `Patterns`.

- [ ] **Step 3: Verify in Unity Editor**

Zero compile errors. PatternBase is abstract so no MonoBehaviour shows in the Add Component menu — that's expected.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Core/PatternBase.cs
git commit -m "feat(day3): add PatternBase abstract class with pool and event system"
```

---

## Task 4: ScreenSweepPattern

**Files:**
- Create: `Assets/Scripts/Core/Patterns/ScreenSweepPattern.cs`

Enemies spawn in a horizontal line off-screen (left or right side), at a random visible Y, and translate across. `UpdateMovement()` also checks the X off-screen bound and calls `ReleaseEnemyManual()` per enemy. Enemies can be shot normally — bullet triggers TakeDamage → Die → ReturnToPool → `OnEnemyReleased` callback → pool.Release.

- [ ] **Step 1: Create the file**

Create `Assets/Scripts/Core/Patterns/ScreenSweepPattern.cs`:

```csharp
// Attach to: ScreenSweep Pattern prefab root
using UnityEngine;
using ShooterGame.Utils;

namespace ShooterGame.Core
{
    public class ScreenSweepPattern : PatternBase
    {
        [SerializeField] private float sweepSpeed    = 4f;
        [SerializeField] private float enemySpacing  = 1.5f;

        private Vector3 _sweepDir;

        protected override void ArrangeEnemies()
        {
            bool leftToRight = Random.value > 0.5f;
            _sweepDir        = leftToRight ? Vector3.right : Vector3.left;

            float startX = leftToRight
                ? -(Constants.PLAY_HALF_WIDTH + enemySpacing * Config.EnemyCount)
                :   Constants.PLAY_HALF_WIDTH + enemySpacing * Config.EnemyCount;

            float y = Random.Range(-Constants.PLAY_HALF_HEIGHT * 0.5f, Constants.PLAY_HALF_HEIGHT * 0.5f);

            for (int i = 0; i < Config.EnemyCount; i++)
            {
                EnemyBase enemy = GetEnemy();
                float x         = leftToRight
                    ? startX + i * enemySpacing
                    : startX - i * enemySpacing;
                enemy.transform.position = new Vector3(x, y, 0f);
            }
        }

        protected override void UpdateMovement()
        {
            float offscreenX = Constants.PLAY_HALF_WIDTH + 2f;
            for (int i = ActiveEnemies.Count - 1; i >= 0; i--)
            {
                EnemyBase enemy = ActiveEnemies[i];
                enemy.transform.Translate(_sweepDir * sweepSpeed * Time.deltaTime);
                if (Mathf.Abs(enemy.transform.position.x) > offscreenX)
                    ReleaseEnemyManual(enemy);
            }
        }
    }
}
```

- [ ] **Step 2: Verify in Unity Editor**

Zero compile errors.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Core/Patterns/ScreenSweepPattern.cs
git commit -m "feat(day3): add ScreenSweepPattern"
```

---

## Task 5: CircleTrapPattern

**Files:**
- Create: `Assets/Scripts/Core/Patterns/CircleTrapPattern.cs`

Enemies are placed evenly on a circle of radius 4f centered on the screen. Each frame they move toward `Vector3.zero`. When close enough to the center they're released without score (manual release, not Die). They can also be shot normally.

- [ ] **Step 1: Create the file**

Create `Assets/Scripts/Core/Patterns/CircleTrapPattern.cs`:

```csharp
// Attach to: CircleTrap Pattern prefab root
using UnityEngine;

namespace ShooterGame.Core
{
    public class CircleTrapPattern : PatternBase
    {
        [SerializeField] private float spawnRadius   = 4f;
        [SerializeField] private float closeSpeed    = 2f;
        [SerializeField] private float releaseRadius = 0.5f;

        protected override void ArrangeEnemies()
        {
            float step = 360f / Config.EnemyCount;
            for (int i = 0; i < Config.EnemyCount; i++)
            {
                float angle     = i * step * Mathf.Deg2Rad;
                float x         = Mathf.Cos(angle) * spawnRadius;
                float y         = Mathf.Sin(angle) * spawnRadius;
                EnemyBase enemy = GetEnemy();
                enemy.transform.position = new Vector3(x, y, 0f);
            }
        }

        protected override void UpdateMovement()
        {
            for (int i = ActiveEnemies.Count - 1; i >= 0; i--)
            {
                EnemyBase enemy = ActiveEnemies[i];
                enemy.transform.position = Vector3.MoveTowards(
                    enemy.transform.position, Vector3.zero, closeSpeed * Time.deltaTime);

                if (enemy.transform.position.magnitude < releaseRadius)
                    ReleaseEnemyManual(enemy);
            }
        }
    }
}
```

- [ ] **Step 2: Verify in Unity Editor**

Zero compile errors.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Core/Patterns/CircleTrapPattern.cs
git commit -m "feat(day3): add CircleTrapPattern"
```

---

## Task 6: MeteorEnemy + MeteorShowerPattern

**Files:**
- Create: `Assets/Scripts/Enemy/Patterns/MeteorEnemy.cs`
- Create: `Assets/Scripts/Core/Patterns/MeteorShowerPattern.cs`

MeteorEnemy overrides `TakeDamage` as a no-op (indestructible). It moves itself via `override Move()` — PatternBase's `UpdateMovement()` is not overridden. Meteors return to pool via EnemyBase's own off-screen Y check.

MeteorShowerPattern staggers spawns with a cached `WaitForSeconds` — this must be cached in `Awake()` (not inside the coroutine) per CLAUDE.md rule.

- [ ] **Step 1: Create Enemy/Patterns folder in Unity Project window**

Right-click `Assets/Scripts/Enemy` → Create → Folder → name it `Patterns`.

- [ ] **Step 2: Create MeteorEnemy.cs**

Create `Assets/Scripts/Enemy/Patterns/MeteorEnemy.cs`:

```csharp
// Attach to: Meteor Enemy prefab root
using UnityEngine;

namespace ShooterGame.Enemy
{
    public class MeteorEnemy : EnemyBase
    {
        protected override void Move()
        {
            transform.Translate(Vector3.down * CurrentSpeed * Time.deltaTime);
        }

        public override void TakeDamage(int dmg) { }
    }
}
```

- [ ] **Step 3: Create MeteorShowerPattern.cs**

Create `Assets/Scripts/Core/Patterns/MeteorShowerPattern.cs`:

```csharp
// Attach to: MeteorShower Pattern prefab root
using System.Collections;
using UnityEngine;
using ShooterGame.Utils;

namespace ShooterGame.Core
{
    public class MeteorShowerPattern : PatternBase
    {
        [SerializeField] private float spawnOffsetY    = 1f;
        [SerializeField] private float staggerInterval = 0.5f;

        private WaitForSeconds _staggerWait;

        private void Awake()
        {
            _staggerWait = new WaitForSeconds(staggerInterval);
        }

        protected override void ArrangeEnemies()
        {
            StartCoroutine(StaggeredSpawn());
        }

        private IEnumerator StaggeredSpawn()
        {
            for (int i = 0; i < Config.EnemyCount; i++)
            {
                float x = Random.Range(
                    -Constants.PLAY_HALF_WIDTH + Constants.SCREEN_BOUND_MARGIN,
                     Constants.PLAY_HALF_WIDTH - Constants.SCREEN_BOUND_MARGIN);
                float y          = Constants.PLAY_HALF_HEIGHT + spawnOffsetY;
                EnemyBase meteor = GetEnemy();
                meteor.transform.position = new Vector3(x, y, 0f);
                yield return _staggerWait;
            }
        }
    }
}
```

- [ ] **Step 4: Verify in Unity Editor**

Zero compile errors.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Enemy/Patterns/MeteorEnemy.cs Assets/Scripts/Core/Patterns/MeteorShowerPattern.cs
git commit -m "feat(day3): add MeteorEnemy and MeteorShowerPattern"
```

---

## Task 7: MiniBossEnemy + MiniBossPattern

**Files:**
- Create: `Assets/Scripts/Enemy/Patterns/MiniBossEnemy.cs`
- Create: `Assets/Scripts/Core/Patterns/MiniBossPattern.cs`

MiniBossEnemy owns its movement entirely. Phase 1: descend to `BOSS_CENTER_Y`. Phase 2: horizontal sinusoidal oscillation. `_reachedCenter` is reset in `OnEnable()` (pool re-use safe) by calling `base.OnEnable()` first so `_released` is also reset.

MiniBossPattern just places the boss and does not override `UpdateMovement()`.

- [ ] **Step 1: Create MiniBossEnemy.cs**

Create `Assets/Scripts/Enemy/Patterns/MiniBossEnemy.cs`:

```csharp
// Attach to: MiniBoss Enemy prefab root
using UnityEngine;
using ShooterGame.Utils;

namespace ShooterGame.Enemy
{
    public class MiniBossEnemy : EnemyBase
    {
        [SerializeField] private float sweepFrequency = 1.5f;
        [SerializeField] private float sweepAmplitude = 3f;

        private bool _reachedCenter;

        protected override void OnEnable()
        {
            base.OnEnable();
            _reachedCenter = false;
        }

        protected override void Move()
        {
            if (!_reachedCenter)
            {
                Vector3 target = new Vector3(transform.position.x, Constants.BOSS_CENTER_Y, 0f);
                transform.position = Vector3.MoveTowards(transform.position, target, CurrentSpeed * Time.deltaTime);

                if (Mathf.Abs(transform.position.y - Constants.BOSS_CENTER_Y) < 0.05f)
                    _reachedCenter = true;
            }
            else
            {
                float newX = Mathf.Sin(Time.time * sweepFrequency) * sweepAmplitude;
                transform.position = new Vector3(newX, Constants.BOSS_CENTER_Y, 0f);
            }
        }
    }
}
```

- [ ] **Step 2: Create MiniBossPattern.cs**

Create `Assets/Scripts/Core/Patterns/MiniBossPattern.cs`:

```csharp
// Attach to: MiniBoss Pattern prefab root
using UnityEngine;
using ShooterGame.Utils;

namespace ShooterGame.Core
{
    public class MiniBossPattern : PatternBase
    {
        [SerializeField] private float spawnOffsetY = 1f;

        protected override void ArrangeEnemies()
        {
            EnemyBase boss       = GetEnemy();
            boss.transform.position = new Vector3(0f, Constants.PLAY_HALF_HEIGHT + spawnOffsetY, 0f);
        }
        // UpdateMovement not overridden — MiniBossEnemy.Move() handles all movement
    }
}
```

- [ ] **Step 3: Verify in Unity Editor**

Zero compile errors.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Enemy/Patterns/MiniBossEnemy.cs Assets/Scripts/Core/Patterns/MiniBossPattern.cs
git commit -m "feat(day3): add MiniBossEnemy (two-phase movement) and MiniBossPattern"
```

---

## Task 8: DifficultyManager — boss timer

**Files:**
- Modify: `Assets/Scripts/Core/DifficultyManager.cs`

Add `using System;`, the `OnMiniBossSpawn` event, the `bossInterval` SerializeField, and a `_bossTimer` float. The timer only runs while the game is running (same guard as existing difficulty Update).

- [ ] **Step 1: Add using System; import**

Open `Assets/Scripts/Core/DifficultyManager.cs`. Add `using System;` below `using UnityEngine;`:

```csharp
using System;
using UnityEngine;
```

- [ ] **Step 2: Add boss timer fields and event**

After the `maxHpMult` SerializeField, add:

```csharp
// ── Mini-boss timer ──────────────────────────────────────
[SerializeField] private float bossInterval = 120f;

public event Action OnMiniBossSpawn;
private float _bossTimer;
```

- [ ] **Step 3: Add boss timer logic in Update()**

At the end of the `Update()` method body, after the three difficulty property lines, add:

```csharp
_bossTimer += Time.deltaTime;
if (_bossTimer >= bossInterval)
{
    _bossTimer = 0f;
    OnMiniBossSpawn?.Invoke();
}
```

The full `Update()` method now looks like:

```csharp
private void Update()
{
    if (InGameManager.Instance == null || !InGameManager.Instance.IsGameRunning) return;

    float t = InGameManager.Instance.ElapsedTime;

    SpawnInterval        = Mathf.Clamp(baseInterval * Mathf.Exp(-k * t), minInterval, baseInterval);
    EnemySpeedMultiplier = Mathf.Clamp(1f + speedGain * t, 1f, maxSpeedMult);
    EnemyHpMultiplier    = Mathf.Clamp(1f + hpGain * t,    1f, maxHpMult);

    _bossTimer += Time.deltaTime;
    if (_bossTimer >= bossInterval)
    {
        _bossTimer = 0f;
        OnMiniBossSpawn?.Invoke();
    }
}
```

- [ ] **Step 4: Verify in Unity Editor**

Zero compile errors. Enter Play Mode — DifficultyManager now compiles with boss timer. No visual test yet (PatternManager is not wired up).

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Core/DifficultyManager.cs
git commit -m "feat(day3): add OnMiniBossSpawn event and boss timer to DifficultyManager"
```

---

## Task 9: PatternManager

**Files:**
- Create: `Assets/Scripts/Core/PatternManager.cs`

PatternManager is a scene-scoped singleton. It holds references to four pattern prefabs (each a GameObject with a PatternBase subclass already attached). `SpawnPattern()` calls `Object.Instantiate()` once per 20 seconds — acceptable because it's not in a per-frame loop. The manual float timer (no `new WaitForSeconds` in loop) mirrors EnemySpawner's approach.

`AddPatternComponent()` selects the correct prefab by `PatternConfig.Kind`, so PatternManager never needs to `switch` on type — it just Instantiates the right prefab.

- [ ] **Step 1: Create the file**

Create `Assets/Scripts/Core/PatternManager.cs`:

```csharp
// Attach to: PatternManager GameObject (Game scene only — no DontDestroyOnLoad)
using System;
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

        private PatternBase _activePattern;
        private bool        _running;
        private float       _patternTimer;

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

            if (DifficultyManager.Instance != null)
                DifficultyManager.Instance.OnMiniBossSpawn += SpawnMiniBoss;
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
            List<PatternConfig> eligible = new List<PatternConfig>();

            foreach (PatternConfig cfg in patternConfigs)
            {
                if (cfg != null && cfg.Kind != PatternType.MiniBoss && cfg.UnlockTime <= elapsed)
                    eligible.Add(cfg);
            }

            if (eligible.Count == 0) return;

            PatternConfig selected = eligible[UnityEngine.Random.Range(0, eligible.Count)];
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
```

- [ ] **Step 2: Verify in Unity Editor**

Zero compile errors.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Core/PatternManager.cs
git commit -m "feat(day3): add PatternManager scene singleton"
```

---

## Task 10: Unity Editor Setup

Wire up all prefabs, ScriptableObjects, and the PatternManager GameObject in the Game scene. No code changes — all Unity Inspector work.

### A. Create folder structure

In the Unity Project window, create these folders if they don't exist:
- `Assets/Prefabs/Enemies/Patterns/`
- `Assets/ScriptableObjects/Enemies/`

### B. Create EnemyData assets for new enemy types

Right-click `Assets/ScriptableObjects/Enemies/` → Create → ShooterGame → Enemy Data:

| Asset name | BaseHp | MoveSpeed | ScoreValue | ContactDamage |
|------------|--------|-----------|------------|---------------|
| `EnemyData_Sweep` | 20 | 4 | 10 | 1 |
| `EnemyData_CircleTrap` | 20 | 0 | 10 | 1 |
| `EnemyData_Meteor` | 9999 | 6 | 0 | 2 |
| `EnemyData_MiniBoss` | 200 | 3 | 500 | 3 |

### C. Create enemy prefabs

For each enemy type, in `Assets/Prefabs/Enemies/Patterns/`:

1. **SweepEnemy prefab**
   - New empty GameObject → add component `EnemyBase`
   - Add `Collider2D` (CircleCollider2D, radius 0.4), tag = `Enemy`
   - Add `SpriteRenderer` (placeholder sprite)
   - Save as `Prefabs/Enemies/Patterns/SweepEnemy.prefab`

2. **CircleTrapEnemy prefab** — same structure as SweepEnemy, save as `CircleTrapEnemy.prefab`

3. **MeteorEnemy prefab**
   - New empty GO → add component `MeteorEnemy`
   - Add `Collider2D`, tag = `Enemy`, larger sprite (it's a meteor)
   - Save as `MeteorEnemy.prefab`

4. **MiniBossEnemy prefab**
   - New empty GO → add component `MiniBossEnemy`
   - Set `SweepFrequency` = 1.5, `SweepAmplitude` = 3
   - Add `Collider2D`, tag = `Enemy`, large sprite
   - Save as `MiniBossEnemy.prefab`

### D. Create PatternConfig assets

Right-click `Assets/ScriptableObjects/Enemies/` → Create → ShooterGame → Pattern Config:

| Asset name | Kind | EnemyPrefab | EnemyData | EnemyCount | UnlockTime | PatternDuration |
|------------|------|-------------|-----------|------------|------------|-----------------|
| `PatternConfig_Sweep` | ScreenSweep | SweepEnemy | EnemyData_Sweep | 5 | 0 | 8 |
| `PatternConfig_Circle` | CircleTrap | CircleTrapEnemy | EnemyData_CircleTrap | 6 | 30 | 10 |
| `PatternConfig_Meteor` | MeteorShower | MeteorEnemy | EnemyData_Meteor | 5 | 60 | 6 |
| `PatternConfig_MiniBoss` | MiniBoss | MiniBossEnemy | EnemyData_MiniBoss | 1 | 0 | 60 |

### E. Create pattern prefabs (for PatternManager references)

In `Assets/Prefabs/Enemies/Patterns/`:

1. **ScreenSweepPattern prefab** — empty GO → add `ScreenSweepPattern` component → save as `ScreenSweepPattern.prefab`
2. **CircleTrapPattern prefab** — empty GO → add `CircleTrapPattern` component → save as `CircleTrapPattern.prefab`
3. **MeteorShowerPattern prefab** — empty GO → add `MeteorShowerPattern` → `StaggerInterval` = 0.5 → save as `MeteorShowerPattern.prefab`
4. **MiniBossPattern prefab** — empty GO → add `MiniBossPattern` component → save as `MiniBossPattern.prefab`

### F. Add PatternManager to the Game scene

1. In the Game scene Hierarchy: Create Empty → rename to `PatternManager`
2. Add component `PatternManager`
3. Wire up Inspector fields:
   - `Pattern Configs` (list, size 3): drag `PatternConfig_Sweep`, `PatternConfig_Circle`, `PatternConfig_Meteor`
   - `Pattern Interval`: 20
   - `Screen Sweep Prefab`: drag `ScreenSweepPattern.prefab`
   - `Circle Trap Prefab`: drag `CircleTrapPattern.prefab`
   - `Meteor Shower Prefab`: drag `MeteorShowerPattern.prefab`
   - `Mini Boss Prefab`: drag `MiniBossPattern.prefab`
   - `Mini Boss Config`: drag `PatternConfig_MiniBoss`

### G. Play Mode verification

- [ ] Enter Play Mode. Linear descent enemies spawn as before (EnemySpawner unchanged).
- [ ] After 20 seconds: a ScreenSweep pattern fires — 5 enemies cross the screen horizontally.
- [ ] Shoot a sweep enemy — it dies and score increases.
- [ ] After ~30 seconds elapsed: CircleTrap becomes eligible (unlockTime=30). Patterns randomly alternate.
- [ ] After ~60 seconds: Meteors appear staggered. Shooting a meteor has no effect (indestructible).
- [ ] For MiniBoss testing: temporarily set `DifficultyManager.BossInterval` to 10 in Inspector during Play Mode. After 10 seconds, an active pattern is interrupted and the boss descends to center then sweeps.
- [ ] Boss is destroyed when HP drops to 0 — ScoreManager increases by 500.

- [ ] **Step: Commit scene changes**

```bash
git add Assets/Scenes/Game.unity Assets/ScriptableObjects/ Assets/Prefabs/Enemies/Patterns/
git commit -m "feat(day3): wire up patterns, prefabs, and PatternManager in Game scene"
```

---

## Self-Review Checklist

- [x] **Spec coverage:** All §4.1–4.12 components have a corresponding task.
- [x] **Placeholder scan:** No TBD/TODO. All code blocks are complete.
- [x] **Type consistency:** `PatternConfig.Kind` (PatternType) used consistently in PatternManager's switch and TrySpawnPattern filter. `ForceReturnToPool()` defined in Task 1, called in Task 3. `ActiveEnemies` defined protected in Task 3, accessed by Tasks 4 & 5. `BOSS_CENTER_Y` defined in Task 1, used in Task 7.
- [x] **CLAUDE.md compliance:** No `Instantiate()` in loops. No `Camera.main` in Update. No raw tag strings. `WaitForSeconds` cached in Awake (MeteorShower). Manual float timer in PatternManager.Update(). All events unsubscribed in OnDestroy. PatternManager is scene-scoped (no DDOL).
