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
- Putting in-game session logic into `GameManager` ❌ → Use `InGameManager` for session-scoped state

---

## 9. Design Improvement Suggestions (Claude 자동 언급 규칙)

작업 완료 후, 아래 상황에 해당하면 Claude가 먼저 개선 방향을 언급한다.
유저가 먼저 묻기 전에 선제적으로 제안하는 것이 원칙이다.

### 언급해야 할 상황
| 상황 | 예시 |
|------|------|
| 한 클래스가 2가지 이상의 책임을 갖고 있을 때 | GameManager가 점수/저장까지 처리 |
| `DontDestroyOnLoad` 여부가 설계 의도와 맞지 않을 때 | 씬 전용 Manager에 DDOL이 붙어 있을 때 |
| Manager 간 직접 참조가 이벤트로 교체 가능할 때 | A.Instance.Method()를 Action으로 대체 가능 |
| 금지 패턴(섹션 8)이 코드에서 발견될 때 | Update 내 Camera.main, 루프 내 Instantiate 등 |
| ScriptableObject로 분리할 수 있는 하드코딩 데이터가 있을 때 | 스탯·수치가 코드에 직접 박혀 있을 때 |
| Object Pool 없이 Instantiate/Destroy가 반복될 때 | 총알·이펙트를 매번 생성·파괴하는 구조 |

### 언급 방식
- 작업 완료 요약 직후, 별도 단락으로 짧게 언급
- "개선 포인트가 있습니다 — 반영할까요?" 형식으로 제안
- 강요하지 않고 유저가 판단하도록 선택권 부여

## 10. Mobile Performance Rules

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

## 11. Manager Class Responsibility Rules

### "Manager"라고 다 같은 Manager가 아니다
Manager 클래스를 만들 때 반드시 아래 기준으로 역할을 분리한다.
이름이 Manager라고 해서 여러 책임을 한 클래스에 몰아넣지 않는다.

| 클래스 | 담당 책임 | DontDestroyOnLoad | 씬 범위 |
|---|---|---|---|
| `GameManager` | 씬 전환, 앱 생명주기, 전역 초기화 | ✅ | 전체 |
| `InGameManager` | 인게임 세션 상태, 경과 시간, 게임오버 트리거 | ❌ | Game 씬 전용 |
| `ScoreManager` | 점수 집계, 이벤트 발행 | ❌ | Game 씬 전용 |
| `SaveManager` | PlayerPrefs / JSON 저장·불러오기 | ✅ | 전체 |
| `AudioManager` | BGM / SFX 재생 | ✅ | 전체 |

### 판단 기준 — 새 Manager를 만들기 전에 자문하라
1. **이 클래스가 하는 일이 한 가지인가?** → 아니라면 분리
2. **씬이 바뀌어도 유지되어야 하는가?** → Yes면 `DontDestroyOnLoad` + 싱글톤, No면 씬 전용
3. **다른 Manager를 직접 호출하는가?** → 가능하면 이벤트(`C# Action`)로 교체
4. **"GameManager에 넣으면 편하다"는 이유만으로 넣으려 하는가?** → 반드시 거부

### 씬 전용 Manager 패턴
```csharp
// Game 씬 전용 Manager — DontDestroyOnLoad 없음
private void Awake()
{
    if (Instance != null && Instance != this) { Destroy(gameObject); return; }
    Instance = this;
    // NOTE: intentionally no DontDestroyOnLoad
}

private void OnDestroy()
{
    if (Instance == this) Instance = null; // 씬 언로드 시 ref 정리
}
```

---

## 12. Scene Management & Initialization Order

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

## 13. Constants & Tag Management

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
