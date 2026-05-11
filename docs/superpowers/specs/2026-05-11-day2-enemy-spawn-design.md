# Day 2 Design Spec — Enemy Spawn & Difficulty System

**Date:** 2026-05-11  
**Engine:** Unity 2022.3 LTS | **Language:** C#  
**Roadmap:** Day 2 of 7 — Infinite Enemy Spawn + Difficulty Scaling

---

## 1. Scope

| In Scope | Out of Scope |
|----------|--------------|
| `EnemyData` ScriptableObject | Mini-boss (Day 3) |
| `EnemyBase` base class | Enemy patterns beyond LinearDescent (Day 3) |
| `LinearDescentEnemy` (straight-down pattern) | Coin drop / economy (Day 4) |
| `DifficultyManager` (exponential scaling) | |
| `EnemySpawner` (object pool + coroutine loop) | |
| `PlayerStats` (HP, invincibility, game-over trigger) | |

---

## 2. File & Namespace Layout

```
Assets/Scripts/Enemy/
├── EnemyBase.cs                  (ShooterGame.Enemy)
└── Patterns/
    └── LinearDescentEnemy.cs     (ShooterGame.Enemy)

Assets/Scripts/Player/
└── PlayerStats.cs                (ShooterGame.Player)

Assets/Scripts/Core/
├── DifficultyManager.cs          (ShooterGame.Core)
└── EnemySpawner.cs               (ShooterGame.Core)

Assets/ScriptableObjects/Enemies/
└── EnemyData.cs                  (ShooterGame.Enemy)
```

---

## 3. Data Flow

```
InGameManager.ElapsedTime
       │
       ▼
DifficultyManager          (reads elapsed time each frame, exposes scaled properties)
  .SpawnInterval
  .EnemySpeedMultiplier
  .EnemyHpMultiplier
       │
       ▼
EnemySpawner               (coroutine loop, ObjectPool<EnemyBase>)
  Pool.Get()
  EnemyBase.Initialize(EnemyData, hpMult, speedMult)
       │
       ▼
LinearDescentEnemy         (moves straight down each frame)
       │
       ├─ Bullet.OnTriggerEnter2D ──► EnemyBase.TakeDamage(bullet.GetDamage())
       │                                       │
       │                              HP ≤ 0 ──► Die()
       │                                       ├─ ScoreManager.Add(scoreValue)
       │                                       └─ Pool.Release(this)
       │
       └─ OnTriggerEnter2D(Player) ──► PlayerStats.TakeDamage(contactDamage)
                                               │
                                        HP ≤ 0 ──► InGameManager.TriggerGameOver()
```

---

## 4. Component Specs

### 4.1 EnemyData (ScriptableObject)

| Field | Type | Description |
|-------|------|-------------|
| `baseHp` | `int` | Base HP before difficulty multiplier |
| `moveSpeed` | `float` | Base move speed before difficulty multiplier |
| `scoreValue` | `int` | Score granted to ScoreManager on death |
| `contactDamage` | `int` | Damage dealt to player on contact |

- Defined with `[CreateAssetMenu]` → create assets in `Assets/ScriptableObjects/Enemies/`

---

### 4.2 EnemyBase

**Attach to:** Enemy prefab root

| Member | Detail |
|--------|--------|
| `Initialize(EnemyData, float hpMult, float speedMult)` | Called by EnemySpawner after Pool.Get(). Sets currentHp and currentSpeed. |
| `TakeDamage(int dmg)` | Reduces HP; calls Die() if HP ≤ 0 |
| `Die()` (virtual) | ScoreManager.Add(scoreValue) → Pool.Release(this) |
| `OnTriggerEnter2D` | If Player tag → PlayerStats.TakeDamage(contactDamage) → Die() |
| Off-screen check | If transform.position.y < bottomBound → Pool.Release(this) without score |

- Bottom bound cached in `Awake()` via Constants.PLAY_HALF_HEIGHT
- Does NOT move — movement delegated to subclass via `protected virtual void Move()`

---

### 4.3 LinearDescentEnemy

**Attach to:** LinearDescent Enemy prefab

- Inherits `EnemyBase`
- `override void Move()` — `transform.Translate(Vector3.down * currentSpeed * Time.deltaTime)`
- No additional fields

---

### 4.4 DifficultyManager

**Attach to:** DifficultyManager GameObject (Game scene only — no DontDestroyOnLoad)

**Formulas** (all values clamped):

```
spawnInterval  = Clamp(baseInterval × e^(−k × t),  minInterval,  maxInterval)
enemySpeed     = Clamp(baseSpeed + speedGain × t,   baseSpeed,    maxSpeed)
hpMultiplier   = Clamp(1 + hpGain × t,              1f,           maxHpMult)
```

| SerializeField | Default | Description |
|----------------|---------|-------------|
| `baseInterval` | 2.0f | Initial spawn interval (seconds) |
| `minInterval` | 0.4f | Minimum spawn interval |
| `k` | 0.02f | Exponential decay rate |
| `baseSpeed` | 3.0f | Initial enemy speed multiplier |
| `speedGain` | 0.01f | Speed increase per second |
| `maxSpeed` | 8.0f | Speed cap |
| `hpGain` | 0.005f | HP multiplier increase per second |
| `maxHpMult` | 5.0f | HP multiplier cap |

- Reads `InGameManager.Instance.ElapsedTime` in Update (float arithmetic only — no allocation)
- Exposes `SpawnInterval`, `EnemySpeedMultiplier`, `EnemyHpMultiplier` as read-only properties

---

### 4.5 EnemySpawner

**Attach to:** EnemySpawner GameObject (Game scene only)

- `ObjectPool<EnemyBase>` initialized in `Awake()` with `Constants.POOL_SIZE_ENEMY`
- Subscribes to `InGameManager.OnGameStart` → `StartSpawning()`
- Subscribes to `InGameManager.OnGameOver` → `StopSpawning()`
- Spawn coroutine uses a **manual float timer** (no `WaitForSeconds`) because spawn interval
  changes dynamically — creating a new `WaitForSeconds` each tick would violate CLAUDE.md rule:
  ```
  float timer = 0f;
  while (isSpawning) {
      timer += Time.deltaTime;
      if (timer >= DifficultyManager.SpawnInterval) { SpawnEnemy(); timer = 0f; }
      yield return null;
  }
  ```
  1. Pick random X in `[−PLAY_HALF_WIDTH + margin, PLAY_HALF_WIDTH − margin]`
  2. Spawn Y = `PLAY_HALF_HEIGHT + spawnOffsetY` (just above screen top)
  3. `pool.Get()` → `enemy.Initialize(enemyData, hpMult, speedMult)`
- Tracks active enemies in `List<EnemyBase>` for `ReleaseAll()` on scene unload
- `OnDestroy()` → unsubscribes events + calls `ReleaseAll()`

---

### 4.6 PlayerStats

**Attach to:** Player GameObject

| SerializeField | Default | Description |
|----------------|---------|-------------|
| `maxHp` | 3 | Maximum HP |
| `invincibleDuration` | 1.5f | Invincibility window after hit (seconds) |

| Member | Detail |
|--------|--------|
| `TakeDamage(int dmg)` | Ignored during invincibility. Reduces HP. HP ≤ 0 → TriggerGameOver(). |
| `OnHpChanged` | `event Action<int, int>` (currentHp, maxHp) — HUD binds to this |
| `IsInvincible` | `bool` property, driven by coroutine timer |

- Invincibility coroutine uses cached `WaitForSeconds`
- On death: `InGameManager.Instance.TriggerGameOver()`

---

## 5. Key Constraints (from CLAUDE.md)

- All enemy/effect spawning via **ObjectPool** — no `Instantiate()` in loops
- All tunable values via `[SerializeField]` — no magic numbers in code
- All tags/layers via `Constants.cs` — no raw string literals
- `Camera.main` must NOT be accessed in `Update()` — cache in `Awake()`
- `WaitForSeconds` must be cached — no `new WaitForSeconds()` inside coroutines
- Event listeners unsubscribed in `OnDestroy()`
- DifficultyManager and EnemySpawner are **scene-scoped** (no DontDestroyOnLoad)

---

## 6. Out-of-Scope Decisions

- **Mini-boss:** DifficultyManager will NOT include 2-minute mini-boss trigger. Added in Day 3.
- **ScreenSweep / CircleTrap / other patterns:** Day 3 only. EnemyBase is designed to be extended.
- **Coin drop:** EnemyBase.Die() does NOT spawn coins. Economy system is Day 4.
- **PlayerStats persistence:** No save on HP — session only.
