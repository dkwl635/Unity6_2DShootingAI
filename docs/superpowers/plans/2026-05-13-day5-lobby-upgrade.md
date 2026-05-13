# Day 5: Lobby Permanent Upgrade System — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 로비에서 코인으로 영구 업그레이드를 구매하고, 게임 시작 시 자동으로 플레이어에 적용하는 시스템 구축

**Architecture:** `LobbyUpgradeData` ScriptableObject로 데이터 정의 → `LobbyUpgradeManager`(Lobby 씬 전용)가 구매 로직 담당 → `SaveManager`에 레벨 저장 → `InGameManager`가 Game 씬 진입 시 SaveManager에서 레벨 읽어 플레이어에 적용

**Tech Stack:** Unity 2022.3 LTS, C#, ScriptableObject, PlayerPrefs, UnityEngine.UI (Legacy)

---

## File Map

| Action | File | Role |
|--------|------|------|
| Create | `Assets/Scripts/Meta/LobbyUpgradeType.cs` | enum — 4가지 업그레이드 타입 |
| Create | `Assets/Scripts/Meta/LobbyUpgradeData.cs` | ScriptableObject — 업그레이드 정의 |
| Create | `Assets/Scripts/Meta/LobbyUpgradeManager.cs` | Lobby 씬 전용 Manager — 구매 로직 |
| Create | `Assets/Scripts/UI/LobbyUpgradeSlot.cs` | 슬롯 1개 렌더링 + 버튼 |
| Create | `Assets/Scripts/UI/LobbyUpgradePanel.cs` | 패널 전체 — 슬롯 초기화/갱신 |
| Create | `Assets/Scripts/Core/LobbyController.cs` | Play 버튼 → Game 씬 로드 |
| Modify | `Assets/Scripts/Utils/Constants.cs` | PlayerPrefs 키 추가 |
| Modify | `Assets/Scripts/Meta/SaveManager.cs` | SpendCoins, GetUpgradeLevel, SetUpgradeLevel 추가 |
| Modify | `Assets/Scripts/Player/PlayerStats.cs` | ApplyPermanentHpBonus 추가 |
| Modify | `Assets/Scripts/Player/PlayerShooter.cs` | ApplyPermanentDamageBonus, ApplyPermanentAtkSpeedBonus 추가 |
| Modify | `Assets/Scripts/Player/MagnetEffect.cs` | ApplyPermanentMagnetBonus 추가 |
| Modify | `Assets/Scripts/Core/InGameManager.cs` | ApplyPermanentBonuses() 추가 |

---

## Task 1: LobbyUpgradeType enum + Constants 키 추가

**Files:**
- Create: `Assets/Scripts/Meta/LobbyUpgradeType.cs`
- Modify: `Assets/Scripts/Utils/Constants.cs`

- [ ] **Step 1: LobbyUpgradeType.cs 생성**

```csharp
// Attach to: (enum — no GameObject attachment needed)
namespace ShooterGame.Meta
{
    public enum LobbyUpgradeType
    {
        MaxHp        = 0,
        Damage       = 1,
        AttackSpeed  = 2,
        MagnetRange  = 3
    }
}
```

- [ ] **Step 2: Constants.cs에 PlayerPrefs 키 추가**

`Assets/Scripts/Utils/Constants.cs`의 `// ── PlayerPrefs Keys` 섹션에 추가:

```csharp
        // ── PlayerPrefs Keys ────────────────────────────────────────
        public const string PREF_BEST_SCORE      = "BestScore";
        public const string PREF_TOTAL_COINS     = "TotalCoins";
        public const string PREF_LOBBY_UPGRADE   = "LobbyUpgrade_"; // append (int)LobbyUpgradeType
```

- [ ] **Step 3: Unity가 컴파일 완료될 때까지 대기**

Console에서 에러 없음 확인 후 진행.

- [ ] **Step 4: 커밋**

```bash
git add Assets/Scripts/Meta/LobbyUpgradeType.cs Assets/Scripts/Meta/LobbyUpgradeType.cs.meta Assets/Scripts/Utils/Constants.cs
git commit -m "feat: add LobbyUpgradeType enum and PlayerPrefs key constant"
```

---

## Task 2: LobbyUpgradeData ScriptableObject

**Files:**
- Create: `Assets/Scripts/Meta/LobbyUpgradeData.cs`

- [ ] **Step 1: LobbyUpgradeData.cs 생성**

```csharp
// Attach to: (ScriptableObject — no GameObject attachment needed)
using UnityEngine;

namespace ShooterGame.Meta
{
    [CreateAssetMenu(fileName = "LobbyUpgrade", menuName = "ShooterGame/Lobby Upgrade Data")]
    public class LobbyUpgradeData : ScriptableObject
    {
        [SerializeField] private LobbyUpgradeType _upgradeType;
        [SerializeField] private string           _displayName  = "업그레이드";
        [SerializeField] private int              _maxLevel     = 5;
        [SerializeField] private int              _baseCost     = 100;
        [SerializeField] private float            _costMultiplier = 2f;
        [SerializeField] private float            _gainPerLevel = 1f;

        public LobbyUpgradeType UpgradeType    => _upgradeType;
        public string           DisplayName    => _displayName;
        public int              MaxLevel       => _maxLevel;
        public float            GainPerLevel   => _gainPerLevel;

        /// <summary>currentLevel은 현재 레벨 (0 = 아무것도 안 산 상태).</summary>
        public int GetCostForLevel(int currentLevel)
            => Mathf.RoundToInt(_baseCost * Mathf.Pow(_costMultiplier, currentLevel));

        /// <summary>level 레벨 구매 후 누적 스탯 증가량.</summary>
        public float GetTotalGain(int level)
            => _gainPerLevel * level;
    }
}
```

- [ ] **Step 2: Unity 컴파일 에러 없음 확인**

- [ ] **Step 3: 커밋**

```bash
git add Assets/Scripts/Meta/LobbyUpgradeData.cs Assets/Scripts/Meta/LobbyUpgradeData.cs.meta
git commit -m "feat: add LobbyUpgradeData ScriptableObject"
```

---

## Task 3: SaveManager 확장

**Files:**
- Modify: `Assets/Scripts/Meta/SaveManager.cs`

- [ ] **Step 1: SaveManager.cs 전체 교체**

```csharp
// Attach to: SaveManager GameObject (DontDestroyOnLoad)
// Responsibility: All persistent data read/write only (PlayerPrefs → JSON migration later).
// Does NOT contain game logic — only data access.

using UnityEngine;
using ShooterGame.Utils;

namespace ShooterGame.Meta
{
    public class SaveManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────
        public static SaveManager Instance { get; private set; }

        // ── Cached Data ──────────────────────────────────────────
        public int BestScore  { get; private set; }
        public int TotalCoins { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAll();
        }

        // ── Public API ───────────────────────────────────────────

        public void TrySaveBestScore(int score)
        {
            if (score <= BestScore) return;
            BestScore = score;
            PlayerPrefs.SetInt(Constants.PREF_BEST_SCORE, BestScore);
            PlayerPrefs.Save();
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            TotalCoins += amount;
            PlayerPrefs.SetInt(Constants.PREF_TOTAL_COINS, TotalCoins);
            PlayerPrefs.Save();
        }

        /// <summary>코인 차감 — 잔액이 부족하면 0으로 클램프.</summary>
        public void SpendCoins(int cost)
        {
            if (cost <= 0) return;
            TotalCoins = Mathf.Max(0, TotalCoins - cost);
            PlayerPrefs.SetInt(Constants.PREF_TOTAL_COINS, TotalCoins);
            PlayerPrefs.Save();
        }

        public int GetUpgradeLevel(LobbyUpgradeType type)
            => PlayerPrefs.GetInt(Constants.PREF_LOBBY_UPGRADE + (int)type, 0);

        public void SetUpgradeLevel(LobbyUpgradeType type, int level)
        {
            PlayerPrefs.SetInt(Constants.PREF_LOBBY_UPGRADE + (int)type, level);
            PlayerPrefs.Save();
        }

        public void ForceSave()
        {
            PlayerPrefs.SetInt(Constants.PREF_BEST_SCORE,  BestScore);
            PlayerPrefs.SetInt(Constants.PREF_TOTAL_COINS, TotalCoins);
            PlayerPrefs.Save();
        }

        // ── Private ──────────────────────────────────────────────

        private void LoadAll()
        {
            BestScore  = PlayerPrefs.GetInt(Constants.PREF_BEST_SCORE,  0);
            TotalCoins = PlayerPrefs.GetInt(Constants.PREF_TOTAL_COINS, 0);
            // upgrade levels are read on-demand via GetUpgradeLevel()
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
```

- [ ] **Step 2: Unity 컴파일 에러 없음 확인**

- [ ] **Step 3: 커밋**

```bash
git add Assets/Scripts/Meta/SaveManager.cs
git commit -m "feat: add SpendCoins, GetUpgradeLevel, SetUpgradeLevel to SaveManager"
```

---

## Task 4: 플레이어 컴포넌트에 영구 보너스 메서드 추가

**Files:**
- Modify: `Assets/Scripts/Player/PlayerStats.cs`
- Modify: `Assets/Scripts/Player/PlayerShooter.cs`
- Modify: `Assets/Scripts/Player/MagnetEffect.cs`

- [ ] **Step 1: PlayerStats.cs — ApplyPermanentHpBonus 추가**

`IncreaseMaxHp()` 메서드 다음에 추가:

```csharp
        /// <summary>게임 시작 시 InGameManager가 한 번 호출. totalGain = gainPerLevel * level.</summary>
        public void ApplyPermanentHpBonus(int totalGain)
        {
            if (totalGain <= 0) return;
            IncreaseMaxHp(totalGain);
        }
```

- [ ] **Step 2: PlayerShooter.cs — ApplyPermanentDamageBonus, ApplyPermanentAtkSpeedBonus 추가**

`IncreaseDamage()` 메서드 다음에 추가:

```csharp
        /// <summary>게임 시작 시 InGameManager가 한 번 호출. totalGain = gainPerLevel * level.</summary>
        public void ApplyPermanentDamageBonus(int totalGain)
        {
            if (totalGain <= 0) return;
            IncreaseDamage(totalGain);
        }

        /// <summary>totalReduction = gainPerLevel * level (초 단위 감소량).</summary>
        public void ApplyPermanentAtkSpeedBonus(float totalReduction)
        {
            if (totalReduction <= 0f) return;
            IncreaseFireRate(totalReduction);
        }
```

- [ ] **Step 3: MagnetEffect.cs — ApplyPermanentMagnetBonus 추가**

`IncreaseRadius()` 메서드 다음에 추가:

```csharp
        /// <summary>게임 시작 시 InGameManager가 한 번 호출. totalGain = gainPerLevel * level.</summary>
        public void ApplyPermanentMagnetBonus(float totalGain)
        {
            if (totalGain <= 0f) return;
            IncreaseRadius(totalGain);
        }
```

- [ ] **Step 4: Unity 컴파일 에러 없음 확인**

- [ ] **Step 5: 커밋**

```bash
git add Assets/Scripts/Player/PlayerStats.cs Assets/Scripts/Player/PlayerShooter.cs Assets/Scripts/Player/MagnetEffect.cs
git commit -m "feat: add ApplyPermanentBonus methods to player components"
```

---

## Task 5: LobbyUpgradeManager

**Files:**
- Create: `Assets/Scripts/Meta/LobbyUpgradeManager.cs`

- [ ] **Step 1: LobbyUpgradeManager.cs 생성**

```csharp
// Attach to: LobbyUpgradeManager GameObject (Lobby scene only — no DontDestroyOnLoad)
using System;
using UnityEngine;

namespace ShooterGame.Meta
{
    public class LobbyUpgradeManager : MonoBehaviour
    {
        public static LobbyUpgradeManager Instance { get; private set; }

        // 배열 인덱스 = (int)LobbyUpgradeType 순서와 일치해야 함
        [SerializeField] private LobbyUpgradeData[] _upgrades;

        public event Action OnUpgradeChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public LobbyUpgradeData GetData(LobbyUpgradeType type) => _upgrades[(int)type];

        public int GetCurrentLevel(LobbyUpgradeType type)
            => SaveManager.Instance.GetUpgradeLevel(type);

        /// <summary>
        /// 구매 시도. 성공하면 true 반환 + OnUpgradeChanged 발행.
        /// </summary>
        public bool TryPurchase(LobbyUpgradeType type)
        {
            LobbyUpgradeData data  = _upgrades[(int)type];
            int currentLevel       = GetCurrentLevel(type);

            if (currentLevel >= data.MaxLevel) return false;

            int cost = data.GetCostForLevel(currentLevel);
            if (SaveManager.Instance.TotalCoins < cost) return false;

            SaveManager.Instance.SpendCoins(cost);
            SaveManager.Instance.SetUpgradeLevel(type, currentLevel + 1);
            OnUpgradeChanged?.Invoke();
            return true;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
```

- [ ] **Step 2: Unity 컴파일 에러 없음 확인**

- [ ] **Step 3: 커밋**

```bash
git add Assets/Scripts/Meta/LobbyUpgradeManager.cs Assets/Scripts/Meta/LobbyUpgradeManager.cs.meta
git commit -m "feat: add LobbyUpgradeManager with TryPurchase logic"
```

---

## Task 6: LobbyUpgradeSlot UI

**Files:**
- Create: `Assets/Scripts/UI/LobbyUpgradeSlot.cs`

- [ ] **Step 1: LobbyUpgradeSlot.cs 생성**

```csharp
// Attach to: 각 UpgradeSlot child GameObject
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using ShooterGame.Meta;

namespace ShooterGame.UI
{
    public class LobbyUpgradeSlot : MonoBehaviour
    {
        [SerializeField] private Text   _nameText;
        [SerializeField] private Text   _levelText;
        [SerializeField] private Text   _costText;
        [SerializeField] private Button _buyButton;
        [SerializeField] private Text   _buyButtonText;

        private LobbyUpgradeType _type;
        private readonly StringBuilder _sb = new StringBuilder(16);

        public void Initialize(LobbyUpgradeType type)
        {
            _type = type;
            _buyButton.onClick.AddListener(OnBuyClicked);
        }

        public void Render(LobbyUpgradeData data, int currentLevel, int totalCoins)
        {
            _nameText.text = data.DisplayName;
            _levelText.text = BuildLevelDots(currentLevel, data.MaxLevel);

            bool isMax = currentLevel >= data.MaxLevel;
            if (isMax)
            {
                _costText.text         = "";
                _buyButtonText.text    = "MAX";
                _buyButton.interactable = false;
            }
            else
            {
                int cost               = data.GetCostForLevel(currentLevel);
                _costText.text         = cost + " 코인";
                _buyButtonText.text    = "구매";
                _buyButton.interactable = totalCoins >= cost;
            }
        }

        private string BuildLevelDots(int current, int max)
        {
            _sb.Clear();
            for (int i = 0; i < max; i++)
                _sb.Append(i < current ? "●" : "○");
            return _sb.ToString();
        }

        private void OnBuyClicked()
        {
            LobbyUpgradeManager.Instance?.TryPurchase(_type);
        }

        private void OnDestroy()
        {
            if (_buyButton != null)
                _buyButton.onClick.RemoveListener(OnBuyClicked);
        }
    }
}
```

- [ ] **Step 2: Unity 컴파일 에러 없음 확인**

- [ ] **Step 3: 커밋**

```bash
git add Assets/Scripts/UI/LobbyUpgradeSlot.cs Assets/Scripts/UI/LobbyUpgradeSlot.cs.meta
git commit -m "feat: add LobbyUpgradeSlot UI component"
```

---

## Task 7: LobbyUpgradePanel UI

**Files:**
- Create: `Assets/Scripts/UI/LobbyUpgradePanel.cs`

- [ ] **Step 1: LobbyUpgradePanel.cs 생성**

```csharp
// Attach to: LobbyUpgradePanel GameObject
using UnityEngine;
using UnityEngine.UI;
using ShooterGame.Meta;

namespace ShooterGame.UI
{
    public class LobbyUpgradePanel : MonoBehaviour
    {
        [SerializeField] private Text               _coinText;
        // 슬롯 배열: 인덱스 0=MaxHp, 1=Damage, 2=AttackSpeed, 3=MagnetRange
        [SerializeField] private LobbyUpgradeSlot[] _slots;

        private LobbyUpgradeData[] _datas;

        private void Start()
        {
            _datas = new LobbyUpgradeData[4];
            for (int i = 0; i < 4; i++)
            {
                var type = (LobbyUpgradeType)i;
                _datas[i] = LobbyUpgradeManager.Instance.GetData(type);
                _slots[i].Initialize(type);
            }

            LobbyUpgradeManager.Instance.OnUpgradeChanged += RefreshAll;
            RefreshAll();
        }

        private void RefreshAll()
        {
            int coins        = SaveManager.Instance.TotalCoins;
            _coinText.text   = "보유 코인: " + coins;

            for (int i = 0; i < 4; i++)
            {
                int level = SaveManager.Instance.GetUpgradeLevel((LobbyUpgradeType)i);
                _slots[i].Render(_datas[i], level, coins);
            }
        }

        private void OnDestroy()
        {
            if (LobbyUpgradeManager.Instance != null)
                LobbyUpgradeManager.Instance.OnUpgradeChanged -= RefreshAll;
        }
    }
}
```

- [ ] **Step 2: Unity 컴파일 에러 없음 확인**

- [ ] **Step 3: 커밋**

```bash
git add Assets/Scripts/UI/LobbyUpgradePanel.cs Assets/Scripts/UI/LobbyUpgradePanel.cs.meta
git commit -m "feat: add LobbyUpgradePanel UI component"
```

---

## Task 8: LobbyController (Play 버튼)

**Files:**
- Create: `Assets/Scripts/Core/LobbyController.cs`

- [ ] **Step 1: LobbyController.cs 생성**

```csharp
// Attach to: LobbyController GameObject (Lobby scene)
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShooterGame.Core
{
    public class LobbyController : MonoBehaviour
    {
        public void OnPlayButtonClicked()
        {
            SceneManager.LoadScene("Game");
        }
    }
}
```

- [ ] **Step 2: Unity 컴파일 에러 없음 확인**

- [ ] **Step 3: 커밋**

```bash
git add Assets/Scripts/Core/LobbyController.cs Assets/Scripts/Core/LobbyController.cs.meta
git commit -m "feat: add LobbyController for scene transition"
```

---

## Task 9: InGameManager — ApplyPermanentBonuses

**Files:**
- Modify: `Assets/Scripts/Core/InGameManager.cs`

- [ ] **Step 1: InGameManager.cs 전체 교체**

```csharp
// Attach to: InGameManager GameObject (Game scene only — no DontDestroyOnLoad)
// Responsibility: In-game session state only — running flag, elapsed time, game-over trigger.

using System;
using UnityEngine;
using ShooterGame.Meta;
using ShooterGame.Player;

namespace ShooterGame.Core
{
    public class InGameManager : MonoBehaviour
    {
        // ── Singleton (scene-scoped) ──────────────────────────────
        public static InGameManager Instance { get; private set; }

        // ── Events ───────────────────────────────────────────────
        public event Action OnGameStart;
        public event Action OnGameOver;

        // ── State ────────────────────────────────────────────────
        public bool  IsGameRunning { get; private set; }
        public float ElapsedTime   { get; private set; }

        // ── Permanent Bonus References ───────────────────────────
        [SerializeField] private PlayerStats    _playerStats;
        [SerializeField] private PlayerShooter  _playerShooter;
        [SerializeField] private MagnetEffect   _magnetEffect;
        // 배열 인덱스 = (int)LobbyUpgradeType 순서와 일치
        [SerializeField] private LobbyUpgradeData[] _lobbyUpgrades;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            // NOTE: intentionally no DontDestroyOnLoad
        }

        private void Start()
        {
            ApplyPermanentBonuses();
            StartGame();
        }

        private void Update()
        {
            if (!IsGameRunning) return;
            ElapsedTime += Time.deltaTime;
        }

        // ── Public API ───────────────────────────────────────────

        public void StartGame()
        {
            ElapsedTime   = 0f;
            IsGameRunning = true;
            OnGameStart?.Invoke();
        }

        public void TriggerGameOver()
        {
            if (!IsGameRunning) return;

            IsGameRunning = false;
            OnGameOver?.Invoke();

            int finalScore = ScoreManager.Instance != null ? ScoreManager.Instance.Score : 0;
            SaveManager.Instance?.TrySaveBestScore(finalScore);
        }

        // ── Private ──────────────────────────────────────────────

        private void ApplyPermanentBonuses()
        {
            if (SaveManager.Instance == null || _lobbyUpgrades == null) return;

            int hpLevel     = SaveManager.Instance.GetUpgradeLevel(LobbyUpgradeType.MaxHp);
            int dmgLevel    = SaveManager.Instance.GetUpgradeLevel(LobbyUpgradeType.Damage);
            int atkLevel    = SaveManager.Instance.GetUpgradeLevel(LobbyUpgradeType.AttackSpeed);
            int magnetLevel = SaveManager.Instance.GetUpgradeLevel(LobbyUpgradeType.MagnetRange);

            if (hpLevel > 0 && _playerStats != null)
                _playerStats.ApplyPermanentHpBonus(
                    Mathf.RoundToInt(_lobbyUpgrades[(int)LobbyUpgradeType.MaxHp].GetTotalGain(hpLevel)));

            if (dmgLevel > 0 && _playerShooter != null)
                _playerShooter.ApplyPermanentDamageBonus(
                    Mathf.RoundToInt(_lobbyUpgrades[(int)LobbyUpgradeType.Damage].GetTotalGain(dmgLevel)));

            if (atkLevel > 0 && _playerShooter != null)
                _playerShooter.ApplyPermanentAtkSpeedBonus(
                    _lobbyUpgrades[(int)LobbyUpgradeType.AttackSpeed].GetTotalGain(atkLevel));

            if (magnetLevel > 0 && _magnetEffect != null)
                _magnetEffect.ApplyPermanentMagnetBonus(
                    _lobbyUpgrades[(int)LobbyUpgradeType.MagnetRange].GetTotalGain(magnetLevel));
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
```

- [ ] **Step 2: Unity 컴파일 에러 없음 확인**

- [ ] **Step 3: 커밋**

```bash
git add Assets/Scripts/Core/InGameManager.cs
git commit -m "feat: apply permanent lobby bonuses to player at game start"
```

---

## Task 10: ScriptableObject 에셋 4개 생성

**Unity Editor 작업 — 코드 없음**

- [ ] **Step 1: `Assets/ScriptableObjects/LobbyUpgrades/` 폴더 생성**

Project 창에서 `Assets/ScriptableObjects/` 우클릭 → Create → Folder → 이름: `LobbyUpgrades`

- [ ] **Step 2: 4개 SO 에셋 생성**

`Assets/ScriptableObjects/LobbyUpgrades/` 우클릭 → Create → ShooterGame → Lobby Upgrade Data

아래 값으로 4개 생성:

| 파일명 | upgradeType | displayName | baseCost | costMultiplier | gainPerLevel |
|--------|------------|-------------|----------|----------------|--------------|
| `LobbyUpgrade_MaxHp` | MaxHp | 최대 체력 | 100 | 2 | 1 |
| `LobbyUpgrade_Damage` | Damage | 공격력 | 100 | 2 | 2 |
| `LobbyUpgrade_AttackSpeed` | AttackSpeed | 공격 속도 | 150 | 2 | 0.03 |
| `LobbyUpgrade_MagnetRange` | MagnetRange | 마그넷 범위 | 100 | 2 | 0.5 |

- [ ] **Step 3: 커밋**

```bash
git add Assets/ScriptableObjects/LobbyUpgrades/
git commit -m "feat: add 4 LobbyUpgradeData ScriptableObject assets"
```

---

## Task 11: Lobby 씬 UI 구성 + 연결

**Unity Editor 작업**

- [ ] **Step 1: Lobby.unity 열기**

File → Open Scene → `Assets/Scenes/Lobby.unity`

- [ ] **Step 2: LobbyUpgradeManager GameObject 추가**

Hierarchy에서 빈 오브젝트 생성 → 이름: `LobbyUpgradeManager`
- `LobbyUpgradeManager` 컴포넌트 추가
- `_upgrades` 배열 크기 4로 설정
- 인덱스 순서대로 SO 할당: [0]=MaxHp, [1]=Damage, [2]=AttackSpeed, [3]=MagnetRange

- [ ] **Step 3: Canvas 및 UI 계층 구성**

Hierarchy에서 우클릭 → UI → Canvas (없으면 생성)

Canvas 하위에 다음 구조 생성:
```
Canvas
├── LobbyUpgradePanel  (빈 오브젝트 → LobbyUpgradePanel 컴포넌트 추가)
│   ├── TitleText      (UI → Text, 텍스트: "영구 업그레이드")
│   ├── CoinText       (UI → Text, 텍스트: "보유 코인: 0")
│   ├── Slot_MaxHp     (빈 오브젝트 → LobbyUpgradeSlot 컴포넌트 추가)
│   │   ├── NameText   (UI → Text)
│   │   ├── LevelText  (UI → Text)
│   │   ├── CostText   (UI → Text)
│   │   └── BuyButton  (UI → Button)
│   │       └── Text   (자동 생성 — BuyButtonText)
│   ├── Slot_Damage    (위와 동일 구조)
│   ├── Slot_AttackSpeed (위와 동일 구조)
│   └── Slot_MagnetRange (위와 동일 구조)
└── PlayButton         (UI → Button, 텍스트: "PLAY")
```

- [ ] **Step 4: 각 슬롯 Inspector 연결**

Slot_MaxHp ~ Slot_MagnetRange 각각에 대해:
- `_nameText` → 해당 NameText
- `_levelText` → 해당 LevelText
- `_costText` → 해당 CostText
- `_buyButton` → 해당 BuyButton
- `_buyButtonText` → BuyButton/Text

- [ ] **Step 5: LobbyUpgradePanel Inspector 연결**

- `_coinText` → CoinText
- `_slots` 배열 크기 4: [0]=Slot_MaxHp, [1]=Slot_Damage, [2]=Slot_AttackSpeed, [3]=Slot_MagnetRange

- [ ] **Step 6: LobbyController 추가 + PlayButton 연결**

- 빈 오브젝트 생성 → 이름: `LobbyController` → LobbyController 컴포넌트 추가
- PlayButton의 `OnClick()` → LobbyController 오브젝트 드래그 → `OnPlayButtonClicked()` 선택

- [ ] **Step 7: 씬 저장**

Ctrl+S

- [ ] **Step 8: 커밋**

```bash
git add Assets/Scenes/Lobby.unity
git commit -m "feat: set up Lobby scene with upgrade panel UI"
```

---

## Task 12: InGameManager에 SO 레퍼런스 연결

**Unity Editor 작업**

- [ ] **Step 1: Game.unity 열기**

File → Open Scene → `Assets/Scenes/Game.unity`

- [ ] **Step 2: InGameManager GameObject 선택**

Hierarchy에서 `InGameManager` 선택

- [ ] **Step 3: Inspector 연결**

- `_playerStats` → Player 오브젝트의 PlayerStats
- `_playerShooter` → Player 오브젝트의 PlayerShooter
- `_magnetEffect` → Player 오브젝트의 MagnetEffect
- `_lobbyUpgrades` 배열 크기 4: [0]=LobbyUpgrade_MaxHp, [1]=LobbyUpgrade_Damage, [2]=LobbyUpgrade_AttackSpeed, [3]=LobbyUpgrade_MagnetRange

- [ ] **Step 4: 씬 저장 + 커밋**

```bash
git add Assets/Scenes/Game.unity
git commit -m "feat: wire InGameManager lobby upgrade references in Game scene"
```

---

## Task 13: 최종 검증

- [ ] **Step 1: SaveManager 테스트 (디버그용)**

PlayerPrefs를 초기화하고 테스트:
- Unity Editor → Edit → Clear All PlayerPrefs
- Game 씬 Play → 코인 없는 상태에서 구매 시도 → 버튼 비활성 확인

- [ ] **Step 2: Lobby → Game 흐름 테스트**

1. Lobby 씬 Play
2. (테스트용) 코인 수동 추가: `SaveManager.Instance.AddCoins(1000)` — Console에서 `SaveManager.Instance.AddCoins(1000)` 직접 호출 또는 DebugStatPanel 활용
3. 업그레이드 구매 → 레벨 도트 변화 확인
4. PLAY 버튼 → Game 씬 진입
5. 게임 시작 시 PlayerStats/PlayerShooter/MagnetEffect에 보너스 반영 확인 (DebugStatPanel 참조)

- [ ] **Step 3: MAX 레벨 테스트**

업그레이드 5레벨 도달 → "MAX" 표시 + 버튼 비활성 확인

- [ ] **Step 4: 씬 재진입 테스트**

Game 씬 → GameOver → Lobby 씬 복귀 → 레벨 유지 확인 (PlayerPrefs 영속성)

- [ ] **Step 5: 최종 커밋**

```bash
git add -A
git commit -m "feat(day5): complete lobby permanent upgrade system"
```
