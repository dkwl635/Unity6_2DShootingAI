# Boss Laser Attack — Design Spec

**Date:** 2026-05-19
**Target:** `FinalBossEnemy.cs`

---

## Summary

Replace the existing aimed burst-shot pattern (`AimAndFireBurst`) in `FinalBossEnemy` with a two-phase laser attack:
1. **Aim phase (1s):** Thin, flickering `_aimLine` tracks the player
2. **Fire phase (1s):** Direction locks, wide `_laserLine` beam fires, Raycast deals one-time damage

---

## Scope

### Removed

| Item | Reason |
|------|--------|
| `AimAndFireBurst()` coroutine | Replaced by `LaserAttack()` |
| `FireBulletAt()` method | No longer needed |
| `phase1AimedCount`, `phase2AimedCount` | Burst count no longer relevant |
| `phase1BurstDelay`, `phase2BurstDelay` | No burst firing |
| `_phase1BurstWait`, `_phase2BurstWait` | Cached WaitForSeconds for burst |

### Kept

- `phase1AimedInterval`, `phase2AimedInterval` — laser cooldown cycle
- `phase1AimDuration`, `phase2AimDuration` — aim telegraph duration (default 1s)
- `_aimLine`, `_aimLineColor`, `_aimLineWidth`, `_aimFlickerSpeed` — aim phase visual

### Added

```csharp
[Header("Laser")]
[SerializeField] private LineRenderer _laserLine;
[SerializeField] private Color        _laserColor      = new Color(1f, 0.6f, 0.1f, 1f);
[SerializeField] private float        _laserWidth      = 0.25f;
[SerializeField] private float        _laserDuration   = 1f;
[SerializeField] private int          _laserDamage     = 3;
[SerializeField] private float        _laserLength     = 20f;
[SerializeField] private LayerMask    _playerLayerMask;
```

---

## Attack Flow

```
LaserAttackLoop (replaces AimedShootLoop)
  └─ while(true)
       ├─ yield _phase1AimedWait / _phase2AimedWait
       └─ LaserAttack()

LaserAttack()
  [1] Cache _playerTransform (FindWithTag once if null)
  [2] Enable _aimLine, track player + flicker for aimDuration
  [3] Early-exit if player not found
  [4] Lock direction: dir = (playerPos - bossPos).normalized
  [5] Disable _aimLine
  [6] Configure _laserLine:
        useWorldSpace = true
        startWidth = endWidth = _laserWidth
        color = _laserColor
        position[0] = transform.position (world)
        position[1] = transform.position + dir * _laserLength
  [7] Enable _laserLine
  [8] Physics2D.Raycast(bossPos, dir, _laserLength, _playerLayerMask)
        → hit? → PlayerStats.TakeDamage(_laserDamage)
  [9] yield _laserWait (WaitForSeconds(_laserDuration), cached)
  [10] Disable _laserLine
```

---

## Visual Specification

| Element | `_aimLine` (aim phase) | `_laserLine` (fire phase) |
|---------|----------------------|--------------------------|
| useWorldSpace | false (local) | true (world) |
| startWidth | `_aimLineWidth` (0.04f) | `_laserWidth` (0.25f) |
| Color | Red, flickering alpha | Orange/white, fully opaque |
| Duration | `aimDuration` (1s) | `_laserDuration` (1s) |
| Tracks player | Yes | No (fixed at lock direction) |

---

## Phase Behaviour

| Phase | Aim Duration | Laser Damage |
|-------|-------------|--------------|
| Phase 1 | `phase1AimDuration` (1.0s) | `_laserDamage` |
| Phase 2 | `phase2AimDuration` (0.6s) | `_laserDamage` |

---

## Prefab Changes Required

1. Add a second `LineRenderer` component (or child GameObject with LineRenderer) to the FinalBoss prefab
2. Assign to `_laserLine` field in Inspector
3. Assign `_playerLayerMask` to the Player layer in Inspector

---

## Unchanged Systems

- `ShootLoop` / `FireSpread` — spread bullet pattern unchanged
- Phase 2 transition, death animation, HP events — all unchanged
- `_aimLine` setup in `OnEnable()` — unchanged
- `StopShootingCoroutines()` — will also disable `_laserLine`
