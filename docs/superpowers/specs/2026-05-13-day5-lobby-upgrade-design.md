# Day 5: Lobby Permanent Upgrade System — Design Spec

**Date:** 2026-05-13
**Scope:** Lobby scene permanent upgrade panel + SaveManager persistence + in-game bonus application

---

## 1. Goal

Allow players to spend coins earned in-game to purchase permanent stat upgrades in the Lobby scene. Bonuses persist across sessions and are applied to the player automatically at game start.

---

## 2. Upgrade Definitions

| Type | Display Name | Gain Per Level | Base Cost | Cost Multiplier |
|------|-------------|----------------|-----------|-----------------|
| `MaxHp` | 최대 체력 | +1 HP | 100 | ×2 |
| `Damage` | 공격력 | +2 damage | 100 | ×2 |
| `AttackSpeed` | 공격 속도 | -0.03s fire interval | 150 | ×2 |
| `MagnetRange` | 마그넷 범위 | +0.5 radius | 100 | ×2 |

- Max level: **5** per upgrade
- Cost formula: `baseCost × costMultiplier^(level - 1)`
  - Level 1: 100, Level 2: 200, Level 3: 400, Level 4: 800, Level 5: 1600

---

## 3. Architecture

### New Files

| File | Location | Type | Role |
|------|----------|------|------|
| `LobbyUpgradeType.cs` | `Scripts/Meta/` | Enum | 4 upgrade type identifiers |
| `LobbyUpgradeData.cs` | `Scripts/Meta/` | ScriptableObject | Per-upgrade definition (cost, gain, name) |
| `LobbyUpgradeManager.cs` | `Scripts/Meta/` | MonoBehaviour (scene-only) | Purchase logic, event publisher |
| `LobbyUpgradePanel.cs` | `Scripts/UI/` | MonoBehaviour | Panel — initializes slots, listens to events |
| `LobbyUpgradeSlot.cs` | `Scripts/UI/` | MonoBehaviour | Single slot — renders state, fires buy click |

### Modified Files

| File | Change |
|------|--------|
| `SaveManager.cs` | Add `int[]` upgrade level storage (PlayerPrefs keys: `LobbyUpgrade_0…3`) |
| `InGameManager.cs` | Call `ApplyPermanentBonuses()` on game start |
| `PlayerStats.cs` | Add `ApplyPermanentHpBonus(int level)` |
| `PlayerShooter.cs` | Add `ApplyPermanentDamageBonus(int)`, `ApplyPermanentAtkSpeedBonus(int)` |
| `MagnetEffect.cs` | Add `ApplyPermanentMagnetBonus(int level)` |

### ScriptableObject Assets (4개 생성)

`Assets/ScriptableObjects/LobbyUpgrades/`
- `LobbyUpgrade_MaxHp.asset`
- `LobbyUpgrade_Damage.asset`
- `LobbyUpgrade_AttackSpeed.asset`
- `LobbyUpgrade_MagnetRange.asset`

---

## 4. Data Flow

```
[Lobby Scene]
LobbyUpgradeData (SO) ──► LobbyUpgradeManager.TryPurchase(type)
                               ├─ SaveManager.AddCoins(-cost)
                               ├─ SaveManager.SetUpgradeLevel(type, level)
                               └─ OnUpgradeChanged?.Invoke()
                                       └─► LobbyUpgradePanel.RefreshAll()
                                               └─► LobbyUpgradeSlot.Render(data, level)

[Game Scene Start]
SaveManager ──► InGameManager.ApplyPermanentBonuses()
                    ├─► PlayerStats.ApplyPermanentHpBonus(level)
                    ├─► PlayerShooter.ApplyPermanentDamageBonus(level)
                    ├─► PlayerShooter.ApplyPermanentAtkSpeedBonus(level)
                    └─► MagnetEffect.ApplyPermanentMagnetBonus(level)
```

---

## 5. Purchase Logic

```
LobbyUpgradeManager.TryPurchase(LobbyUpgradeType type):
  currentLevel = SaveManager.GetUpgradeLevel(type)
  if currentLevel >= maxLevel → return (already max)
  cost = baseCost × costMultiplier^currentLevel
  if SaveManager.TotalCoins < cost → return (insufficient coins)
  SaveManager.SpendCoins(cost)      // 새 메서드 — AddCoins는 양수 전용
  SaveManager.SetUpgradeLevel(type, currentLevel + 1)
  OnUpgradeChanged?.Invoke()
```

---

## 6. SaveManager Extension

```csharp
// PlayerPrefs keys
"LobbyUpgrade_0" // MaxHp
"LobbyUpgrade_1" // Damage
"LobbyUpgrade_2" // AttackSpeed
"LobbyUpgrade_3" // MagnetRange

public int  GetUpgradeLevel(LobbyUpgradeType type)
public void SetUpgradeLevel(LobbyUpgradeType type, int level)
public void SpendCoins(int cost)   // 코인 차감 전용 (AddCoins는 양수 전용이라 별도 분리)
// LoadAll() / ForceSave() updated to include upgrade levels
```

---

## 7. UI Hierarchy (Lobby Scene)

```
Canvas
├── LobbyUpgradePanel
│   ├── TitleText          "영구 업그레이드"
│   ├── CoinText           "보유 코인: 350"
│   ├── UpgradeSlot_MaxHp
│   ├── UpgradeSlot_Damage
│   ├── UpgradeSlot_AttackSpeed
│   └── UpgradeSlot_MagnetRange
└── PlayButton             → loads Game scene
```

**Slot layout per row:**
`[Name] [Level ●●●○○] [Cost: 400코인] [BUY button]`
- BUY disabled + text "MAX" when level == 5
- BUY disabled when coins < cost

---

## 8. In-Game Bonus Application

Applied once in `InGameManager` at game start (before gameplay begins).
Bonus values are cumulative across levels (level × gainPerLevel).

| Type | Applied To | Method |
|------|-----------|--------|
| MaxHp | `PlayerStats` | `ApplyPermanentHpBonus(int totalGain)` → calls `IncreaseMaxHp()` |
| Damage | `PlayerShooter` | `ApplyPermanentDamageBonus(int totalGain)` → calls `IncreaseDamage()` |
| AttackSpeed | `PlayerShooter` | `ApplyPermanentAtkSpeedBonus(float totalReduction)` → calls `IncreaseFireRate()` |
| MagnetRange | `MagnetEffect` | `ApplyPermanentMagnetBonus(float totalGain)` → increases radius |

---

## 9. Out of Scope (Day 5)

- Upgrade icons / artwork
- Upgrade reset / refund
- Coin shop (IAP)
- Lobby background / visual polish
