# Claude Dev Guidelines — Infinite Roguelike Shooter

> **Engine:** Unity 2022.3 LTS | **Language:** C# | **Platform:** Mobile (Android / iOS)
> **Genre:** Infinite Loop Shooter with Roguelike progression

---

## 1. Role & Context

You are a Unity 2022 LTS + C# expert game developer.
Always follow the architecture and rules defined in this document.

### Core Loop
1. **Fly** — Survive as long as possible, shoot down enemies
2. **Collect** — Pick up coins and EXP items dropped by enemies
3. **Grow** — Choose random in-game upgrades (Roguelike) + permanent lobby upgrades (Metagame)
4. **Record** — Beat the best score (survival time / distance)

---

## 2. Code Rules (Always Follow)

| Rule | Detail |
|------|--------|
| **Attach comment** | Top of every script: `// Attach to: [GameObject name]` |
| **Inspector exposure** | All tunable values must use `[SerializeField]` |
| **Singleton scope** | Only for Manager classes (GameManager, AudioManager, UIManager) |
| **Comments** | Write all comments in English |
| **Object Pooling** | Bullets, enemies, effects — always use Object Pool, never `Instantiate()` in loops |
| **Namespace** | Separate by system (e.g. `ShooterGame.Enemy`, `ShooterGame.Upgrade`) |
| **ScriptableObject** | Define all data (enemy stats, upgrade items) as ScriptableObjects |
| **Event system** | Use `C# Action` or `UnityEvent` for cross-system communication. Minimize direct references. |

---

## 3. Project Folder Structure

```
Assets/
├── Scripts/
│   ├── Core/          # GameManager, SceneLoader, InputManager
│   ├── Player/        # PlayerController, PlayerStats, BulletPool
│   ├── Enemy/         # EnemyBase, EnemySpawner, DifficultyManager, Patterns/
│   ├── Upgrade/       # UpgradeManager, UpgradeData(SO), UpgradeUI
│   ├── Economy/       # CoinSystem, CoinDrop, MagnetEffect
│   ├── Meta/          # LobbyUpgradeManager, SaveManager
│   ├── UI/            # HUDController, LevelUpPanel, GameOverPanel
│   └── Utils/         # ObjectPool, ExtensionMethods, Constants
├── Prefabs/
│   ├── Enemies/
│   ├── Bullets/
│   ├── Effects/
│   └── UI/
├── ScriptableObjects/
│   ├── Enemies/
│   ├── Upgrades/
│   └── Patterns/
├── Scenes/
│   ├── Lobby.unity
│   └── Game.unity
├── Audio/
│   ├── BGM/
│   └── SFX/
└── Art/
    ├── Sprites/
    └── Effects/
```

---

## 4. Core System Specs

### A. Difficulty System (DifficultyManager)
- Base variable: `float elapsedTime`
- Enemy HP, speed, and spawn interval scale exponentially over time
- Always clamp with `Mathf.Clamp`
- Mini-boss spawns every **2 minutes**

```
spawnInterval = Mathf.Clamp(baseInterval * e^(-k * t), minInterval, maxInterval)
enemySpeed    = Mathf.Clamp(baseSpeed + speedGain * t, baseSpeed, maxSpeed)
```

### B. Enemy Pattern System
| Pattern | Description |
|---------|-------------|
| `LinearDescent` | Basic straight downward movement |
| `ScreenSweep` | Enemies sweep left to right across screen |
| `CircleTrap` | Surround player, then close in to center |
| `MeteorShower` | Indestructible obstacles — dodge only |
| `FeverTime` | Short invincible bonus phase with mass coin drops |

### C. Upgrade System (UpgradeManager)
- All upgrade data defined via `UpgradeData` ScriptableObject
- Upgrade types managed as `Enum`:

```csharp
public enum UpgradeType {
    AttackSpeed, Damage, Shield, Magnet,
    CriticalRate, MultiShot, HomingMissile, ExpGain
}
```
- On level-up: randomly select 3 upgrades → show UI panel → apply on selection
- Rarity controlled by `weight` field on each UpgradeData

### D. Save System (SaveManager)
- Use `PlayerPrefs` for prototyping → migrate to JSON file later
- Save targets: best score, total coins, permanent upgrade levels

### E. Economy System
- Coin drop: spawn `CoinDrop` prefab on enemy death (Object Pool)
- Magnet: auto-collect coins within radius — radius upgradeable
- Coin multiplier: adjustable via lobby permanent upgrades

---

## 5. 7-Day Roadmap

| Day | Goal | Key Output |
|-----|------|------------|
| **Day 1** | Foundation | Infinite background scroll, touch drag movement, basic shooting |
| **Day 2** | Infinite Spawn | `EnemySpawner.cs` + `DifficultyManager.cs` |
| **Day 3** | Pattern Variety | 5–7 enemy pattern scripts |
| **Day 4** | Economy | Coin drop, magnet effect, in-game level-up UI |
| **Day 5** | Growth System | Lobby upgrade panel + `SaveManager` |
| **Day 6** | Visual Polish | Effects, player animation, `CameraShake` |
| **Day 7** | Balance & Ads | Revive-via-ad logic, final balance testing |

---

## 6. Request Templates

### New Script
```
[Script Request]
Filename: XXX.cs
Attach to: (e.g. EnemySpawner GameObject)
Connected scripts: (e.g. DifficultyManager.cs, GameManager.cs)
Behavior:
- Condition A → Action B
- Variable C must be exposed via [SerializeField]
Output: Full code + key logic explained in comments
```

### Bug / Error
```
[Debug Request]
Unity version: 2022.3 LTS
Error message: (paste here)
Related script: (paste code)
Provide: root cause + fixed code
```

### Feature Extension
```
[Extension Request]
Existing script: (paste code)
New feature: (description)
Extend without breaking existing structure.
```

---

## 7. Code Quality Checklist

- [ ] `// Attach to: [GameObject]` comment at top of script
- [ ] All tunable values use `[SerializeField]`
- [ ] Pooled objects used for bullets / enemies / effects
- [ ] Null checks and exception handling included
- [ ] All comments written in English
- [ ] Data separated into ScriptableObjects
- [ ] No heavy operations inside `Update()` — use caching or coroutines
- [ ] `Camera.main` cached in `Awake()`, not accessed in `Update()`
- [ ] `WaitForSeconds` declared as a cached field, not `new`-ed in coroutines
- [ ] No string literals for tags/layers — use `Constants.cs`
- [ ] Event listeners unsubscribed in `OnDisable()` or `OnDestroy()`
- [ ] Singleton duplicate guard implemented in `Awake()`

---

## 8. Prohibited Patterns

- `FindObjectOfType()` inside `Update()` ❌
- `Instantiate()` for high-frequency objects (bullets, effects) ❌ → Use Object Pool
- Hardcoded magic numbers in code ❌ → Use `[SerializeField]` or ScriptableObject
- Multiple system responsibilities in one script ❌ → Follow Single Responsibility Principle (SRP)
- `Camera.main` inside `Update()` ❌ → Cache in `Awake()` or `Start()`
- String concatenation (`+`) inside loops ❌ → Use `StringBuilder`
- `new WaitForSeconds()` inside coroutines ❌ → Cache as a field
- `SetActive(false/true)` for high-frequency objects ❌ → Use Object Pool instead
- Hardcoded tag/layer strings (e.g. `"Player"`, `"Enemy"`) ❌ → Use `Constants.cs`

---

## 9. Mobile Performance Rules

These rules apply specifically to Android / iOS targets and must never be skipped.

| Rule | Detail |
|------|--------|
| **Cache Camera.main** | Assign `Camera.main` once in `Awake()`, store in a private field. Direct access in `Update()` triggers an internal `FindObjectOfType`. |
| **Avoid GC allocations in hot paths** | No `new` inside `Update()`, coroutine loops, or pooled-object logic. Pre-allocate lists and reuse them. |
| **WaitForSeconds caching** | Declare `private readonly WaitForSeconds _wait = new WaitForSeconds(x);` as a class field and reuse it in all coroutines. |
| **StringBuilder for dynamic text** | Use `StringBuilder` for any HUD text that updates every frame (score, timer). Avoid `string +` or `string.Format` in loops. |
| **Physics2D Layer Matrix** | Configure the collision matrix in Project Settings so only necessary layer pairs interact. Reduces Physics2D overhead significantly. |
| **Texture compression** | Use ETC2 (Android) / ASTC (iOS) for all sprites. Never leave textures at default uncompressed. |
| **Target frame rate** | Set `Application.targetFrameRate = 60;` in GameManager `Awake()`. Do not leave it at platform default. |

---

## 10. Scene Management & Initialization Order

### DontDestroyOnLoad Objects
Only the following Managers persist across scenes. All others must be destroyed and re-created:

- `GameManager` (singleton)
- `AudioManager` (singleton)
- `SaveManager` (singleton)

Each singleton must check for duplicates in `Awake()`:
```csharp
private void Awake()
{
    if (Instance != null && Instance != this) { Destroy(gameObject); return; }
    Instance = this;
    DontDestroyOnLoad(gameObject);
}
```

### Scene Initialization Order
When entering **Game** scene from **Lobby**:
1. `GameManager.OnGameStart()` — resets all runtime state
2. `DifficultyManager.Initialize()` — resets elapsed time
3. `EnemySpawner.StartSpawning()` — begins spawn loop
4. `HUDController.Initialize()` — binds to GameManager events

When returning to **Lobby** from **Game**:
1. All object pools must call `ReleaseAll()` before scene unload
2. Unsubscribe all C# Action / UnityEvent listeners in `OnDestroy()`

### Event Cleanup Rule
Any script that subscribes to an event in `OnEnable()` or `Start()` **must** unsubscribe in `OnDisable()` or `OnDestroy()` to prevent ghost listeners across scene loads.

---

## 11. Constants & Tag Management

All tag strings, layer names, and global numeric constants must be defined in `Assets/Scripts/Utils/Constants.cs`. Never use raw string literals for tags or layers anywhere else in the codebase.

```csharp
// Assets/Scripts/Utils/Constants.cs
namespace ShooterGame.Utils
{
    public static class Constants
    {
        // Tags
        public const string TAG_PLAYER  = "Player";
        public const string TAG_ENEMY   = "Enemy";
        public const string TAG_COIN    = "Coin";
        public const string TAG_BULLET  = "Bullet";

        // Layers
        public const string LAYER_ENEMY  = "Enemy";
        public const string LAYER_PLAYER = "Player";

        // PlayerPrefs keys
        public const string PREF_BEST_SCORE   = "BestScore";
        public const string PREF_TOTAL_COINS  = "TotalCoins";

        // Pool default sizes
        public const int POOL_SIZE_BULLET = 30;
        public const int POOL_SIZE_ENEMY  = 20;
        public const int POOL_SIZE_COIN   = 40;
        public const int POOL_SIZE_EFFECT = 20;
    }
}
```
