# Day 4 Part 2 — Magnet Effect Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 플레이어 주변 반경 내 코인·EXP 드롭을 자동으로 흡인하는 자석 효과를 구현한다.

**Architecture:** `MagnetEffect`(Player 부착)가 0.02s 코루틴으로 `Physics2D.OverlapCircleNonAlloc`을 실행해 반경 내 `DropBase`를 감지하고 `Attract()` 신호를 보낸다. `DropBase`는 `_attractTarget`이 설정되면 Update에서 `Vector3.MoveTowards`로 플레이어를 향해 이동한다.

**Tech Stack:** Unity 2022.3 LTS, C#, Physics2D (OverlapCircleNonAlloc), Coroutine

---

## File Map

| Action | Path | Responsibility |
|--------|------|----------------|
| Modify | `Assets/Scripts/Economy/DropBase.cs` | `_attractTarget` 상태 + `Attract()` 메서드 추가 |
| Create | `Assets/Scripts/Player/MagnetEffect.cs` | 코루틴 감지 + 반경 관리 + Part 3 hook |
| Scene  | `Assets/Scenes/Game.unity` | Player에 MagnetEffect 컴포넌트 추가 |

---

## Task 1: DropBase — Attract 상태 추가

**Files:**
- Modify: `Assets/Scripts/Economy/DropBase.cs`

- [ ] **Step 1: `_attractTarget`과 `_attractSpeed` 필드 추가**

`private bool _released;` 라인 바로 아래에 다음 두 필드를 추가한다.

```csharp
private Transform _attractTarget;
private float     _attractSpeed;
```

- [ ] **Step 2: `OnEnable()`에 `_attractTarget = null` 초기화 추가**

기존:
```csharp
protected virtual void OnEnable()
{
    _released = false;
}
```

수정 후:
```csharp
protected virtual void OnEnable()
{
    _released      = false;
    _attractTarget = null;
}
```

- [ ] **Step 3: `Update()` 수정 — 흡인 중일 때 MoveTowards로 이동**

기존:
```csharp
private void Update()
{
    if (_released) return;
    transform.Translate(Vector3.down * (fallSpeed * Time.deltaTime));
    if (transform.position.y < _bottomBound)
        ReturnToPool();
}
```

수정 후:
```csharp
private void Update()
{
    if (_released) return;

    if (_attractTarget != null)
        transform.position = Vector3.MoveTowards(
            transform.position, _attractTarget.position, _attractSpeed * Time.deltaTime);
    else
        transform.Translate(Vector3.down * (fallSpeed * Time.deltaTime));

    if (transform.position.y < _bottomBound)
        ReturnToPool();
}
```

- [ ] **Step 4: `Attract()` 공개 메서드 추가**

`protected abstract void OnCollect();` 바로 위에 추가:

```csharp
public void Attract(Transform target, float speed)
{
    _attractTarget = target;
    _attractSpeed  = speed;
}
```

- [ ] **Step 5: `ReturnToPool()`에 `_attractTarget = null` 추가**

기존:
```csharp
protected void ReturnToPool()
{
    if (_released) return;
    _released = true;
    _onRelease?.Invoke();
    _onRelease = null;
}
```

수정 후:
```csharp
protected void ReturnToPool()
{
    if (_released) return;
    _released      = true;
    _attractTarget = null;
    _onRelease?.Invoke();
    _onRelease = null;
}
```

- [ ] **Step 6: Unity 콘솔에서 컴파일 오류 없음 확인**

`read_console` 툴로 Error 필터 확인.

- [ ] **Step 7: 커밋**

```
git add Assets/Scripts/Economy/DropBase.cs
git commit -m "feat(day4): add Attract state to DropBase for magnet effect"
```

---

## Task 2: MagnetEffect — 코루틴 감지 + 반경 관리

**Files:**
- Create: `Assets/Scripts/Player/MagnetEffect.cs`

- [ ] **Step 1: MagnetEffect.cs 생성**

```csharp
// Attach to: Player GameObject
using System.Collections;
using UnityEngine;
using ShooterGame.Economy;

namespace ShooterGame.Player
{
    public class MagnetEffect : MonoBehaviour
    {
        public static MagnetEffect Instance { get; private set; }

        [SerializeField] private float magnetRadius = 1.5f;
        [SerializeField] private float attractSpeed = 8f;
        [SerializeField] private int   maxHits      = 80;

        private Collider2D[]   _hits;
        private WaitForSeconds _detectWait;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance    = this;
            _hits       = new Collider2D[maxHits];
            _detectWait = new WaitForSeconds(0.02f);
        }

        private void OnEnable()  => StartCoroutine(DetectRoutine());
        private void OnDisable() => StopAllCoroutines();

        private IEnumerator DetectRoutine()
        {
            while (true)
            {
                int count = Physics2D.OverlapCircleNonAlloc(
                    transform.position, magnetRadius, _hits);

                for (int i = 0; i < count; i++)
                {
                    if (_hits[i] == null) continue;
                    DropBase drop = _hits[i].GetComponent<DropBase>();
                    drop?.Attract(transform, attractSpeed);
                }

                yield return _detectWait;
            }
        }

        public void IncreaseRadius(float amount) => magnetRadius += amount;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
```

- [ ] **Step 2: Unity 콘솔에서 컴파일 오류 없음 확인**

`read_console` 툴로 Error 필터 확인.

- [ ] **Step 3: 커밋**

```
git add Assets/Scripts/Player/MagnetEffect.cs
git commit -m "feat(day4): add MagnetEffect coroutine-based drop attraction"
```

---

## Task 3: 씬 설정 — Player에 MagnetEffect 부착

**Files:**
- Scene: `Assets/Scenes/Game.unity`

- [ ] **Step 1: Player GameObject에 MagnetEffect 컴포넌트 추가**

Unity MCP의 `manage_components` 툴 또는 `find_gameobjects`로 Player를 찾은 뒤 `MagnetEffect` 컴포넌트를 추가한다.

Inspector 기본값 확인:
- `Magnet Radius`: 1.5
- `Attract Speed`: 8
- `Max Hits`: 80

- [ ] **Step 2: Play Mode 동작 검증**

Play Mode 진입 후:
1. 적을 처치해 코인·EXP 드롭 생성
2. 플레이어를 드롭 근처로 이동 — 반경 1.5f 내 진입 시 드롭이 플레이어를 향해 부드럽게 이동하는지 확인
3. 드롭이 플레이어에 닿으면 사라지는지 확인
4. `read_console`로 런타임 에러 없음 확인

- [ ] **Step 3: 씬 저장 및 커밋**

```
git add Assets/Scenes/Game.unity
git commit -m "feat(day4-part2): complete magnet effect - attach MagnetEffect to Player"
```
