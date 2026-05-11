# Day 3 Design Spec — Enemy Pattern Variety

**Date:** 2026-05-11  
**Engine:** Unity 2022.3 LTS | **Language:** C#  
**Roadmap:** Day 3 of 7 — Enemy Pattern System + Mini-Boss

---

## 1. Scope

| In Scope | Out of Scope |
|----------|--------------|
| `PatternConfig` ScriptableObject | FeverTime (Day 4) |
| `PatternBase` abstract MonoBehaviour | Coin drop from pattern enemies (Day 4) |
| `ScreenSweepPattern` | Additional boss movement patterns beyond Phase1/Phase2 |
| `CircleTrapPattern` | Sound effects / VFX (Day 6) |
| `MeteorShowerPattern` | Save/load of pattern unlock state |
| `MiniBossPattern` + `MiniBossEnemy` | |
| `PatternManager` (scene singleton) | |
| `DifficultyManager` — `OnMiniBossSpawn` event + boss timer | |

---

## 2. File & Namespace Layout

```
Assets/Scripts/Enemy/
├── EnemyBase.cs                          (ShooterGame.Enemy)  ← unchanged
├── Patterns/
│   ├── LinearDescentEnemy.cs             (ShooterGame.Enemy)  ← unchanged
│   ├── MeteorEnemy.cs                    (ShooterGame.Enemy)  ← new
│   └── MiniBossEnemy.cs                  (ShooterGame.Enemy)  ← new

Assets/Scripts/Core/
├── PatternBase.cs                        (ShooterGame.Core)   ← new (abstract)
├── Patterns/
│   ├── ScreenSweepPattern.cs             (ShooterGame.Core)   ← new
│   ├── CircleTrapPattern.cs              (ShooterGame.Core)   ← new
│   ├── MeteorShowerPattern.cs            (ShooterGame.Core)   ← new
│   └── MiniBossPattern.cs                (ShooterGame.Core)   ← new
├── PatternManager.cs                     (ShooterGame.Core)   ← new
├── DifficultyManager.cs                  (ShooterGame.Core)   ← add boss timer
└── EnemySpawner.cs                       (ShooterGame.Core)   ← unchanged

Assets/ScriptableObjects/Enemies/
└── PatternConfig.cs                      (ShooterGame.Enemy)  ← new
```

---

## 3. Data Flow

```
InGameManager.ElapsedTime
       │
       ▼
DifficultyManager
  .OnMiniBossSpawn  ─────────────────────────────┐
       │                                         │
       ▼                                         ▼
PatternManager                           PatternManager
  (20s timer)                            (boss override)
  selects PatternConfig                  forces MiniBossPattern
       │
       ▼
PatternBase (concrete subclass)
  .StartPattern(config)
  ├─ ArrangeEnemies()  → ObjectPool<EnemyBase>.Get() × N
  └─ UpdateMovement()  (each frame, pattern-specific)
       │
       ├── enemy.OnDie / ReturnToPool → _pool.Release(enemy)
       │       last release → OnPatternComplete event
       │
       └── MiniBossEnemy.Move()   (boss moves itself, not PatternBase)
```

---

## 4. Component Specs

### 4.1 PatternType Enum

```csharp
// Assets/Scripts/Enemy/PatternConfig.cs
public enum PatternType { ScreenSweep, CircleTrap, MeteorShower, MiniBoss }
```

---

### 4.2 PatternConfig (ScriptableObject)

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `patternType` | `PatternType` | — | Which pattern class to activate |
| `enemyPrefab` | `EnemyBase` | — | Prefab spawned by this pattern |
| `enemyData` | `EnemyData` | — | Stats data passed to Initialize() |
| `enemyCount` | `int` | 5 | Number of enemies per pattern event |
| `unlockTime` | `float` | 0f | Elapsed seconds before this pattern is eligible |
| `patternDuration` | `float` | 8f | Max seconds before pattern self-destructs |

- `[CreateAssetMenu(menuName = "ShooterGame/Pattern Config")]`
- Create assets in `Assets/ScriptableObjects/Enemies/`

---

### 4.3 PatternBase (abstract MonoBehaviour)

**Attach to:** Pattern prefab root (instantiated at runtime by PatternManager)

| Member | Detail |
|--------|--------|
| `StartPattern(PatternConfig config)` | Called by PatternManager. Caches config, creates pool, calls ArrangeEnemies(), starts coroutine. |
| `abstract void ArrangeEnemies()` | Position enemies at spawn; must call GetEnemy() for each slot. |
| `virtual void UpdateMovement()` | Called every frame via coroutine. Override in subclasses for movement logic. |
| `event Action OnPatternComplete` | Fired when all enemies returned to pool OR duration exceeded. |
| `GetEnemy()` | `_pool.Get()` → `enemy.Initialize(config.EnemyData, hpMult, speedMult, ReleaseEnemy)` → returns enemy. |
| `ReleaseEnemy(EnemyBase)` | `_pool.Release(enemy)`. When `_aliveCount == 0` → fires `OnPatternComplete`. |

- `_pool` = `ObjectPool<EnemyBase>` created in `StartPattern()` from `config.EnemyPrefab`
- `_aliveCount` int tracks living enemies; decremented in `ReleaseEnemy()`
- Duration timer: if `patternDuration` expires, calls `ForceComplete()` → releases all remaining, fires event
- Reads `DifficultyManager.Instance.EnemyHpMultiplier / EnemySpeedMultiplier` when getting enemies
- `OnDestroy()` → releases all remaining enemies to pool

---

### 4.4 ScreenSweepPattern : PatternBase

- `ArrangeEnemies()` → line of N enemies horizontally at `y = PLAY_HALF_HEIGHT + spawnOffsetY`, evenly spaced across full play width. Direction chosen randomly (left→right or right→left).
- `UpdateMovement()` → all enemies `transform.Translate(sweepDirection * sweepSpeed * Time.deltaTime)`
- `sweepSpeed` = `[SerializeField] float` (default 4f)
- Enemy returns to pool when `|x| > PLAY_HALF_WIDTH + margin` (handled by EnemyBase off-screen check)

---

### 4.5 CircleTrapPattern : PatternBase

- `ArrangeEnemies()` → distribute N enemies evenly on a circle of radius `spawnRadius` (default 6f) centered on `(0, 0)` (screen center).
- `UpdateMovement()` → each enemy moves toward `Vector3.zero` at `closeSpeed` (default 2f) via `Vector3.MoveTowards`.
- Enemies return to pool when reaching center bounds (`|pos| < 0.5f`) → `ReturnToPool()` called manually in `UpdateMovement()`.

---

### 4.6 MeteorShowerPattern : PatternBase

- Uses `MeteorEnemy` prefab (see §4.7).
- `ArrangeEnemies()` → scatter N meteors at random X across play width, `y = PLAY_HALF_HEIGHT + spawnOffsetY`, staggered spawn times via coroutine (`yield return new WaitForSeconds` cached in Awake).
- `UpdateMovement()` → not needed; MeteorEnemy moves itself via `override Move()`.

#### 4.7 MeteorEnemy : EnemyBase

- `override void Move()` → `transform.Translate(Vector3.down * CurrentSpeed * Time.deltaTime)`
- `override void TakeDamage(int dmg)` → no-op (indestructible)
- `override void Die()` → not callable (TakeDamage blocked); only returns to pool via off-screen check

---

### 4.8 MiniBossPattern : PatternBase

- `ArrangeEnemies()` → places one `MiniBossEnemy` at `(0, PLAY_HALF_HEIGHT + spawnOffsetY)` (top-center).
- `UpdateMovement()` → **not overridden**. Boss movement is owned by `MiniBossEnemy.Move()`.
- `enemyCount` is always 1 for MiniBoss configs.
- Triggered exclusively via `DifficultyManager.OnMiniBossSpawn`.

#### 4.9 MiniBossEnemy : EnemyBase

- Own `EnemyData` asset with higher `baseHp` and `scoreValue` (no code-level distinction needed).
- `override void Move()` — two-phase:
  - **Phase 1:** `Vector3.MoveTowards` downward until `y <= Constants.BOSS_CENTER_Y` (new constant, default 1f)
  - **Phase 2:** horizontal oscillation `Mathf.Sin(Time.time * sweepFrequency) * sweepAmplitude`
  - Phase tracked by `private bool _reachedCenter`
- `[SerializeField] float sweepFrequency = 1.5f`
- `[SerializeField] float sweepAmplitude = 3f`
- Future boss movement variants → new subclass of `MiniBossEnemy` (or `EnemyBase`)

---

### 4.10 PatternManager

**Attach to:** PatternManager GameObject (Game scene only — no DontDestroyOnLoad)

| SerializeField | Type | Default | Description |
|----------------|------|---------|-------------|
| `patternConfigs` | `List<PatternConfig>` | — | All available patterns (Inspector-assigned) |
| `patternInterval` | `float` | 20f | Seconds between pattern events |
| `miniBossPatternConfig` | `PatternConfig` | — | MiniBoss config (Inspector-assigned) |

**Behavior:**
- Subscribes to `InGameManager.OnGameStart → StartLoop()`, `OnGameOver → StopLoop()`
- Subscribes to `DifficultyManager.OnMiniBossSpawn → SpawnMiniBoss()`
- `StartLoop()` → coroutine: manual float timer (same pattern as EnemySpawner — no `new WaitForSeconds` inside loop)
- Pattern selection: filter `patternConfigs` where `unlockTime <= ElapsedTime`, pick random
- At most 1 pattern active: waits for `OnPatternComplete` before selecting next
- `SpawnMiniBoss()` → if active pattern exists, calls `ForceComplete()` on it, then spawns MiniBossPattern
- Pattern GameObject instantiated in `SpawnPattern(config)` → `patternBase.StartPattern(config)` → subscribe `OnPatternComplete`
- On complete: `Destroy(patternGO)`
- `OnDestroy()` → unsubscribe all events

---

### 4.11 DifficultyManager additions

- Add `public event Action OnMiniBossSpawn`
- Add `[SerializeField] private float bossInterval = 120f`
- Add `private float _bossTimer`
- In `Update()`: `_bossTimer += Time.deltaTime; if (_bossTimer >= bossInterval) { OnMiniBossSpawn?.Invoke(); _bossTimer = 0f; }`

---

### 4.12 Constants additions

```csharp
public const float BOSS_CENTER_Y = 1f;
```

---

## 5. Key Constraints (from CLAUDE.md)

- All pattern enemy spawning via `ObjectPool` in `PatternBase` — no `Instantiate()` per enemy
- `PatternConfig` fields via `[SerializeField]` — no magic numbers
- Tags/layers via `Constants.cs` only
- `WaitForSeconds` for MeteorShower stagger: cached in `Awake()` as a field
- Pattern loop timer: manual float timer (same as EnemySpawner) — no `new WaitForSeconds` inside while-loop
- Event listeners unsubscribed in `OnDestroy()`
- `PatternManager` and all Pattern GameObjects are scene-scoped (no DontDestroyOnLoad)

---

## 6. Out-of-Scope Decisions

- **FeverTime:** Pattern type exists in enum but no implementation — Day 4.
- **Coin drops from pattern enemies:** `Die()` does not spawn coins — Day 4.
- **Boss health bar UI:** No HUD widget for boss HP — Day 6 visual polish.
- **Additional boss phases:** `MiniBossEnemy` has Phase 1 + Phase 2 only; new phases added as subclasses in later days.
- **EnemySpawner:** No changes — LinearDescent individual spawn continues in parallel with patterns.
