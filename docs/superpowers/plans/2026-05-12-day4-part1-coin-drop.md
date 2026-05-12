# Day 4 Part 1 — Coin Drop System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 적 처치 시 코인·EXP 아이템이 드롭되어 낙하하고, 플레이어가 접촉 시 수집되는 경제 시스템을 구현한다.

**Architecture:** `EnemyBase.Die()`가 static event `OnEnemyDied`를 발행하면, 씬 싱글톤 `DropManager`가 구독하여 `ObjectPool<CoinDrop>` / `ObjectPool<ExpDrop>`에서 아이템을 꺼내 스폰한다. `CoinSystem`과 `ExpSystem`은 집계만 담당하며 각각 `OnCoinChanged`, `OnLevelUp` 이벤트를 발행해 HUD·UpgradeManager와 디커플링 통신한다.

**Tech Stack:** Unity 2022.3 LTS, C#, Unity Physics2D (Trigger), ObjectPool\<T\> (기존 유틸)

---

## File Map

| Action | Path | Responsibility |
|--------|------|----------------|
| Modify | `Assets/Scripts/Utils/Constants.cs` | `POOL_SIZE_EXP = 40` 추가 |
| Modify | `Assets/Scripts/Enemy/EnemyData.cs` | `coinDrop`, `expDrop` 필드 추가 |
| Modify | `Assets/Scripts/Enemy/EnemyBase.cs` | `static event OnEnemyDied` 추가, `Die()`에서 발행 |
| Create | `Assets/Scripts/Economy/DropBase.cs` | 낙하 이동 + 플레이어 충돌 수집 추상 베이스 |
| Create | `Assets/Scripts/Economy/CoinDrop.cs` | DropBase 상속, CoinSystem.Add() 호출 |
| Create | `Assets/Scripts/Economy/ExpDrop.cs` | DropBase 상속, ExpSystem.Add() 호출 |
| Create | `Assets/Scripts/Economy/CoinSystem.cs` | 씬 싱글톤, 코인 합산 + OnCoinChanged |
| Create | `Assets/Scripts/Economy/ExpSystem.cs` | 씬 싱글톤, EXP 합산 + OnLevelUp (레벨업 curve) |
| Create | `Assets/Scripts/Economy/DropManager.cs` | 씬 싱글톤, 두 풀 관리, OnEnemyDied 구독 |

---

## Task 1: Constants — POOL_SIZE_EXP 추가

**Files:**
- Modify: `Assets/Scripts/Utils/Constants.cs`

- [ ] **Step 1: Constants.cs 에 EXP 풀 크기 상수 추가**

`POOL_SIZE_COIN = 40;` 라인 바로 아래에 다음 한 줄을 추가한다.

```csharp
public const int POOL_SIZE_EXP = 40;
```

- [ ] **Step 2: Unity 콘솔에서 컴파일 오류 없음 확인**

`read_console` 툴로 Error 필터 확인. 오류 없으면 통과.

- [ ] **Step 3: 커밋**

```
git add Assets/Scripts/Utils/Constants.cs
git commit -m "feat(day4): add POOL_SIZE_EXP constant"
```

---

## Task 2: EnemyData — coinDrop / expDrop 필드 추가

**Files:**
- Modify: `Assets/Scripts/Enemy/EnemyData.cs`

- [ ] **Step 1: EnemyData.cs 에 두 필드 및 프로퍼티 추가**

기존 `contactDamage` 필드 바로 아래에 추가:

```csharp
[SerializeField] private int coinDrop = 1;
[SerializeField] private int expDrop  = 5;

public int CoinDrop => coinDrop;
public int ExpDrop  => expDrop;
```

- [ ] **Step 2: 컴파일 오류 없음 확인**

`read_console` Error 필터 확인.

- [ ] **Step 3: 커밋**

```
git add Assets/Scripts/Enemy/EnemyData.cs
git commit -m "feat(day4): add coinDrop and expDrop to EnemyData"
```

---

## Task 3: EnemyBase — OnEnemyDied static 이벤트 추가

**Files:**
- Modify: `Assets/Scripts/Enemy/EnemyBase.cs`

- [ ] **Step 1: using System; 이미 있는지 확인 후 static event 선언 추가**

클래스 상단 필드 선언부에 추가:

```csharp
// Static event — DropManager subscribes without a direct reference to EnemyBase instances
public static event Action<Vector3, int, int> OnEnemyDied;
```

- [ ] **Step 2: Die() 메서드에 이벤트 발행 추가**

기존 `Die()`:
```csharp
protected virtual void Die()
{
    ScoreManager.Instance?.Add(_data.ScoreValue);
    ReturnToPool();
}
```

수정 후:
```csharp
protected virtual void Die()
{
    ScoreManager.Instance?.Add(_data.ScoreValue);
    OnEnemyDied?.Invoke(transform.position, _data.CoinDrop, _data.ExpDrop);
    ReturnToPool();
}
```

- [ ] **Step 3: 컴파일 오류 없음 확인**

`read_console` Error 필터 확인.

- [ ] **Step 4: 커밋**

```
git add Assets/Scripts/Enemy/EnemyBase.cs
git commit -m "feat(day4): fire OnEnemyDied static event from EnemyBase.Die()"
```

---

## Task 4: DropBase — 낙하 이동 추상 베이스 클래스 생성

**Files:**
- Create: `Assets/Scripts/Economy/DropBase.cs`

- [ ] **Step 1: DropBase.cs 생성**

```csharp
// Attach to: CoinDrop prefab / ExpDrop prefab
using System;
using UnityEngine;
using ShooterGame.Utils;

namespace ShooterGame.Economy
{
    public abstract class DropBase : MonoBehaviour
    {
        [SerializeField] protected float fallSpeed = 2f;

        private Action _onRelease;
        private float  _bottomBound;
        private bool   _released;

        private void Awake()
        {
            _bottomBound = -(Constants.PLAY_HALF_HEIGHT + 1f);
        }

        protected virtual void OnEnable()
        {
            _released = false;
        }

        public void Initialize(Vector3 position, Action onRelease)
        {
            transform.position = position;
            _onRelease = onRelease;
        }

        private void Update()
        {
            if (_released) return;
            transform.Translate(Vector3.down * (fallSpeed * Time.deltaTime));
            if (transform.position.y < _bottomBound)
                ReturnToPool();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_released) return;
            if (other.CompareTag(Constants.TAG_PLAYER))
            {
                OnCollect();
                ReturnToPool();
            }
        }

        protected abstract void OnCollect();

        protected void ReturnToPool()
        {
            if (_released) return;
            _released = true;
            _onRelease?.Invoke();
            _onRelease = null;
        }
    }
}
```

- [ ] **Step 2: 컴파일 오류 없음 확인**

`read_console` Error 필터 확인.

- [ ] **Step 3: 커밋**

```
git add Assets/Scripts/Economy/DropBase.cs
git commit -m "feat(day4): add DropBase abstract class for falling collectibles"
```

---

## Task 5: CoinDrop — 코인 수집 오브젝트 생성

**Files:**
- Create: `Assets/Scripts/Economy/CoinDrop.cs`

- [ ] **Step 1: CoinDrop.cs 생성**

```csharp
// Attach to: CoinDrop prefab
namespace ShooterGame.Economy
{
    public class CoinDrop : DropBase
    {
        private int _value;

        public void SetValue(int value) => _value = value;

        protected override void OnCollect() => CoinSystem.Instance?.Add(_value);
    }
}
```

- [ ] **Step 2: 컴파일 오류 없음 확인**

`read_console` Error 필터 확인.

- [ ] **Step 3: 커밋**

```
git add Assets/Scripts/Economy/CoinDrop.cs
git commit -m "feat(day4): add CoinDrop collectible"
```

---

## Task 6: ExpDrop — EXP 수집 오브젝트 생성

**Files:**
- Create: `Assets/Scripts/Economy/ExpDrop.cs`

- [ ] **Step 1: ExpDrop.cs 생성**

```csharp
// Attach to: ExpDrop prefab
namespace ShooterGame.Economy
{
    public class ExpDrop : DropBase
    {
        private int _value;

        public void SetValue(int value) => _value = value;

        protected override void OnCollect() => ExpSystem.Instance?.Add(_value);
    }
}
```

- [ ] **Step 2: 컴파일 오류 없음 확인**

`read_console` Error 필터 확인.

- [ ] **Step 3: 커밋**

```
git add Assets/Scripts/Economy/ExpDrop.cs
git commit -m "feat(day4): add ExpDrop collectible"
```

---

## Task 7: CoinSystem — 코인 집계 씬 싱글톤 생성

**Files:**
- Create: `Assets/Scripts/Economy/CoinSystem.cs`

- [ ] **Step 1: CoinSystem.cs 생성**

```csharp
// Attach to: CoinSystem GameObject (Game scene only — no DontDestroyOnLoad)
using System;
using UnityEngine;
using ShooterGame.Core;

namespace ShooterGame.Economy
{
    public class CoinSystem : MonoBehaviour
    {
        public static CoinSystem Instance { get; private set; }

        public event Action<int> OnCoinChanged;

        public int Total { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (InGameManager.Instance != null)
                InGameManager.Instance.OnGameStart += ResetCoins;
        }

        public void Add(int amount)
        {
            if (amount <= 0) return;
            Total += amount;
            OnCoinChanged?.Invoke(Total);
        }

        private void ResetCoins()
        {
            Total = 0;
            OnCoinChanged?.Invoke(Total);
        }

        private void OnDestroy()
        {
            if (InGameManager.Instance != null)
                InGameManager.Instance.OnGameStart -= ResetCoins;
            if (Instance == this) Instance = null;
        }
    }
}
```

- [ ] **Step 2: 컴파일 오류 없음 확인**

`read_console` Error 필터 확인.

- [ ] **Step 3: 커밋**

```
git add Assets/Scripts/Economy/CoinSystem.cs
git commit -m "feat(day4): add CoinSystem scene singleton"
```

---

## Task 8: ExpSystem — EXP 집계 + 레벨업 이벤트 씬 싱글톤 생성

**Files:**
- Create: `Assets/Scripts/Economy/ExpSystem.cs`

- [ ] **Step 1: ExpSystem.cs 생성**

```csharp
// Attach to: ExpSystem GameObject (Game scene only — no DontDestroyOnLoad)
// Level-up curve: expToNextLevel = baseExpPerLevel * currentLevel
using System;
using UnityEngine;
using ShooterGame.Core;

namespace ShooterGame.Economy
{
    public class ExpSystem : MonoBehaviour
    {
        public static ExpSystem Instance { get; private set; }

        public event Action<int, int> OnExpChanged;  // (currentExp, expToNext)
        public event Action<int>      OnLevelUp;     // (newLevel) — Part 3 hook

        [SerializeField] private int baseExpPerLevel = 10;

        public int CurrentExp   { get; private set; }
        public int CurrentLevel { get; private set; } = 1;
        public int ExpToNext    => baseExpPerLevel * CurrentLevel;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (InGameManager.Instance != null)
                InGameManager.Instance.OnGameStart += ResetExp;
        }

        public void Add(int amount)
        {
            if (amount <= 0) return;
            CurrentExp += amount;
            OnExpChanged?.Invoke(CurrentExp, ExpToNext);

            while (CurrentExp >= ExpToNext)
            {
                CurrentExp -= ExpToNext;
                CurrentLevel++;
                OnLevelUp?.Invoke(CurrentLevel);
                OnExpChanged?.Invoke(CurrentExp, ExpToNext);
            }
        }

        private void ResetExp()
        {
            CurrentExp   = 0;
            CurrentLevel = 1;
            OnExpChanged?.Invoke(CurrentExp, ExpToNext);
        }

        private void OnDestroy()
        {
            if (InGameManager.Instance != null)
                InGameManager.Instance.OnGameStart -= ResetExp;
            if (Instance == this) Instance = null;
        }
    }
}
```

- [ ] **Step 2: 컴파일 오류 없음 확인**

`read_console` Error 필터 확인.

- [ ] **Step 3: 커밋**

```
git add Assets/Scripts/Economy/ExpSystem.cs
git commit -m "feat(day4): add ExpSystem with level-up event for Part 3 hook"
```

---

## Task 9: DropManager — 풀 관리 + 이벤트 구독 씬 싱글톤 생성

**Files:**
- Create: `Assets/Scripts/Economy/DropManager.cs`

- [ ] **Step 1: DropManager.cs 생성**

```csharp
// Attach to: DropManager GameObject (Game scene only — no DontDestroyOnLoad)
// Holds ObjectPool<CoinDrop> and ObjectPool<ExpDrop>.
// Subscribes to EnemyBase.OnEnemyDied and spawns drops at death position.
using UnityEngine;
using ShooterGame.Enemy;
using ShooterGame.Utils;

namespace ShooterGame.Economy
{
    public class DropManager : MonoBehaviour
    {
        public static DropManager Instance { get; private set; }

        [SerializeField] private CoinDrop coinDropPrefab;
        [SerializeField] private ExpDrop  expDropPrefab;

        private ObjectPool<CoinDrop> _coinPool;
        private ObjectPool<ExpDrop>  _expPool;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _coinPool = new ObjectPool<CoinDrop>(coinDropPrefab, Constants.POOL_SIZE_COIN, transform);
            _expPool  = new ObjectPool<ExpDrop>(expDropPrefab,   Constants.POOL_SIZE_EXP,  transform);
        }

        private void OnEnable()  => EnemyBase.OnEnemyDied += HandleEnemyDied;
        private void OnDisable() => EnemyBase.OnEnemyDied -= HandleEnemyDied;

        private void HandleEnemyDied(Vector3 pos, int coin, int exp)
        {
            if (coin > 0) SpawnCoin(pos, coin);
            if (exp  > 0) SpawnExp(pos, exp);
        }

        private void SpawnCoin(Vector3 pos, int value)
        {
            CoinDrop drop = _coinPool.Get();
            drop.SetValue(value);
            drop.Initialize(pos, () => _coinPool.Release(drop));
        }

        private void SpawnExp(Vector3 pos, int value)
        {
            ExpDrop drop = _expPool.Get();
            drop.SetValue(value);
            drop.Initialize(pos, () => _expPool.Release(drop));
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
```

- [ ] **Step 2: 컴파일 오류 없음 확인**

`read_console` Error 필터 확인.

- [ ] **Step 3: 커밋**

```
git add Assets/Scripts/Economy/DropManager.cs
git commit -m "feat(day4): add DropManager with coin+exp object pools"
```

---

## Task 10: 씬 및 프리팹 설정

**Files:**
- Scene: `Assets/Scenes/Game.unity`
- Create Prefab: `Assets/Prefabs/Drops/CoinDrop.prefab`
- Create Prefab: `Assets/Prefabs/Drops/ExpDrop.prefab`

- [ ] **Step 1: CoinDrop 프리팹 생성**

Hierarchy에 빈 GameObject 생성 → 이름: `CoinDrop`.
컴포넌트 추가:
- `Sprite Renderer` (임시 스프라이트 할당, 노란색 계열)
- `Circle Collider 2D` → Is Trigger = ✅, Radius = 0.25
- `CoinDrop` (스크립트)

Tag → `Coin` 설정.
`Assets/Prefabs/Drops/` 폴더에 프리팹으로 저장 후 씬에서 삭제.

- [ ] **Step 2: ExpDrop 프리팹 생성**

동일하게 `ExpDrop` GameObject 생성.
컴포넌트 추가:
- `Sprite Renderer` (임시 스프라이트, 파란색 계열)
- `Circle Collider 2D` → Is Trigger = ✅, Radius = 0.25
- `ExpDrop` (스크립트)

Tag는 Default 유지 (Player 레이어와 충돌 감지는 Physics2D Layer Matrix로 설정).  
`Assets/Prefabs/Drops/ExpDrop.prefab`으로 저장 후 씬에서 삭제.

- [ ] **Step 3: Player 콜라이더 확인**

Player GameObject에 `Collider2D`가 붙어있고 Tag가 `Player`인지 확인.  
없으면 `Circle Collider 2D`를 추가한다 (Is Trigger = false).

- [ ] **Step 4: CoinSystem, ExpSystem, DropManager GameObject 씬에 추가**

Hierarchy에 빈 GameObject 3개 생성:
- `CoinSystem` → `CoinSystem.cs` 컴포넌트 추가
- `ExpSystem` → `ExpSystem.cs` 컴포넌트 추가
- `DropManager` → `DropManager.cs` 컴포넌트 추가  
  → Inspector에서 `Coin Drop Prefab`, `Exp Drop Prefab` 슬롯에 각 프리팹 연결

- [ ] **Step 5: Physics2D 레이어 매트릭스 확인**

`Edit → Project Settings → Physics 2D → Layer Collision Matrix`  
`Coin` 레이어 ↔ `Player` 레이어 충돌이 활성화되어 있는지 확인.  
비활성화되어 있으면 체크박스를 켠다.

- [ ] **Step 6: Play Mode 동작 검증**

Play Mode 진입 → 적을 처치.
- 적 사망 위치에 코인·EXP 아이템이 스폰되는지 확인
- 아이템이 아래로 낙하하는지 확인
- 플레이어가 아이템에 닿으면 사라지는지 확인
- `read_console`에서 런타임 에러 없음 확인

- [ ] **Step 7: 씬 저장 및 최종 커밋**

```
git add Assets/Scenes/Game.unity Assets/Prefabs/Drops/
git commit -m "feat(day4-part1): complete coin drop system - CoinDrop, ExpDrop, CoinSystem, ExpSystem, DropManager"
```
