# Day 4 Part 2 — Magnet Effect Design

**Date:** 2026-05-12  
**Scope:** MagnetEffect (Player) + DropBase 상태 확장  
**Depends on:** DropBase, CoinDrop, ExpDrop (Part 1)  
**Feeds into:** Part 3 (UpgradeManager → MagnetEffect.IncreaseRadius)

---

## 1. Overview

Player에 부착된 `MagnetEffect`가 코루틴(0.02s 간격)으로 반경 내 드롭 아이템(CoinDrop, ExpDrop)을 감지하고, 감지된 드롭에 `Attract()` 신호를 보낸다. 드롭 아이템은 자체 Update에서 `MoveTowards`로 플레이어를 향해 부드럽게 이동한다. 게임 시작부터 기본 반경(1.5f)이 활성화되어 있으며, Part 3의 UpgradeManager가 `IncreaseRadius()`를 호출해 반경을 확장한다.

---

## 2. Files Created / Modified

| File | Location | Action |
|------|----------|--------|
| `MagnetEffect.cs` | `Assets/Scripts/Player/` | 신규 생성 |
| `DropBase.cs` | `Assets/Scripts/Economy/` | 수정 — Attract 상태 추가 |

---

## 3. Data Flow

```
MagnetEffect (코루틴, 0.02s 간격)
  └─ Physics2D.OverlapCircleNonAlloc(playerPos, magnetRadius, _hits)
        └─ hit마다 GetComponent<DropBase>() → drop.Attract(playerTransform, attractSpeed)

DropBase.Update() (매 프레임)
  ├─ _attractTarget == null  →  Vector3.down 낙하
  └─ _attractTarget != null  →  MoveTowards(_attractTarget.position, _attractSpeed)
        └─ 플레이어 도달 시 OnTriggerEnter2D → OnCollect() → ReturnToPool()
              └─ ReturnToPool()에서 _attractTarget = null 초기화

Part 3 연동:
  UpgradeManager → MagnetEffect.Instance?.IncreaseRadius(float amount)
```

---

## 4. MagnetEffect 스펙

```csharp
// Attach to: Player GameObject
using System.Collections;
using UnityEngine;

namespace ShooterGame.Player
{
    public class MagnetEffect : MonoBehaviour
    {
        public static MagnetEffect Instance { get; private set; }

        [SerializeField] private float magnetRadius = 1.5f;
        [SerializeField] private float attractSpeed = 8f;
        [SerializeField] private int   maxHits      = 80;

        private Collider2D[]    _hits;
        private WaitForSeconds  _detectWait;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
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
                    DropBase drop = _hits[i].GetComponent<DropBase>();
                    drop?.Attract(transform, attractSpeed);
                }
                yield return _detectWait;
            }
        }

        // Part 3 hook — called by UpgradeManager on Magnet upgrade
        public void IncreaseRadius(float amount) => magnetRadius += amount;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
```

**설계 참고:**
- `_hits` 배열은 Awake()에서 한 번만 할당 — Update/코루틴 루프 내 GC 할당 없음
- `_detectWait`는 Awake()에서 캐시 — CLAUDE.md `new WaitForSeconds` 규칙 준수
- 싱글톤은 `Destroy(this)` (컴포넌트만 제거, GameObject 유지)
- `OnEnable/OnDisable`로 코루틴 시작/정지 — 씬 라이프사이클 안전

---

## 5. DropBase 수정 사항

### 추가 필드

```csharp
private Transform _attractTarget;
private float     _attractSpeed;
```

### OnEnable() 수정

```csharp
protected virtual void OnEnable()
{
    _released      = false;
    _attractTarget = null;   // 풀에서 꺼낼 때 초기화
}
```

### Update() 수정

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

### 추가 메서드

```csharp
public void Attract(Transform target, float speed)
{
    _attractTarget = target;
    _attractSpeed  = speed;
}
```

### ReturnToPool() 수정

```csharp
protected void ReturnToPool()
{
    if (_released) return;
    _released      = true;
    _attractTarget = null;   // 풀 반환 시 흡인 상태 초기화
    _onRelease?.Invoke();
    _onRelease = null;
}
```

---

## 6. Physics2D 레이어 설정

`OverlapCircleNonAlloc`는 레이어 마스크 없이 호출하고, `GetComponent<DropBase>()`로 필터링한다. CoinDrop(Coin 레이어)과 ExpDrop(Default 레이어) 모두 감지 가능하다.

레이어 마스크를 추가하려면 `Constants.cs`에 Coin + Default 조합 마스크를 정의할 수 있으나, 현재는 `GetComponent` 필터만으로 충분하다.

---

## 7. 업그레이드 연동 (Part 3 참고)

```csharp
// UpgradeManager에서 Magnet 업그레이드 선택 시
MagnetEffect.Instance?.IncreaseRadius(1.0f);  // 반경 +1f 증가
```

---

## 8. Out of Scope (Part 2)

- 자석 업그레이드 UI 및 UpgradeManager 연동 → Part 3
- 자석 반경 시각적 표시 (Gizmo 제외) → Day 6 Visual Polish
- 자석 활성/비활성 토글 → 현재 설계에 없음 (항상 활성)
