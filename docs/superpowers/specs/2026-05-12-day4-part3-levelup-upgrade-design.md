# Day 4 Part 3 — Level-Up Upgrade System Design

**Date:** 2026-05-12  
**Scope:** UpgradeType, UpgradeData(SO), UpgradeManager, LevelUpPanel + PlayerShooter/PlayerStats 수정  
**Depends on:** ExpSystem.OnLevelUp (Part 1), MagnetEffect.IncreaseRadius (Part 2)  
**Upgrade types:** AttackSpeed, Damage, Shield, Magnet (4종)

---

## 1. Overview

레벨업 시 `ExpSystem.OnLevelUp` 이벤트가 발행되면, `UpgradeManager`가 가중치 기반으로 4종 중 3종을 무작위 선택해 `LevelUpPanel`에 전달한다. 패널은 `Time.timeScale = 0`으로 게임을 일시정지하고 카드 3장을 표시한다. 플레이어가 선택하면 `UpgradeManager`가 효과를 적용하고 게임을 재개한다. 업그레이드는 누적 적용된다.

---

## 2. Files Created / Modified

| File | Location | Action |
|------|----------|--------|
| `UpgradeType.cs` | `Assets/Scripts/Upgrade/` | 신규 — enum 정의 |
| `UpgradeData.cs` | `Assets/Scripts/Upgrade/` | 신규 — ScriptableObject |
| `UpgradeManager.cs` | `Assets/Scripts/Upgrade/` | 신규 — 씬 싱글톤, 선택·적용 |
| `LevelUpPanel.cs` | `Assets/Scripts/UI/` | 신규 — UI 패널, 일시정지/재개 |
| `PlayerShooter.cs` | `Assets/Scripts/Player/` | 수정 — `_bulletDamage` 추적, `IncreaseDamage()`, `IncreaseFireRate()` |
| `PlayerStats.cs` | `Assets/Scripts/Player/` | 수정 — `IncreaseMaxHp()` |

---

## 3. Data Flow

```
ExpSystem.OnLevelUp(newLevel)
  └─ UpgradeManager (구독)
        └─ 가중치 기반 랜덤 3종 선택 (4종 중 중복 없이)
              └─ LevelUpPanel.Show(UpgradeData[3])
                    └─ Time.timeScale = 0
                    └─ 버튼 3개에 이름·수치 바인딩
                          └─ 플레이어 카드 선택
                                └─ UpgradeManager.ApplyUpgrade(data)
                                      ├─ AttackSpeed → PlayerShooter.IncreaseFireRate(data.Value)
                                      ├─ Damage      → PlayerShooter.IncreaseDamage((int)data.Value)
                                      ├─ Shield      → PlayerStats.IncreaseMaxHp((int)data.Value)
                                      └─ Magnet      → MagnetEffect.IncreaseRadius(data.Value)
                                └─ LevelUpPanel.Hide()
                                      └─ Time.timeScale = 1
```

---

## 4. UpgradeType enum

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

---

## 5. UpgradeData ScriptableObject

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

**기본 ScriptableObject 4개 생성 예시:**

| Name | Type | Value | Weight |
|------|------|-------|--------|
| 공격속도 향상 | AttackSpeed | 0.05f | 10 |
| 데미지 향상 | Damage | 5 | 10 |
| 방어막 | Shield | 1 | 7 |
| 자석 확장 | Magnet | 1.0f | 5 |

---

## 6. UpgradeManager

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
            // Weighted random selection without replacement
            _eligible.Clear();
            _eligible.AddRange(upgradePool);

            UpgradeData[] result = new UpgradeData[count];
            for (int i = 0; i < count && _eligible.Count > 0; i++)
            {
                int totalWeight = 0;
                foreach (UpgradeData d in _eligible) totalWeight += d.Weight;

                int roll = Random.Range(0, totalWeight);
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

---

## 7. LevelUpPanel

```csharp
// Attach to: LevelUpPanel GameObject (Game scene only)
using UnityEngine;
using UnityEngine.UI;
using ShooterGame.Upgrade;

namespace ShooterGame.UI
{
    public class LevelUpPanel : MonoBehaviour
    {
        [SerializeField] private Button[] cardButtons;   // 3개 버튼
        [SerializeField] private Text[]   nameTexts;     // 각 카드 이름 텍스트

        private UpgradeData[] _currentPicks;

        public void Show(UpgradeData[] picks)
        {
            _currentPicks = picks;
            for (int i = 0; i < cardButtons.Length; i++)
            {
                int index = i;  // closure capture
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

---

## 8. PlayerShooter 수정

기존 `SetFireRate(float newRate)` 유지. 추가 사항:

```csharp
// 추가 필드 (Awake에서 fireRate로 초기화)
private int _bulletDamage = 10;  // Bullet 기본 데미지와 동일

// Fire() 수정 — 매 발사마다 현재 데미지 적용
private void Fire()
{
    Bullet bullet = BulletPool.Instance.Get();
    if (bullet == null) return;
    bullet.transform.position = firePoint != null ? firePoint.position : transform.position;
    bullet.transform.rotation = Quaternion.identity;
    bullet.Initialize(BulletPool.Instance);
    bullet.SetDamage(_bulletDamage);  // 추가
}

// 추가 public 메서드
public void IncreaseFireRate(float amount)
{
    _fireInterval = Mathf.Max(0.05f, _fireInterval - amount);
}

public void IncreaseDamage(int amount)
{
    _bulletDamage += amount;
}
```

---

## 9. PlayerStats 수정

```csharp
// 추가 public 메서드
public void IncreaseMaxHp(int amount)
{
    maxHp    += amount;
    CurrentHp = Mathf.Min(CurrentHp + amount, maxHp);
    OnHpChanged?.Invoke(CurrentHp, maxHp);
}
```

---

## 10. Scene & Prefab 요구사항

| 항목 | 내용 |
|------|------|
| `UpgradeManager` GameObject | 씬에 추가, Inspector에서 Pool(4개 SO), LevelUpPanel, PlayerShooter, PlayerStats 연결 |
| `LevelUpPanel` UI GameObject | Canvas 하위, 기본적으로 비활성(SetActive false), Button 3개 + Text 3개 |
| `UpgradeData` SO 4개 | `Assets/ScriptableObjects/Upgrades/` 폴더에 생성 |

---

## 11. Out of Scope (Part 3)

- CriticalRate, MultiShot, HomingMissile, ExpGain → Day 5+
- 업그레이드 설명 텍스트 (추가 UI) → Day 6 Visual Polish
- 영구 업그레이드 (Lobby) → Day 5 SaveManager 연동
