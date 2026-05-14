# Stage System Design
**Date:** 2026-05-14  
**Project:** Infinite Roguelike Shooter (Unity 2022.3 LTS)

---

## 1. Overview

Convert the current infinite-loop pattern system into a stage-based format.  
Each stage lasts 4 minutes, contains a mid-stage mini-boss (2 min) and a stage-ending final boss (4 min).  
After the final boss is defeated, a "Stage Clear" popup is shown and the next stage begins with increased difficulty.  
The loop is currently unlimited (Stage 1 → 2 → 1 → 2 …); a future update will add Stage 3 and a true game-clear ending.

---

## 2. Stage Flow

```
Stage N
  0:00 ~ 4:00  Regular enemy patterns cycle continuously (existing PatternManager logic)
  2:00         Mini-boss spawns alongside running patterns (no pattern interruption)
               └─ Mini-boss dies → nothing special, patterns keep going
  4:00         ALL patterns stop, EnemySpawner stops
               └─ Final boss spawns (only enemy on screen)
               └─ Final boss dies → "Stage N Clear!" popup (3 s) → Stage N+1 begins
               └─ If player dies before 4:00 → Game Over (existing flow)
               └─ If player dies during boss → Game Over
```

After Stage 2, loop back to Stage 1 with difficulty multiplier incremented.

---

## 3. Architecture

### 3.1 Responsibility Split

| Class | Responsibility | DontDestroyOnLoad |
|---|---|---|
| `StageManager` (new) | Stage timer, boss spawning, stage transition events | No (Game scene only) |
| `PatternManager` (modified) | Regular enemy patterns only — no boss logic | No |
| `EnemySpawner` (modified) | Add pause/resume API | No |
| `DifficultyManager` (modified) | Remove boss timer; add per-stage difficulty scaling | No |
| `StageClearUI` (new) | "Stage N Clear!" popup display | No |
| `FinalBossEnemy` (new) | Final boss AI with 2-phase attack | No |
| `FinalBossPattern` (new) | PatternBase wrapper for final boss | No |

### 3.2 Event Flow

```
StageManager (t = 120 s)
  → Instantiate MiniBossPattern directly (bypasses PatternManager's _activePattern slot)
  → Subscribe to MiniBossPattern.OnPatternComplete → no-op (just cleanup)

StageManager (t = 240 s) fires OnFinalBossPhaseStart
  → _activeMiniBoss?.ForceComplete()  — force-kill mini-boss if still alive
  → PatternManager.PausePatterns()    — ForceComplete active pattern, stop timer
  → EnemySpawner.PauseSpawning()     — stop coroutine
  → StageManager instantiates FinalBossPattern
  → Subscribe to FinalBossPattern.OnPatternComplete → HandleBossDefeated()

StageManager.HandleBossDefeated() — starts coroutine StageTransition()
  StageTransition():
    1. StageClearUI.ShowStageClear(stageNum)   ← popup fades in
    2. yield 3 s
    3. StageClearUI hides
    4. _currentStage = (_currentStage % totalStages) + 1
       if wrapped → _loopCount++
    5. DifficultyManager.SetStage(_currentStage, _loopCount)
    6. _stageTimer = 0f, _miniBossSpawned = false, _inFinalBossPhase = false
    7. PatternManager.ResumePatterns()
    8. EnemySpawner.ResumeSpawning()
```

---

## 4. New Files

### 4.1 `StageManager.cs`
**Attach to:** StageManager GameObject (Game scene only)  
**Namespace:** `ShooterGame.Core`

```
Fields (SerializeField):
  float miniBossTime       = 120f   // seconds into stage when mini-boss spawns
  float finalBossTime      = 240f   // seconds into stage when final boss spawns
  MiniBossPattern  miniBossPrefab
  FinalBossPattern finalBossPrefab
  int totalStages          = 2      // expandable to 3 later

State:
  int   _currentStage      (1-based)
  int   _loopCount         (how many full loops completed)
  float _stageTimer
  bool  _miniBossSpawned
  bool  _inFinalBossPhase
  PatternBase _activeMiniBoss
  PatternBase _activeFinalBoss

Events (C# Action):
  OnFinalBossPhaseStart    — EnemySpawner and PatternManager subscribe
  OnStageComplete(int)     — StageClearUI subscribes

Lifecycle:
  Start  → subscribe to InGameManager.OnGameStart / OnGameOver
  Update → tick _stageTimer when IsGameRunning
           at miniBossTime  → SpawnMiniBoss()
           at finalBossTime → StartFinalBossPhase()
  OnDestroy → unsubscribe all
```

### 4.2 `FinalBossEnemy.cs`
**Attach to:** FinalBoss prefab root  
**Namespace:** `ShooterGame.Enemy`  
**Extends:** `EnemyBase`

```
Phase 1 (HP 100% → 50%)
  Movement: side-to-side sweep (same as MiniBossEnemy)
  Attack:   5-way spread shot every 1.5 s

Phase 2 (HP < 50%)
  Triggered once on HP crossing threshold
  Movement speed ×1.5
  Attack:   7-way spread shot every 1.0 s
  Visual:   sprite color tint to red (flash effect on transition)

SerializeField:
  float phase2HpThreshold  = 0.5f
  int   phase1BulletCount  = 5
  int   phase2BulletCount  = 7
  float phase1FireInterval  = 1.5f
  float phase2FireInterval  = 1.0f
  float spreadAngle         = 20f
  int   bulletDamage        = 8
  float bossCenterY         = 5f
  float sweepFrequency      = 1.0f
  float sweepAmplitude      = 3f
```

### 4.3 `FinalBossPattern.cs`
**Attach to:** FinalBossPattern prefab  
**Namespace:** `ShooterGame.Enemy`  
**Extends:** `PatternBase`  

Mirrors `MiniBossPattern` structure: spawns `FinalBossEnemy`, waits for its death, fires `OnPatternComplete`.

### 4.4 `StageClearUI.cs`
**Attach to:** StageClearPanel (child of HUD Canvas)  
**Namespace:** `ShooterGame.UI`

```
SerializeField:
  TextMeshProUGUI stageLabel    // "Stage N Clear!"
  float           displayTime = 3f
  CanvasGroup     canvasGroup

API:
  ShowStageClear(int stageNum)  — sets text, fades in, waits displayTime, fades out
```

---

## 5. Modified Files

### 5.1 `DifficultyManager.cs`
- **Remove:** `bossInterval` field, `_bossTimer` field, `OnMiniBossSpawn` event, boss-timer Update logic
- **Add:** `public void SetStage(int stage, int loopCount)` — adjusts internal difficulty multiplier
  - Stage 1, Loop 0: base multipliers (×1.0)
  - Stage 2: +0.3 to speed/HP multiplier base
  - Each additional loop: +0.2 further on top

### 5.2 `PatternManager.cs`
- **Remove:** `miniBossConfig`, `miniBossPrefab` fields (moved to StageManager), `SpawnMiniBoss()` method, `OnMiniBossSpawn` subscription
- **Add:** `public void PausePatterns()` — calls `_activePattern?.ForceComplete()`, sets `_running = false`
- **Add:** `public void ResumePatterns()` — sets `_running = true`, resets `_patternTimer`
- StageManager subscribes to `InGameManager.OnGameStart/OnGameOver` for resume/pause instead

### 5.3 `EnemySpawner.cs`
- **Add:** `public void PauseSpawning()` — same as `StopSpawning()` but doesn't unsubscribe events
- **Add:** `public void ResumeSpawning()` — same as `StartSpawning()`
- These are distinct from `StopSpawning()`/`StartSpawning()` which are tied to game-over lifecycle

---

## 6. Scene Changes

- Add **StageManager** GameObject to Game scene
- Add **StageClearPanel** child under existing HUD Canvas (initially inactive)
- Add **FinalBossPattern** prefab under `Assets/Prefabs/Enemies/`
- Wire StageManager Inspector fields: miniBossPrefab, finalBossPrefab

---

## 7. Out of Scope (Future)

- Stage 3 and game-clear ending
- Per-stage background / music changes
- Stage-specific regular enemy variants
- HP recovery between stages

---

## 8. Success Criteria

- [ ] At exactly 2:00, mini-boss spawns without interrupting running pattern
- [ ] Mini-boss death has no stage effect; patterns continue
- [ ] At exactly 4:00, all patterns and spawning stop; only final boss on screen
- [ ] Final boss enters phase 2 at 50% HP (visual + attack change)
- [ ] Final boss death shows "Stage N Clear!" popup for 3 seconds
- [ ] After popup, stage increments, difficulty increases, patterns resume
- [ ] Player death at any point triggers normal Game Over flow
- [ ] No `Instantiate` in Update loops; all boss objects cleaned up on stage transition
- [ ] All event listeners unsubscribed in `OnDestroy`
