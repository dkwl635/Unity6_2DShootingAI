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

---

## 8. Prohibited Patterns

- `FindObjectOfType()` inside `Update()` ❌
- `Instantiate()` for high-frequency objects (bullets, effects) ❌ → Use Object Pool
- Hardcoded magic numbers in code ❌ → Use `[SerializeField]` or ScriptableObject
- Multiple system responsibilities in one script ❌ → Follow Single Responsibility Principle (SRP)
