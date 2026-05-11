# Day 2 — Enemy Spawn & Difficulty System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement infinite enemy spawning with exponential difficulty scaling, a base enemy class, a LinearDescent pattern enemy, and a player HP system.

**Architecture:** `DifficultyManager` reads `InGameManager.ElapsedTime` each frame and exposes scaled multipliers. `EnemySpawner` runs a manual-timer coroutine, calls `ObjectPool<EnemyBase>.Get()`, and passes the multipliers plus a release callback into `EnemyBase.Initialize()`. `PlayerStats` sits on the Player GameObject and handles HP, invincibility frames, and game-over triggering.

**Tech Stack:** Unity 2022.3 LTS, C#, Unity 2D Physics, ScriptableObjects, `ObjectPool<T>` (existing generic pool in `Assets/Scripts/Utils/ObjectPool.cs`)

---

## File Map

| Action | Path | Responsibility |
|--------|------|----------------|
| Create | `Assets/Scripts/Enemy/EnemyData.cs` | ScriptableObject: base HP, speed, score, contact damage |
| Create | `Assets/Scripts/Player/PlayerStats.cs` | HP, invincibility, game-over trigger |
| Create | `Assets/Scripts/Enemy/EnemyBase.cs` | Damage intake, pool release, player collision |
| Create | `Assets/Scripts/Enemy/Patterns/LinearDescentEnemy.cs` | Straight-down movement |
| Create | `Assets/Scripts/Core/DifficultyManager.cs` | Exponential scaling via elapsed time |
| Create | `Assets/Scripts/Core/EnemySpawner.cs` | Spawn loop, pool management |
| Modify | `Assets/Scripts/Player/Bullet.cs` | Add `TakeDamage` call on enemy hit |

---

## Task 1: EnemyData ScriptableObject

**Files:**
- Create: `Assets/Scripts/Enemy/EnemyData.cs`

- [ ] **Step 1: Create the file**

```csharp
// Attach to: (ScriptableObject — no GameObject attachment needed)
using UnityEngine;

namespace ShooterGame.Enemy
{
    [CreateAssetMenu(fileName = "EnemyData", menuName = "ShooterGame/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [SerializeField] public int   baseHp        = 30;
        [SerializeField] public float moveSpeed     = 3f;
        [SerializeField] public int   scoreValue    = 10;
        [SerializeField] public int   contactDamage = 1;
    }
}
```

Save to `Assets/Scripts/Enemy/EnemyData.cs`.

- [ ] **Step 2: Verify compile**

Switch to Unity. Wait for the spinner in the bottom-right to finish.  
Open **Console** (Window → General → Console).  
Expected: zero errors related to `EnemyData`.

- [ ] **Step 3: Create the asset**

In Unity Project window, right-click `Assets/ScriptableObjects/Enemies/` →  
**Create → ShooterGame → Enemy Data**.  
Name it `BasicEnemy`. Leave default values for now.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Enemy/EnemyData.cs Assets/ScriptableObjects/Enemies/BasicEnemy.asset
git commit -m "feat: add EnemyData ScriptableObject and BasicEnemy asset"
```

---

## Task 2: PlayerStats

**Files:**
- Create: `Assets/Scripts/Player/PlayerStats.cs`

- [ ] **Step 1: Create the file**

```csharp
// Attach to: Player GameObject
using System;
using System.Collections;
using UnityEngine;
using ShooterGame.Core;

namespace ShooterGame.Player
{
    public class PlayerStats : MonoBehaviour
    {
        [SerializeField] private int   maxHp              = 3;
        [SerializeField] private float invincibleDuration = 1.5f;

        public event Action<int, int> OnHpChanged; // (currentHp, maxHp)

        public int  CurrentHp    { get; private set; }
        public bool IsInvincible { get; private set; }

        private WaitForSeconds _invincibleWait;

        private void Awake()
        {
            CurrentHp = maxHp;
            _invincibleWait = new WaitForSeconds(invincibleDuration);
        }

        private void Start()
        {
            if (InGameManager.Instance != null)
                InGameManager.Instance.OnGameStart += ResetHp;
        }

        public void TakeDamage(int dmg)
        {
            if (IsInvincible || dmg <= 0) return;

            CurrentHp = Mathf.Max(0, CurrentHp - dmg);
            OnHpChanged?.Invoke(CurrentHp, maxHp);

            if (CurrentHp <= 0)
            {
                InGameManager.Instance?.TriggerGameOver();
                return;
            }

            StartCoroutine(InvincibilityRoutine());
        }

        private IEnumerator InvincibilityRoutine()
        {
            IsInvincible = true;
            yield return _invincibleWait;
            IsInvincible = false;
        }

        private void ResetHp()
        {
            CurrentHp    = maxHp;
            IsInvincible = false;
            OnHpChanged?.Invoke(CurrentHp, maxHp);
        }

        private void OnDestroy()
        {
            if (InGameManager.Instance != null)
                InGameManager.Instance.OnGameStart -= ResetHp;
        }
    }
}
```

- [ ] **Step 2: Verify compile**

Switch to Unity, wait for compilation.  
Expected: zero errors for `PlayerStats`.

- [ ] **Step 3: Attach to Player GameObject**

In the Game scene hierarchy, select the **Player** GameObject.  
In the Inspector, click **Add Component → PlayerStats**.  
Leave defaults (maxHp: 3, invincibleDuration: 1.5).

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Player/PlayerStats.cs
git commit -m "feat: add PlayerStats with HP, invincibility frames, and game-over trigger"
```

---

## Task 3: EnemyBase

**Files:**
- Create: `Assets/Scripts/Enemy/EnemyBase.cs`

- [ ] **Step 1: Create the file**

```csharp
// Attach to: Enemy Prefab root
using System;
using UnityEngine;
using ShooterGame.Core;
using ShooterGame.Player;
using ShooterGame.Utils;

namespace ShooterGame.Enemy
{
    public class EnemyBase : MonoBehaviour
    {
        protected int   CurrentHp;
        protected float CurrentSpeed;

        private EnemyData         _data;
        private Action<EnemyBase> _releaseCallback;
        private float             _bottomBound;
        private bool              _released;

        private void Awake()
        {
            _bottomBound = -(Constants.PLAY_HALF_HEIGHT + 1f);
        }

        private void OnEnable()
        {
            // Reset guard when taken from pool
            _released = false;
        }

        /// <summary>Called by EnemySpawner immediately after pool.Get().</summary>
        public void Initialize(EnemyData data, float hpMultiplier, float speedMultiplier,
                               Action<EnemyBase> releaseCallback)
        {
            _data            = data;
            _releaseCallback = releaseCallback;
            CurrentHp        = Mathf.RoundToInt(data.baseHp * hpMultiplier);
            CurrentSpeed     = data.moveSpeed * speedMultiplier;
        }

        protected virtual void Update()
        {
            Move();
            if (transform.position.y < _bottomBound)
                ReturnToPool();
        }

        /// <summary>Override in subclasses to define movement pattern.</summary>
        protected virtual void Move() { }

        public void TakeDamage(int dmg)
        {
            if (dmg <= 0 || _released) return;
            CurrentHp -= dmg;
            if (CurrentHp <= 0) Die();
        }

        protected virtual void Die()
        {
            ScoreManager.Instance?.Add(_data.scoreValue);
            ReturnToPool();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_released) return;
            if (other.CompareTag(Constants.TAG_PLAYER))
            {
                PlayerStats stats = other.GetComponent<PlayerStats>();
                stats?.TakeDamage(_data.contactDamage);
                Die();
            }
        }

        private void ReturnToPool()
        {
            if (_released) return;
            _released = true;
            _releaseCallback?.Invoke(this);
        }
    }
}
```

- [ ] **Step 2: Verify compile**

Switch to Unity, wait for compilation.  
Expected: zero errors for `EnemyBase`.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Enemy/EnemyBase.cs
git commit -m "feat: add EnemyBase with TakeDamage, Die, pool release callback, and player collision"
```

---

## Task 4: LinearDescentEnemy

**Files:**
- Create: `Assets/Scripts/Enemy/Patterns/LinearDescentEnemy.cs`

- [ ] **Step 1: Create the Patterns folder and file**

```csharp
// Attach to: LinearDescent Enemy Prefab
using UnityEngine;

namespace ShooterGame.Enemy
{
    public class LinearDescentEnemy : EnemyBase
    {
        protected override void Move()
        {
            transform.Translate(Vector3.down * CurrentSpeed * Time.deltaTime);
        }
    }
}
```

Save to `Assets/Scripts/Enemy/Patterns/LinearDescentEnemy.cs`.

- [ ] **Step 2: Verify compile**

Switch to Unity, wait for compilation.  
Expected: zero errors for `LinearDescentEnemy`.

- [ ] **Step 3: Create the enemy prefab**

In Unity, go to `Assets/Prefabs/Enemies/`.  
Right-click → **Create Empty** GameObject in the scene, name it `LinearDescentEnemy`.  
Add components:
- **Sprite Renderer** (assign any placeholder sprite, e.g. a white square)
- **Rigidbody2D**: Body Type = Kinematic, Simulated = true
- **CircleCollider2D**: Is Trigger = true, Radius = 0.4
- **LinearDescentEnemy** script component

Drag it from hierarchy to `Assets/Prefabs/Enemies/` to create the prefab.  
Delete from scene hierarchy after prefab is saved.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Enemy/Patterns/LinearDescentEnemy.cs Assets/Prefabs/Enemies/LinearDescentEnemy.prefab
git commit -m "feat: add LinearDescentEnemy pattern and prefab"
```

---

## Task 5: DifficultyManager

**Files:**
- Create: `Assets/Scripts/Core/DifficultyManager.cs`

- [ ] **Step 1: Create the file**

```csharp
// Attach to: DifficultyManager GameObject (Game scene only — no DontDestroyOnLoad)
using UnityEngine;

namespace ShooterGame.Core
{
    public class DifficultyManager : MonoBehaviour
    {
        public static DifficultyManager Instance { get; private set; }

        // ── Spawn interval: exponential decay ───────────────────
        [SerializeField] private float baseInterval = 2.0f;
        [SerializeField] private float minInterval  = 0.4f;
        [SerializeField] private float k            = 0.02f;

        // ── Speed multiplier: linear ramp ────────────────────────
        [SerializeField] private float speedGain    = 0.01f;
        [SerializeField] private float maxSpeedMult = 3.0f;

        // ── HP multiplier: linear ramp ───────────────────────────
        [SerializeField] private float hpGain       = 0.005f;
        [SerializeField] private float maxHpMult    = 5.0f;

        public float SpawnInterval        { get; private set; }
        public float EnemySpeedMultiplier { get; private set; }
        public float EnemyHpMultiplier    { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            // Initialize to t=0 defaults
            SpawnInterval        = baseInterval;
            EnemySpeedMultiplier = 1f;
            EnemyHpMultiplier    = 1f;
        }

        private void Update()
        {
            if (InGameManager.Instance == null || !InGameManager.Instance.IsGameRunning) return;

            float t = InGameManager.Instance.ElapsedTime;

            SpawnInterval        = Mathf.Clamp(baseInterval * Mathf.Exp(-k * t), minInterval, baseInterval);
            EnemySpeedMultiplier = Mathf.Clamp(1f + speedGain * t, 1f, maxSpeedMult);
            EnemyHpMultiplier    = Mathf.Clamp(1f + hpGain * t,    1f, maxHpMult);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
```

- [ ] **Step 2: Verify compile**

Switch to Unity, wait for compilation.  
Expected: zero errors for `DifficultyManager`.

- [ ] **Step 3: Add to Game scene**

In the Game scene hierarchy, right-click → **Create Empty**, name it `DifficultyManager`.  
Add component: **DifficultyManager**.  
Leave all defaults.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Core/DifficultyManager.cs
git commit -m "feat: add DifficultyManager with exponential spawn interval and linear HP/speed scaling"
```

---

## Task 6: EnemySpawner

**Files:**
- Create: `Assets/Scripts/Core/EnemySpawner.cs`

- [ ] **Step 1: Create the file**

```csharp
// Attach to: EnemySpawner GameObject (Game scene only — no DontDestroyOnLoad)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ShooterGame.Enemy;
using ShooterGame.Utils;

namespace ShooterGame.Core
{
    public class EnemySpawner : MonoBehaviour
    {
        public static EnemySpawner Instance { get; private set; }

        [SerializeField] private EnemyBase enemyPrefab;
        [SerializeField] private EnemyData enemyData;
        [SerializeField] private float     spawnOffsetY = 1f;

        private ObjectPool<EnemyBase> _pool;
        private List<EnemyBase>       _activeEnemies = new List<EnemyBase>();
        private Coroutine             _spawnCoroutine;
        private bool                  _spawning;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _pool = new ObjectPool<EnemyBase>(enemyPrefab, Constants.POOL_SIZE_ENEMY, transform);
        }

        private void Start()
        {
            if (InGameManager.Instance != null)
            {
                InGameManager.Instance.OnGameStart += StartSpawning;
                InGameManager.Instance.OnGameOver  += StopSpawning;

                // Handle case where OnGameStart already fired before we subscribed
                if (InGameManager.Instance.IsGameRunning)
                    StartSpawning();
            }
        }

        private void StartSpawning()
        {
            if (_spawning) return;
            _spawning       = true;
            _spawnCoroutine = StartCoroutine(SpawnLoop());
        }

        private void StopSpawning()
        {
            _spawning = false;
            if (_spawnCoroutine != null)
            {
                StopCoroutine(_spawnCoroutine);
                _spawnCoroutine = null;
            }
        }

        private IEnumerator SpawnLoop()
        {
            float timer = 0f;
            while (_spawning)
            {
                timer += Time.deltaTime;
                float interval = DifficultyManager.Instance != null
                    ? DifficultyManager.Instance.SpawnInterval
                    : 2f;

                if (timer >= interval)
                {
                    SpawnEnemy();
                    timer = 0f;
                }
                yield return null;
            }
        }

        private void SpawnEnemy()
        {
            float x = Random.Range(
                -Constants.PLAY_HALF_WIDTH  + Constants.SCREEN_BOUND_MARGIN,
                 Constants.PLAY_HALF_WIDTH  - Constants.SCREEN_BOUND_MARGIN);
            float y = Constants.PLAY_HALF_HEIGHT + spawnOffsetY;

            EnemyBase enemy = _pool.Get();
            enemy.transform.position = new Vector3(x, y, 0f);

            float hpMult    = DifficultyManager.Instance?.EnemyHpMultiplier    ?? 1f;
            float speedMult = DifficultyManager.Instance?.EnemySpeedMultiplier ?? 1f;

            enemy.Initialize(enemyData, hpMult, speedMult, ReleaseEnemy);
            _activeEnemies.Add(enemy);
        }

        private void ReleaseEnemy(EnemyBase enemy)
        {
            _activeEnemies.Remove(enemy);
            _pool.Release(enemy);
        }

        private void OnDestroy()
        {
            StopSpawning();
            _pool.ReleaseAll(_activeEnemies);

            if (InGameManager.Instance != null)
            {
                InGameManager.Instance.OnGameStart -= StartSpawning;
                InGameManager.Instance.OnGameOver  -= StopSpawning;
            }

            if (Instance == this) Instance = null;
        }
    }
}
```

- [ ] **Step 2: Verify compile**

Switch to Unity, wait for compilation.  
Expected: zero errors for `EnemySpawner`.

- [ ] **Step 3: Add to Game scene and wire Inspector**

In the Game scene hierarchy, right-click → **Create Empty**, name it `EnemySpawner`.  
Add component: **EnemySpawner**.  
In the Inspector:
- **Enemy Prefab** → drag `Assets/Prefabs/Enemies/LinearDescentEnemy.prefab`
- **Enemy Data** → drag `Assets/ScriptableObjects/Enemies/BasicEnemy.asset`
- **Spawn Offset Y** → 1 (default)

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Core/EnemySpawner.cs
git commit -m "feat: add EnemySpawner with ObjectPool, manual-timer coroutine, and difficulty-scaled spawning"
```

---

## Task 7: Update Bullet.cs — call TakeDamage on enemy hit

**Files:**
- Modify: `Assets/Scripts/Player/Bullet.cs` (line 43–46)

The existing `OnTriggerEnter2D` returns the bullet to pool but does not deal damage. Update it to call `TakeDamage`.

- [ ] **Step 1: Edit Bullet.cs**

Find this block in `Assets/Scripts/Player/Bullet.cs`:

```csharp
private void OnTriggerEnter2D(Collider2D other)
{
    // Damage is handled by EnemyBase (Day 2) — just return to pool on hit
    if (other.CompareTag(Utils.Constants.TAG_ENEMY))
        ReturnToPool();
}
```

Replace with:

```csharp
private void OnTriggerEnter2D(Collider2D other)
{
    if (other.CompareTag(Utils.Constants.TAG_ENEMY))
    {
        Enemy.EnemyBase enemy = other.GetComponent<Enemy.EnemyBase>();
        enemy?.TakeDamage(damage);
        ReturnToPool();
    }
}
```

Also add the using statement at the top of `Bullet.cs` if not present:

```csharp
using ShooterGame.Enemy;
```

Then the `OnTriggerEnter2D` can be written without the full qualification:

```csharp
private void OnTriggerEnter2D(Collider2D other)
{
    if (other.CompareTag(Utils.Constants.TAG_ENEMY))
    {
        EnemyBase enemy = other.GetComponent<EnemyBase>();
        enemy?.TakeDamage(damage);
        ReturnToPool();
    }
}
```

- [ ] **Step 2: Verify compile**

Switch to Unity, wait for compilation.  
Expected: zero errors for `Bullet`.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Player/Bullet.cs
git commit -m "fix: Bullet now calls EnemyBase.TakeDamage on hit"
```

---

## Task 8: Scene Setup & Integration Verification

**Files:**
- No new files — scene wiring and play mode testing only.

- [ ] **Step 1: Verify all GameObjects are in the Game scene**

Open the Game scene. Confirm these exist in the hierarchy:
- `GameManager` (persists from Lobby or present in Game scene for testing)
- `InGameManager`
- `ScoreManager`
- `DifficultyManager`
- `EnemySpawner`
- `Player` (with `PlayerController`, `PlayerShooter`, `PlayerStats` components)

- [ ] **Step 2: Verify Physics 2D layer matrix**

Go to **Edit → Project Settings → Physics 2D → Layer Collision Matrix**.  
Ensure:
- **Bullet** layer collides with **Enemy** layer ✓
- **Enemy** layer collides with **Player** layer ✓
- **Bullet** layer does NOT collide with **Player** layer (uncheck)

- [ ] **Step 3: Enter Play mode and verify spawning**

Press **Play**.  
Expected observations (within 10 seconds):
- Enemies appear at the top of the screen
- Enemies move downward
- Enemies disappear when they reach the bottom (pool release, not Destroy)
- Enemies that are shot disappear immediately (pool release)
- Console: zero errors or null reference exceptions

- [ ] **Step 4: Verify difficulty scaling**

Leave the game running for 60 seconds.  
Expected: enemies spawn faster as time passes (interval shrinks from 2s toward 0.4s).  
You can confirm by watching Console if you add a temporary `Debug.Log` in `SpawnEnemy()` to log `DifficultyManager.Instance.SpawnInterval`.  
Remove any debug logs after verification.

- [ ] **Step 5: Verify player HP**

Let an enemy reach and touch the Player.  
Expected:
- Player takes 1 damage (contactDamage from BasicEnemy asset)
- 1.5 second invincibility window (no damage from further contacts)
- At 0 HP: game-over triggers (IsGameRunning = false, OnGameOver fires)

- [ ] **Step 6: Verify bullet deals damage**

Shoot an enemy (tap/hold fire).  
Expected:
- Enemy disappears after enough bullets hit it (baseHp / bullet damage hits)
- Score increases (ScoreManager.Add fires)

- [ ] **Step 7: Final commit**

```bash
git add .
git commit -m "feat: Day 2 complete — enemy spawn, difficulty scaling, player HP system"
```

---

## Self-Review Checklist

- [x] EnemyData SO fields: baseHp, moveSpeed, scoreValue, contactDamage — all used in EnemyBase.Initialize ✓
- [x] EnemyBase.Initialize signature matches EnemySpawner call site ✓
- [x] ReleaseEnemy callback type `Action<EnemyBase>` consistent across EnemyBase and EnemySpawner ✓
- [x] DifficultyManager properties (SpawnInterval, EnemyHpMultiplier, EnemySpeedMultiplier) match EnemySpawner references ✓
- [x] PlayerStats.OnHpChanged signature `Action<int, int>` ready for HUD binding in Day 4 ✓
- [x] WaitForSeconds cached in PlayerStats.Awake — no new in coroutine ✓
- [x] EnemySpawner uses manual float timer instead of WaitForSeconds (dynamic interval) ✓
- [x] All event subscriptions have matching unsubscriptions in OnDestroy ✓
- [x] `_released` guard prevents double pool-release on EnemyBase ✓
- [x] No magic numbers — all tunable values via [SerializeField] or Constants ✓
- [x] No raw tag strings — all tags via Constants.TAG_* ✓
