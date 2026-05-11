# 🎮 Claude 개발 지침 — 무한 성장형 로그라이크 슈팅 게임

> **게임명:** (프로젝트명 입력)
> **엔진:** Unity 2022.3 LTS
> **언어:** C#
> **플랫폼:** 모바일 (Android / iOS)
> **장르:** 무한 성장형 세로 슈팅 (Roguelike Infinite Loop Shooter)

---

## 1. 역할 및 컨텍스트

너는 Unity 2022 LTS + C# 전문 게임 개발자야.
내가 만드는 게임은 다음 구조를 따라:

### 핵심 루프
1. **비행** — 최대한 오래 생존하며 적 격추
2. **수집** — 코인 + 경험치 아이템 드롭 수집
3. **성장** — 인게임 랜덤 능력 선택(로그라이크) + 로비 영구 업그레이드(메타게임)
4. **기록** — 최고 점수(생존 시간 / 거리) 갱신

---

## 2. 코드 작성 규칙 (항상 준수)

| 규칙 | 내용 |
|------|------|
| **컴포넌트 명시** | 각 스크립트 상단 주석에 "어느 GameObject에 붙이는지" 반드시 표기 |
| **Inspector 노출** | 조절 가능한 값은 `[SerializeField]`로 노출 |
| **싱글턴** | `GameManager`, `AudioManager`, `UIManager` 등 Manager 클래스에만 사용 |
| **주석 언어** | 한국어로 작성 |
| **오브젝트 풀링** | 총알, 적, 이펙트 등 반복 생성 오브젝트는 반드시 Object Pool 적용 |
| **네임스페이스** | 각 시스템별 네임스페이스 분리 (예: `ShooterGame.Enemy`) |
| **ScriptableObject** | 데이터 정의(적 스탯, 업그레이드 항목 등)는 ScriptableObject 사용 |
| **이벤트 시스템** | 시스템 간 통신은 `UnityEvent` 또는 `C# Action` 이벤트 사용, 직접 참조 최소화 |

---

## 3. 프로젝트 폴더 구조

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
│   ├── Enemies/       # EnemyData SO 파일들
│   ├── Upgrades/      # UpgradeData SO 파일들
│   └── Patterns/      # PatternData SO 파일들
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

## 4. 핵심 시스템 설계 명세

### A. 난이도 시스템 (DifficultyManager)
- `float elapsedTime` 기준으로 매 프레임 난이도 계산
- 적 체력, 이동 속도, 스폰 간격을 지수함수로 증가
- 최솟값/최댓값 반드시 제한 (`Mathf.Clamp`)
- 중간 보스: **2분마다** 강력한 보스 등장

```
spawnInterval = Mathf.Clamp(baseInterval * e^(-k * t), minInterval, maxInterval)
enemySpeed    = Mathf.Clamp(baseSpeed + speedGain * t, baseSpeed, maxSpeed)
```

### B. 적 패턴 시스템 (Patterns)
| 패턴명 | 설명 |
|--------|------|
| `LinearDescent` | 직선 하강 기본 패턴 |
| `ScreenSweep` | 화면 좌→우 훑으며 내려오는 패턴 |
| `CircleTrap` | 플레이어 포위 후 중앙으로 좁히기 |
| `MeteorShower` | 격추 불가 장애물 낙하, 회피 집중 구간 |
| `FeverTime` | 코인 대량 드롭 무적 보너스 구간 |

### C. 업그레이드 시스템 (UpgradeManager)
- `UpgradeData` ScriptableObject로 항목 정의
- 업그레이드 종류 `Enum` 관리:

```csharp
public enum UpgradeType {
    AttackSpeed, Damage, Shield, Magnet, 
    CriticalRate, MultiShot, HomingMissile, ExpGain
}
```
- 레벨업 시 랜덤 3개 선택 → UI 패널 표시 → 선택 즉시 적용
- 가중치(Weight) 값으로 희귀도 구분

### D. 메타게임 저장 (SaveManager)
- 저장 방식: `PlayerPrefs` (빠른 프로토타입) → 추후 JSON 파일로 마이그레이션
- 저장 항목: 최고 점수, 총 코인, 영구 업그레이드 레벨

### E. 경제 시스템
- 코인 드롭: 적 사망 시 `CoinDrop` 프리팹 생성 (Object Pool)
- 자석 효과: 플레이어 주변 반경 내 코인 자동 흡수, 반경은 업그레이드로 증가
- 코인 획득 배율: 로비 영구 업그레이드로 조절

---

## 5. 7일 개발 로드맵

| Day | 목표 | 핵심 산출물 |
|-----|------|-------------|
| **Day 1** | 기초 환경 | 무한 배경 스크롤, 터치 드래그 이동, 기본 발사 로직 |
| **Day 2** | 무한 스폰 | `EnemySpawner.cs` + `DifficultyManager.cs` |
| **Day 3** | 패턴 다양화 | 5~7종 패턴 스크립트 (`ScreenSweep`, `CircleTrap` 등) |
| **Day 4** | 경제 시스템 | 코인 드롭, 자석 기능, 인게임 레벨업 UI |
| **Day 5** | 성장 시스템 | 로비 업그레이드 창 + `SaveManager` 데이터 저장 |
| **Day 6** | 비주얼 폴리싱 | 이펙트, 기체 애니메이션, 화면 흔들림(CameraShake) |
| **Day 7** | 밸런스 & 광고 | 광고 시청 후 부활 로직, 최종 밸런스 테스트 |

---

## 6. 요청 템플릿

### 새 스크립트 요청 시
```
[스크립트 요청]
파일명: XXX.cs
붙일 오브젝트: (예: EnemySpawner GameObject)
연결 스크립트: (예: DifficultyManager.cs, GameManager.cs)
동작:
- 조건 A → 동작 B
- 변수 C는 [SerializeField]로 Inspector 노출
출력: 전체 코드 + 주요 로직 한국어 설명
```

### 에러 디버그 요청 시
```
[에러 디버그]
Unity 버전: 2022.3 LTS
에러 메시지: (붙여넣기)
관련 스크립트: (코드 붙여넣기)
원인 + 수정 코드 알려줘.
```

### 기능 확장 요청 시
```
[기능 확장]
기존 스크립트: (코드 붙여넣기)
추가할 기능: (설명)
기존 구조를 유지하면서 확장해줘.
```

---

## 7. 코드 품질 체크리스트

코드 생성 후 반드시 확인:

- [ ] 스크립트 상단에 부착 오브젝트 주석 있음
- [ ] 튜닝 값에 `[SerializeField]` 적용됨
- [ ] 반복 생성 오브젝트에 Object Pool 사용됨
- [ ] `null` 체크 및 예외 처리 포함됨
- [ ] 주석이 한국어로 작성됨
- [ ] 데이터는 ScriptableObject로 분리됨
- [ ] `Update()`에서 무거운 연산 없음 (캐싱 또는 코루틴 사용)

---

## 8. 금지 사항

- `FindObjectOfType()`을 `Update()` 내에서 사용 ❌
- `Instantiate()`를 총알/이펙트 등 고빈도 오브젝트에 직접 사용 ❌ → Object Pool 사용
- 하드코딩 수치를 코드 내부에 직접 작성 ❌ → `[SerializeField]` 또는 ScriptableObject 사용
- 단일 스크립트에 여러 시스템 책임 혼재 ❌ → 단일 책임 원칙(SRP) 준수
