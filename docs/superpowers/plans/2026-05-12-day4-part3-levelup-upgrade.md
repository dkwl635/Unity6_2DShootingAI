# Day 4 Part 3 — Level-Up Upgrade System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 레벨업 시 가중치 기반으로 4종 업그레이드 카드 3장을 제시하고, 선택한 카드 효과를 플레이어에 누적 적용하는 인게임 Level-Up Upgrade System을 구현한다.

**Architecture:** `ExpSystem.OnLevelUp` 이벤트가 `UpgradeManager`를 트리거하고, UpgradeManager가 가중치 기반 무작위 3종을 선택해 `LevelUpPanel`에 전달한다. 패널은 `Time.timeScale = 0`으로 게임을 일시정지하고 카드를 표시한다. 플레이어 선택 시 UpgradeManager가 `PlayerShooter` / `PlayerStats` / `MagnetEffect`에 직접 효과를 적용하고 게임을 재개한다. 모든 컴포넌트는 씬 전용 싱글톤이며 DontDestroyOnLoad 없음.

**Tech Stack:** Unity 2022.3 LTS, C#, UnityEngine.UI (Text + Button), ScriptableObject, C# Action events, Time.timeScale

---

## File Map

| Action | Path | Responsibility |
|--------|------|----------------|
| Create | `Assets/Scripts/Upgrade/UpgradeType.cs` | enum 정의 (4종) |
| Create | `Assets/Scripts/Upgrade/UpgradeData.cs` | ScriptableObject 데이터 컨테이너 |
| Create | `Assets/Scripts/Upgrade/UpgradeManager.cs` | 씬 싱글톤, 가중치 선택, 효과 적용 |
| Create | `Assets/Scripts/UI/LevelUpPanel.cs` | UI 패널, 일시정지/재개 제어 |
| Modify | `Assets/Scripts/Player/PlayerShooter.cs` | `_bulletDamage` 추적, `IncreaseFireRate()`, `IncreaseDamage()` 추가, `Fire()` 수정 |
| Modify | `Assets/Scripts/Player/PlayerStats.cs` | `IncreaseMaxHp()` 추가 |
| Scene  | `Assets/Scenes/Game.unity` | UpgradeManager GameObject 추가 + LevelUpPanel UI 구성 |
| Assets | `Assets/ScriptableObjects/Upgrades/` | UpgradeData SO 4개 생성 |

---

## Task 1: UpgradeType enum

**Files:**
- Create: `Assets/Scripts/Upgrade/UpgradeType.cs`

- [ ] **Step 1: UpgradeType.cs 생성**

```csharp
// Attach to: (static enum — no GameObject attachment needed)
namespace ShooterGame.Upgrade
{
    public enum UpgradeType
    {
        AttackSpeed,
        Damage,
        Shield,
        Magnet
    }
}
```

`Assets/Scripts/Upgrade/` 디렉토리가 없으면 먼저 생성한다. Unity MCP `create_script` 툴 또는 파일 쓰기 툴로 생성한다.

- [ ] **Step 2: 컴파일 확인**

`read_console` 툴로 Error 필터 확인. 오류 없음이어야 한다.

- [ ] **Step 3: 커밋**

```
git add Assets/Scripts/Upgrade/UpgradeType.cs
git commit -m "feat(day4-part3): add UpgradeType enum"
```

---

## Task 2: UpgradeData ScriptableObject

**Files:**
- Create: `Assets/Scripts/Upgrade/UpgradeData.cs`

- [ ] **Step 1: UpgradeData.cs 생성**

```csharp
// Attach to: (ScriptableObject — no GameObject attachment needed)
using UnityEngine;

namespace ShooterGame.Upgrade
{
    [CreateAssetMenu(fileName = "UpgradeData", menuName = "ShooterGame/Upgrade Data")]
    public class UpgradeData : ScriptableObject
    {
        [SerializeField] private string      upgradeName = "업그레이드";
        [SerializeField] private UpgradeType type;
        [SerializeField] private float       value  = 1f;
        [SerializeField] private int         weight = 10;

        public string      Name   => upgradeName;
        public UpgradeType Type   => type;
        public float       Value  => value;
        public int         Weight => weight;
    }
}
```

- [ ] **Step 2: 컴파일 확인**

`read_console` 툴로 Error 필터 확인. `UpgradeType`이 같은 네임스페이스이므로 별도 using 없이 참조 가능.

- [ ] **Step 3: 커밋**

```
git add Assets/Scripts/Upgrade/UpgradeData.cs
git commit -m "feat(day4-part3): add UpgradeData ScriptableObject"
```

---

## Task 3: PlayerShooter 수정 — 데미지 추적 + 업그레이드 메서드

**Files:**
- Modify: `Assets/Scripts/Player/PlayerShooter.cs`

현재 `PlayerShooter.cs` 상태 (수정 전):
- `fireRate = 0.3f`, `_fireInterval`, `_fireTimer` 필드 존재
- `Fire()`는 `bullet.Initialize(BulletPool.Instance)` 까지만 호출 — `SetDamage` 없음
- `SetFireRate(float newRate)` 존재 — 유지
- `_bulletDamage` 필드 없음

`Bullet.SetDamage(int)` 은 이미 `Assets/Scripts/Player/Bullet.cs:55`에 존재하므로 추가 불필요.

- [ ] **Step 1: `_bulletDamage` 필드 추가 — `Awake()` 위에**

기존:
```csharp
private float _fireInterval;
private float _fireTimer;

private void Awake()
{
    _fireInterval = fireRate;
}
```

수정 후:
```csharp
private float _fireInterval;
private float _fireTimer;
private int   _bulletDamage = 10;  // matches Bullet prefab's default damage

private void Awake()
{
    _fireInterval = fireRate;
}
```

- [ ] **Step 2: `Fire()` 수정 — `bullet.SetDamage(_bulletDamage)` 추가**

기존:
```csharp
private void Fire()
{
    Bullet bullet = BulletPool.Instance.Get();
    if (bullet == null) return;

    // Place bullet at fire point
    bullet.transform.position = firePoint != null ? firePoint.position : transform.position;
    bullet.transform.rotation = Quaternion.identity;
    bullet.Initialize(BulletPool.Instance);
}
```

수정 후:
```csharp
private void Fire()
{
    Bullet bullet = BulletPool.Instance.Get();
    if (bullet == null) return;

    bullet.transform.position = firePoint != null ? firePoint.position : transform.position;
    bullet.transform.rotation = Quaternion.identity;
    bullet.Initialize(BulletPool.Instance);
    bullet.SetDamage(_bulletDamage);
}
```

- [ ] **Step 3: `IncreaseFireRate()` + `IncreaseDamage()` 추가 — `SetFireRate()` 아래에**

기존:
```csharp
/// <summary>Call from UpgradeManager to modify attack speed.</summary>
public void SetFireRate(float newRate)
{
    _fireInterval = Mathf.Max(0.05f, newRate); // floor to prevent zero-interval
}
```

수정 후:
```csharp
/// <summary>Call from UpgradeManager to modify attack speed.</summary>
public void SetFireRate(float newRate)
{
    _fireInterval = Mathf.Max(0.05f, newRate); // floor to prevent zero-interval
}

public void IncreaseFireRate(float amount)
{
    _fireInterval = Mathf.Max(0.05f, _fireInterval - amount);
}

public void IncreaseDamage(int amount)
{
    _bulletDamage += amount;
}
```

- [ ] **Step 4: 컴파일 확인**

`read_console` 툴로 Error 필터 확인.

- [ ] **Step 5: 커밋**

```
git add Assets/Scripts/Player/PlayerShooter.cs
git commit -m "feat(day4-part3): add damage tracking and upgrade methods to PlayerShooter"
```

---

## Task 4: PlayerStats 수정 — IncreaseMaxHp

**Files:**
- Modify: `Assets/Scripts/Player/PlayerStats.cs`

- [ ] **Step 1: `IncreaseMaxHp()` 추가 — `ResetHp()` 위에**

기존:
```csharp
private void ResetHp()
{
    CurrentHp    = maxHp;
    IsInvincible = false;
    OnHpChanged?.Invoke(CurrentHp, maxHp);
}
```

수정 후:
```csharp
public void IncreaseMaxHp(int amount)
{
    maxHp     += amount;
    CurrentHp  = Mathf.Min(CurrentHp + amount, maxHp);
    OnHpChanged?.Invoke(CurrentHp, maxHp);
}

private void ResetHp()
{
    CurrentHp    = maxHp;
    IsInvincible = false;
    OnHpChanged?.Invoke(CurrentHp, maxHp);
}
```

- [ ] **Step 2: 컴파일 확인**

`read_console` 툴로 Error 필터 확인.

- [ ] **Step 3: 커밋**

```
git add Assets/Scripts/Player/PlayerStats.cs
git commit -m "feat(day4-part3): add IncreaseMaxHp to PlayerStats"
```

---

## Task 5: LevelUpPanel

**Files:**
- Create: `Assets/Scripts/UI/LevelUpPanel.cs`

Unity 프로젝트가 `UnityEngine.UI.Text`를 사용한다. TextMeshPro를 사용하는 경우 `using TMPro;`로 교체하고 `Text`를 `TextMeshProUGUI`로 교체한다.

- [ ] **Step 1: LevelUpPanel.cs 생성**

```csharp
// Attach to: LevelUpPanel GameObject (Canvas child, default inactive)
using UnityEngine;
using UnityEngine.UI;
using ShooterGame.Upgrade;

namespace ShooterGame.UI
{
    public class LevelUpPanel : MonoBehaviour
    {
        [SerializeField] private Button[] cardButtons;  // exactly 3
        [SerializeField] private Text[]   nameTexts;    // exactly 3, one per button

        private UpgradeData[] _currentPicks;

        public void Show(UpgradeData[] picks)
        {
            _currentPicks = picks;
            for (int i = 0; i < cardButtons.Length; i++)
            {
                int index = i;  // closure capture — must be local variable
                nameTexts[i].text = picks[i].Name;
                cardButtons[i].onClick.RemoveAllListeners();
                cardButtons[i].onClick.AddListener(() => OnCardSelected(index));
            }
            gameObject.SetActive(true);
            Time.timeScale = 0f;
        }

        public void Hide()
        {
            Time.timeScale = 1f;
            gameObject.SetActive(false);
        }

        private void OnCardSelected(int index)
        {
            UpgradeManager.Instance?.ApplyUpgrade(_currentPicks[index]);
        }
    }
}
```

- [ ] **Step 2: 컴파일 확인**

`read_console` 툴로 Error 필터 확인. `UpgradeManager`가 아직 없으므로 CS0246 오류가 발생할 수 있다 — Task 6 완료 후 재확인.

- [ ] **Step 3: 커밋**

```
git add Assets/Scripts/UI/LevelUpPanel.cs
git commit -m "feat(day4-part3): add LevelUpPanel UI controller"
```

---

## Task 6: UpgradeManager

**Files:**
- Create: `Assets/Scripts/Upgrade/UpgradeManager.cs`

의존 관계: `ExpSystem` (Economy), `LevelUpPanel` (UI), `PlayerShooter`, `PlayerStats` (Player), `MagnetEffect` (Player). 모두 이미 존재한다.

- [ ] **Step 1: UpgradeManager.cs 생성**

```csharp
// Attach to: UpgradeManager GameObject (Game scene only — no DontDestroyOnLoad)
using System.Collections.Generic;
using UnityEngine;
using ShooterGame.Economy;
using ShooterGame.Player;
using ShooterGame.UI;

namespace ShooterGame.Upgrade
{
    public class UpgradeManager : MonoBehaviour
    {
        public static UpgradeManager Instance { get; private set; }

        [SerializeField] private List<UpgradeData> upgradePool;
        [SerializeField] private LevelUpPanel      levelUpPanel;
        [SerializeField] private PlayerShooter     playerShooter;
        [SerializeField] private PlayerStats       playerStats;

        private readonly List<UpgradeData> _eligible = new List<UpgradeData>(4);

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (ExpSystem.Instance != null)
                ExpSystem.Instance.OnLevelUp += HandleLevelUp;
        }

        private void HandleLevelUp(int newLevel)
        {
            UpgradeData[] picks = PickRandom(3);
            levelUpPanel.Show(picks);
        }

        private UpgradeData[] PickRandom(int count)
        {
            _eligible.Clear();
            _eligible.AddRange(upgradePool);

            UpgradeData[] result = new UpgradeData[count];
            for (int i = 0; i < count && _eligible.Count > 0; i++)
            {
                int totalWeight = 0;
                foreach (UpgradeData d in _eligible) totalWeight += d.Weight;

                int roll        = Random.Range(0, totalWeight);
                int accumulated = 0;
                for (int j = 0; j < _eligible.Count; j++)
                {
                    accumulated += _eligible[j].Weight;
                    if (roll < accumulated)
                    {
                        result[i] = _eligible[j];
                        _eligible.RemoveAt(j);
                        break;
                    }
                }
            }
            return result;
        }

        public void ApplyUpgrade(UpgradeData data)
        {
            switch (data.Type)
            {
                case UpgradeType.AttackSpeed:
                    playerShooter.IncreaseFireRate(data.Value);
                    break;
                case UpgradeType.Damage:
                    playerShooter.IncreaseDamage((int)data.Value);
                    break;
                case UpgradeType.Shield:
                    playerStats.IncreaseMaxHp((int)data.Value);
                    break;
                case UpgradeType.Magnet:
                    MagnetEffect.Instance?.IncreaseRadius(data.Value);
                    break;
            }
            levelUpPanel.Hide();
        }

        private void OnDestroy()
        {
            if (ExpSystem.Instance != null)
                ExpSystem.Instance.OnLevelUp -= HandleLevelUp;
            if (Instance == this) Instance = null;
        }
    }
}
```

- [ ] **Step 2: 컴파일 확인**

`read_console` 툴로 Error 필터 확인. Task 5의 LevelUpPanel.cs와 함께 오류가 해소되어야 한다.

- [ ] **Step 3: 커밋**

```
git add Assets/Scripts/Upgrade/UpgradeManager.cs
git commit -m "feat(day4-part3): add UpgradeManager with weighted random selection"
```

---

## Task 7: UpgradeData ScriptableObject 에셋 4개 생성

**Files:**
- Assets: `Assets/ScriptableObjects/Upgrades/` 폴더 내 4개 SO 에셋

Unity MCP `manage_scriptable_object` 툴 또는 `execute_menu_item`으로 SO를 생성한다. `Assets/ScriptableObjects/Upgrades/` 디렉토리가 없으면 먼저 `manage_asset` 툴로 생성한다.

- [ ] **Step 1: 폴더 확인/생성**

Unity MCP로 `Assets/ScriptableObjects/Upgrades/` 폴더 존재 여부 확인. 없으면 생성.

- [ ] **Step 2: AttackSpeed 에셋 생성**

`manage_scriptable_object` 툴로 생성:
- Path: `Assets/ScriptableObjects/Upgrades/UpgradeData_AttackSpeed.asset`
- Type: `ShooterGame.Upgrade.UpgradeData`
- Fields: `upgradeName = "공격속도 향상"`, `type = AttackSpeed(0)`, `value = 0.05`, `weight = 10`

- [ ] **Step 3: Damage 에셋 생성**

- Path: `Assets/ScriptableObjects/Upgrades/UpgradeData_Damage.asset`
- Fields: `upgradeName = "데미지 향상"`, `type = Damage(1)`, `value = 5`, `weight = 10`

- [ ] **Step 4: Shield 에셋 생성**

- Path: `Assets/ScriptableObjects/Upgrades/UpgradeData_Shield.asset`
- Fields: `upgradeName = "방어막"`, `type = Shield(2)`, `value = 1`, `weight = 7`

- [ ] **Step 5: Magnet 에셋 생성**

- Path: `Assets/ScriptableObjects/Upgrades/UpgradeData_Magnet.asset`
- Fields: `upgradeName = "자석 확장"`, `type = Magnet(3)`, `value = 1.0`, `weight = 5`

- [ ] **Step 6: 커밋**

```
git add Assets/ScriptableObjects/Upgrades/
git commit -m "feat(day4-part3): add 4 UpgradeData ScriptableObject assets"
```

---

## Task 8: 씬 설정 — UpgradeManager GameObject 추가 및 연결

**Files:**
- Scene: `Assets/Scenes/Game.unity`

- [ ] **Step 1: UpgradeManager GameObject 생성**

Unity MCP `manage_gameobject` 툴로 빈 GameObject 생성:
- Name: `UpgradeManager`
- 부모: 씬 루트

- [ ] **Step 2: UpgradeManager 컴포넌트 부착**

`manage_components` 툴로 `ShooterGame.Upgrade.UpgradeManager` 컴포넌트를 `UpgradeManager` GameObject에 추가.

- [ ] **Step 3: Inspector 연결**

`manage_components` 또는 `execute_code` 툴로 다음을 Inspector에 연결:
- `Upgrade Pool`: 4개 SO 에셋 (Task 7에서 생성한 4개)
- `Level Up Panel`: (Task 9에서 생성 — 해당 Task 이후 연결)
- `Player Shooter`: 씬 내 Player GameObject의 `PlayerShooter` 컴포넌트
- `Player Stats`: 씬 내 Player GameObject의 `PlayerStats` 컴포넌트

- [ ] **Step 4: 씬 저장**

`manage_scene` 툴로 씬 저장.

- [ ] **Step 5: 커밋**

```
git add Assets/Scenes/Game.unity
git commit -m "feat(day4-part3): add UpgradeManager to Game scene"
```

---

## Task 9: 씬 설정 — LevelUpPanel UI 구성 및 연결

**Files:**
- Scene: `Assets/Scenes/Game.unity`

LevelUpPanel은 Canvas 하위에 Panel → 3개 Button(각 Button 내부에 Text 자식) 구조.

- [ ] **Step 1: 씬 내 Canvas 확인**

`find_gameobjects` 툴로 "Canvas" 검색. 없으면 `manage_ui` 툴로 Canvas 생성.

- [ ] **Step 2: LevelUpPanel GameObject 생성**

`manage_ui` 또는 `manage_gameobject` 툴로:
- Name: `LevelUpPanel`
- 부모: Canvas
- 기본적으로 `SetActive(false)` — 시작 시 비활성

RectTransform: 전체 화면 Anchor (Stretch-Stretch), 반투명 배경 Image (optional, alpha 0.8 검은색).

- [ ] **Step 3: Button 3개 생성**

`manage_ui` 툴로 LevelUpPanel 하위에 Button 3개 생성:
- Name: `CardButton0`, `CardButton1`, `CardButton2`
- 가로 배치 (예: X = -300, 0, 300 / Y = 0)
- 각 Button 내부에 기본 Text 자식이 자동 생성됨 — Name을 `CardNameText0`, `CardNameText1`, `CardNameText2`로 변경

- [ ] **Step 4: LevelUpPanel 컴포넌트 부착**

`manage_components` 툴로 `ShooterGame.UI.LevelUpPanel` 컴포넌트를 `LevelUpPanel` GameObject에 추가.

- [ ] **Step 5: LevelUpPanel Inspector 연결**

`manage_components` 또는 `execute_code` 툴로:
- `Card Buttons[0,1,2]`: 3개 Button 컴포넌트
- `Name Texts[0,1,2]`: 3개 Text 컴포넌트

- [ ] **Step 6: UpgradeManager에 LevelUpPanel 연결**

Task 8 Step 3에서 건너뛴 `Level Up Panel` 필드에 이제 생성된 `LevelUpPanel` GameObject 연결.

- [ ] **Step 7: 씬 저장 및 커밋**

```
git add Assets/Scenes/Game.unity
git commit -m "feat(day4-part3): add LevelUpPanel UI to Game scene and wire references"
```

---

## Task 10: Play Mode 동작 검증

**Files:**
- None (Play Mode 테스트)

- [ ] **Step 1: Play Mode 진입**

`manage_editor` 툴로 Play Mode 진입.

- [ ] **Step 2: EXP 강제 부여로 레벨업 트리거**

`execute_code` 툴로 `ExpSystem.Instance.Add(999)` 실행 — 레벨업 발생해야 함.

- [ ] **Step 3: LevelUpPanel 표시 확인**

`find_gameobjects` 툴로 `LevelUpPanel` 검색 — `activeSelf == true` 이어야 한다.

`Time.timeScale == 0`인지 확인:
```csharp
Debug.Log("timeScale: " + Time.timeScale);
```

`read_console` 툴로 `timeScale: 0` 로그 확인.

- [ ] **Step 4: 카드 선택 시뮬레이션**

`execute_code` 툴로 첫 번째 카드 선택:
```csharp
LevelUpPanel panel = GameObject.FindObjectOfType<ShooterGame.UI.LevelUpPanel>();
// Simulate button click
UnityEngine.UI.Button[] buttons = panel.GetComponentsInChildren<UnityEngine.UI.Button>();
buttons[0].onClick.Invoke();
```

- [ ] **Step 5: 효과 적용 + 패널 숨김 확인**

`execute_code` 툴로 확인:
```csharp
// Check panel hidden
var panel = GameObject.Find("LevelUpPanel");
Debug.Log("Panel active: " + panel.activeSelf);
Debug.Log("timeScale: " + Time.timeScale);
```

`read_console` 툴로:
- `Panel active: False` 확인
- `timeScale: 1` 확인

- [ ] **Step 6: 런타임 오류 없음 확인**

`read_console` 툴로 Error 필터 확인 — 오류 없음이어야 한다.

- [ ] **Step 7: Play Mode 종료 및 최종 커밋**

```
git add Assets/Scenes/Game.unity
git commit -m "feat(day4-part3): complete level-up upgrade system - verified in Play Mode"
```

---

## 주요 주의사항

| 항목 | 내용 |
|------|------|
| `_bulletDamage` 초기값 | `Bullet` 프리팹의 `[SerializeField] damage = 10`과 일치해야 함. 다른 값이면 첫 업그레이드 전 데미지가 달라짐. |
| closure capture | `LevelUpPanel.Show()` 루프 내 `int index = i;` 로컬 변수 필수 — 없으면 모든 카드가 마지막 인덱스로 고정됨. |
| `Time.timeScale = 0` + 코루틴 | `WaitForSeconds`는 timeScale=0에서 멈춤. `WaitForSecondsRealtime` 필요 시 InvincibilityRoutine에 영향 없음 — 레벨업 패널 표시 중 피격 불가이므로 문제 없음. |
| 풀 크기 < count | `upgradePool`에 SO 4개 미만이면 `PickRandom(3)` 결과 배열에 null이 포함됨 — 반드시 4개 SO 등록. |
| UI Text vs TMPro | 프로젝트에 TextMeshPro 패키지가 있으면 `using TMPro;` + `TextMeshProUGUI`로 교체. |
