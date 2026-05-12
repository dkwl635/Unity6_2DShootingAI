# Day 4 Part 1 — Coin Drop System Design

**Date:** 2026-05-12  
**Scope:** Economy/CoinDrop + Economy/ExpDrop + CoinSystem + ExpSystem + DropManager  
**Depends on:** EnemyBase, EnemyData, ObjectPool, Constants  
**Feeds into:** Part 2 (MagnetEffect), Part 3 (UpgradeManager / LevelUpPanel)

---

## 1. Overview

When an enemy dies, it drops Coin and EXP items at its position. Both items fall downward at a constant speed and are collected when the player touches them. Items that exit the bottom of the screen are returned to their respective object pools. Coin count per enemy varies by `EnemyData`; EXP amount also comes from `EnemyData`.

---

## 2. Files Created / Modified

| File | Location | Action |
|------|----------|--------|
| `EnemyData.cs` | `Assets/Scripts/Enemy/` | Add `coinDrop` and `expDrop` int fields |
| `EnemyBase.cs` | `Assets/Scripts/Enemy/` | Fire `static event OnEnemyDied` from `Die()` |
| `DropBase.cs` | `Assets/Scripts/Economy/` | Abstract base: fall movement + player collision |
| `CoinDrop.cs` | `Assets/Scripts/Economy/` | Inherits DropBase, calls `CoinSystem.Add()` |
| `ExpDrop.cs` | `Assets/Scripts/Economy/` | Inherits DropBase, calls `ExpSystem.Add()` |
| `DropManager.cs` | `Assets/Scripts/Economy/` | Scene singleton, holds both pools, subscribes to event |
| `CoinSystem.cs` | `Assets/Scripts/Economy/` | Scene singleton, coin total + `OnCoinChanged` event |
| `ExpSystem.cs` | `Assets/Scripts/Economy/` | Scene singleton, EXP total + `OnLevelUp` event |
| `Constants.cs` | `Assets/Scripts/Utils/` | Add `POOL_SIZE_EXP = 40` |

---

## 3. Data Flow

```
EnemyBase.Die()
  └─ static event OnEnemyDied(Vector3 pos, int coinAmt, int expAmt)
        └─ DropManager (subscribed)
              ├─ coinPool.Get() → CoinDrop.Initialize(pos, coinAmt, release)
              │     └─ falls down → player trigger → CoinSystem.Add(coinAmt)
              │                                            └─ OnCoinChanged(total) → HUD
              └─ expPool.Get() → ExpDrop.Initialize(pos, expAmt, release)
                    └─ falls down → player trigger → ExpSystem.Add(expAmt)
                                                           └─ OnLevelUp → (Part 3) UpgradeManager
```

---

## 4. EnemyData Changes

```csharp
[SerializeField] private int coinDrop = 1;   // coins spawned on death
[SerializeField] private int expDrop  = 5;   // exp granted on death

public int CoinDrop => coinDrop;
public int ExpDrop  => expDrop;
```

---

## 5. EnemyBase Changes

```csharp
// Static event — DropManager subscribes; no direct reference needed
public static event Action<Vector3, int, int> OnEnemyDied;

protected virtual void Die()
{
    ScoreManager.Instance?.Add(_data.ScoreValue);
    OnEnemyDied?.Invoke(transform.position, _data.CoinDrop, _data.ExpDrop);
    ReturnToPool();
}
```

---

## 6. DropBase

```csharp
// Attach to: CoinDrop prefab / ExpDrop prefab
// Abstract base for falling collectible items.
namespace ShooterGame.Economy
{
    public abstract class DropBase : MonoBehaviour
    {
        [SerializeField] protected float fallSpeed = 2f;

        private Action<DropBase> _releaseCallback;
        private float _bottomBound;

        private void Awake()
        {
            _bottomBound = -(Constants.PLAY_HALF_HEIGHT + 1f);
        }

        public void Initialize(Vector3 position, Action<DropBase> releaseCallback)
        {
            transform.position = position;
            _releaseCallback = releaseCallback;
        }

        private void Update()
        {
            transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);
            if (transform.position.y < _bottomBound)
                ReturnToPool();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag(Constants.TAG_PLAYER))
            {
                OnCollect();
                ReturnToPool();
            }
        }

        protected abstract void OnCollect();

        private void ReturnToPool()
        {
            gameObject.SetActive(false);
            _releaseCallback?.Invoke(this);
            _releaseCallback = null;
        }
    }
}
```

---

## 7. CoinDrop / ExpDrop

```csharp
// CoinDrop — value set by DropManager before use
public class CoinDrop : DropBase
{
    private int _value;
    public void SetValue(int v) => _value = v;
    protected override void OnCollect() => CoinSystem.Instance?.Add(_value);
}

// ExpDrop — identical shape
public class ExpDrop : DropBase
{
    private int _value;
    public void SetValue(int v) => _value = v;
    protected override void OnCollect() => ExpSystem.Instance?.Add(_value);
}
```

---

## 8. DropManager

```csharp
// Attach to: DropManager GameObject (Game scene only)
// Scene singleton — holds ObjectPool<CoinDrop> and ObjectPool<ExpDrop>.
// Subscribes to EnemyBase.OnEnemyDied; spawns drops at death position.
public class DropManager : MonoBehaviour
{
    public static DropManager Instance { get; private set; }

    [SerializeField] private CoinDrop coinDropPrefab;
    [SerializeField] private ExpDrop  expDropPrefab;

    private ObjectPool<CoinDrop> _coinPool;
    private ObjectPool<ExpDrop>  _expPool;

    private void Awake() { /* singleton guard, no DDOL */ }
    private void OnEnable()  => EnemyBase.OnEnemyDied += HandleEnemyDied;
    private void OnDisable() => EnemyBase.OnEnemyDied -= HandleEnemyDied;

    private void HandleEnemyDied(Vector3 pos, int coin, int exp)
    {
        // Spawn one object per drop type, not one object per coin unit.
        // CoinDrop value = coin (e.g., MiniBoss drops one item worth 5 coins).
        // Skip spawn if value is 0 to avoid wasted pool usage.
        if (coin > 0) SpawnCoin(pos, coin);
        if (exp  > 0) SpawnExp(pos, exp);
    }
}
```

---

## 9. CoinSystem

```csharp
// Attach to: CoinSystem GameObject (Game scene only)
// Tracks in-game coin total and broadcasts changes for HUD.
public class CoinSystem : MonoBehaviour
{
    public static CoinSystem Instance { get; private set; }
    public event Action<int> OnCoinChanged;
    public int Total { get; private set; }

    public void Add(int amount)
    {
        Total += amount;
        OnCoinChanged?.Invoke(Total);
    }

    private void OnGameStart() => Total = 0;   // reset on InGameManager.OnGameStart
}
```

---

## 10. ExpSystem

```csharp
// Attach to: ExpSystem GameObject (Game scene only)
// Tracks EXP and fires OnLevelUp when threshold is crossed.
// Level-up curve: expToNextLevel = 10 * currentLevel  (tunable via SerializeField)
public class ExpSystem : MonoBehaviour
{
    public static ExpSystem Instance { get; private set; }
    public event Action<int, int> OnExpChanged;  // (currentExp, expToNext)
    public event Action<int>      OnLevelUp;     // (newLevel) — Part 3 hook

    [SerializeField] private int baseExpPerLevel = 10;  // multiplied by level

    public int CurrentExp   { get; private set; }
    public int CurrentLevel { get; private set; } = 1;
    public int ExpToNext    => baseExpPerLevel * CurrentLevel;

    public void Add(int amount)
    {
        CurrentExp += amount;
        OnExpChanged?.Invoke(CurrentExp, ExpToNext);
        while (CurrentExp >= ExpToNext)
        {
            CurrentExp -= ExpToNext;
            CurrentLevel++;
            OnLevelUp?.Invoke(CurrentLevel);
        }
    }
}
```

---

## 11. Constants Addition

```csharp
public const int POOL_SIZE_EXP = 40;
```

---

## 12. Prefab Requirements

| Prefab | Tag | Layer | Collider |
|--------|-----|-------|----------|
| `CoinDrop` | `Coin` | `Coin` | Circle Collider 2D (IsTrigger = true) |
| `ExpDrop` | — | `Default` | Circle Collider 2D (IsTrigger = true) |

Both prefabs need a Sprite Renderer. Sprite assets can be placeholder for now.

---

## 13. Out of Scope (Part 1)

- Magnet auto-collection → Part 2
- Level-up UI panel → Part 3
- Coin persistence / lobby currency → Day 5
