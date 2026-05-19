# Boss Laser Attack Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace `FinalBossEnemy`'s aimed burst-shot pattern with a two-phase laser attack — 1 second aim telegraph, then 1 second fixed-direction laser beam with one-time Raycast damage.

**Architecture:** Modify `FinalBossEnemy.cs` only. Remove burst-shot fields and methods; add `_laserLine` LineRenderer + laser fields; replace `AimedShootLoop`/`AimAndFireBurst` with `LaserAttackLoop`/`LaserAttack`. The FinalBoss prefab needs one new LineRenderer component assigned to `_laserLine`.

**Tech Stack:** Unity 2022.3 LTS, C#, LineRenderer, Physics2D.Raycast

---

## File Map

| File | Change |
|------|--------|
| `Assets/Scripts/Enemy/Patterns/FinalBossEnemy.cs` | Remove burst fields/methods; add laser fields; replace coroutines |
| FinalBoss prefab (Unity Editor) | Add second LineRenderer; assign `_laserLine`; set `_playerLayerMask` |

---

## Task 1: Remove Burst-Shot Fields and Methods

**Files:**
- Modify: `Assets/Scripts/Enemy/Patterns/FinalBossEnemy.cs`

Remove all burst-shot infrastructure that will be replaced by the laser.

- [ ] **Step 1: Remove the four burst-related `[SerializeField]` fields from the `[Header("Aimed Shot")]` block**

  In `FinalBossEnemy.cs`, locate the `[Header("Aimed Shot")]` block (lines ~47–54) and delete these four lines:

  ```csharp
  // DELETE these four lines:
  [SerializeField] private int   phase1AimedCount    = 3;
  [SerializeField] private float phase1BurstDelay    = 0.12f; // 연발 사이 딜레이
  [SerializeField] private int   phase2AimedCount    = 5;
  [SerializeField] private float phase2BurstDelay    = 0.08f;
  ```

  Keep `phase1AimedInterval`, `phase2AimedInterval`, `phase1AimDuration`, `phase2AimDuration`.

- [ ] **Step 2: Remove the two burst `WaitForSeconds` private fields**

  Find the private field block (~lines 86–92) and delete:

  ```csharp
  // DELETE these two lines:
  private WaitForSeconds _phase1BurstWait;
  private WaitForSeconds _phase2BurstWait;
  ```

- [ ] **Step 3: Remove burst WaitForSeconds initialization from `OnEnable()`**

  In `OnEnable()` (~line 113), delete:

  ```csharp
  // DELETE these two lines:
  _phase1BurstWait = new WaitForSeconds(phase1BurstDelay);
  _phase2BurstWait = new WaitForSeconds(phase2BurstDelay);
  ```

- [ ] **Step 4: Remove `AimAndFireBurst()` and `FireBulletAt()` methods**

  Delete the entire `AimAndFireBurst()` coroutine (~lines 320–379) and `FireBulletAt()` method (~lines 400–407). These are fully replaced by the new laser methods.

- [ ] **Step 5: Rename `AimedShootLoop` to `LaserAttackLoop`**

  Find `private IEnumerator AimedShootLoop()` and rename it. Also update the two call sites that reference it:

  - In `Move()`: `_aimedCoroutine = StartCoroutine(AimedShootLoop());` → `StartCoroutine(LaserAttackLoop());`
  - In `Phase2Transition()`: same replacement

  The body of `LaserAttackLoop` will still call `StartCoroutine(AimAndFireBurst())` at this point — change that call to `StartCoroutine(LaserAttack())` (the method does not exist yet; it will be added in Task 2).

  Final `LaserAttackLoop`:

  ```csharp
  private IEnumerator LaserAttackLoop()
  {
      while (true)
      {
          yield return _isPhase2 ? _phase2AimedWait : _phase1AimedWait;
          yield return StartCoroutine(LaserAttack());
      }
  }
  ```

- [ ] **Step 6: Verify the script compiles**

  Open Unity Editor. Wait for compilation (or check `Assets/Scripts/Enemy/Patterns/FinalBossEnemy.cs` in IDE for red errors). The only expected error at this point is `LaserAttack()` not found — that is fine; Task 2 adds it.

  If other compile errors appear, re-check steps 1–5 for any remaining references to removed fields.

---

## Task 2: Add Laser Fields and Infrastructure

**Files:**
- Modify: `Assets/Scripts/Enemy/Patterns/FinalBossEnemy.cs`

- [ ] **Step 1: Add the `[Header("Laser")]` SerializeField block**

  Add this block directly after the existing `[Header("Aim Line")]` block in the field declarations:

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

- [ ] **Step 2: Initialize `_laserLine` in `OnEnable()`**

  At the end of the `_aimLine` initialization block in `OnEnable()` (around line 114–122), add:

  ```csharp
  if (_laserLine != null)
  {
      _laserLine.positionCount = 2;
      _laserLine.useWorldSpace = true;
      _laserLine.enabled       = false;
  }
  ```

- [ ] **Step 3: Update `StopShootingCoroutines()` to also disable `_laserLine`**

  The current method ends with `if (_aimLine != null) _aimLine.enabled = false;`. Add one line after it:

  ```csharp
  private void StopShootingCoroutines()
  {
      if (_shootCoroutine != null) { StopCoroutine(_shootCoroutine); _shootCoroutine = null; }
      if (_aimedCoroutine != null) { StopCoroutine(_aimedCoroutine); _aimedCoroutine = null; }
      if (_aimLine   != null) _aimLine.enabled   = false;
      if (_laserLine != null) _laserLine.enabled = false;  // ADD THIS LINE
  }
  ```

- [ ] **Step 4: Verify compilation**

  Check Unity Editor / IDE — there should still be one error: `LaserAttack()` not defined. No other errors.

---

## Task 3: Implement `LaserAttack()` Coroutine

**Files:**
- Modify: `Assets/Scripts/Enemy/Patterns/FinalBossEnemy.cs`

- [ ] **Step 1: Add the `LaserAttack()` coroutine**

  Add the following method in the `// ── Shooting ──` region, after `LaserAttackLoop()`:

  ```csharp
  private IEnumerator LaserAttack()
  {
      float aimDuration = _isPhase2 ? phase2AimDuration : phase1AimDuration;

      // Cache player transform once
      if (_playerTransform == null)
      {
          GameObject p = GameObject.FindWithTag(Constants.TAG_PLAYER);
          if (p != null) _playerTransform = p.transform;
      }

      // ── Aim phase: flickering aim line tracks player ──────────
      if (_aimLine != null) _aimLine.enabled = true;
      for (float t = 0f; t < aimDuration; t += Time.deltaTime)
      {
          if (_aimLine != null && _playerTransform != null)
          {
              _aimLine.SetPosition(1, transform.InverseTransformPoint(_playerTransform.position));
              float alpha = (Mathf.Sin(t * _aimFlickerSpeed) + 1f) * 0.5f;
              Color c     = _aimLineColor;
              c.a = alpha;
              _aimLine.startColor = c;
              c.a = alpha * 0.2f;
              _aimLine.endColor = c;
          }
          yield return null;
      }
      if (_aimLine != null) _aimLine.enabled = false;

      if (_playerTransform == null) yield break;

      // ── Lock direction ────────────────────────────────────────
      Vector2 dir = (_playerTransform.position - transform.position).normalized;

      // ── Fire phase: wide laser beam, origin follows boss ──────
      if (_laserLine != null)
      {
          _laserLine.startWidth = _laserWidth;
          _laserLine.endWidth   = _laserWidth;
          _laserLine.startColor = _laserColor;
          _laserLine.endColor   = _laserColor;
          _laserLine.enabled    = true;
      }

      AudioManager.Instance?.PlaySFX(SfxType.EnemyShoot);

      bool     damageDealt = false;
      float    elapsed     = 0f;

      while (elapsed < _laserDuration)
      {
          // Update beam origin each frame so it follows the sweeping boss
          if (_laserLine != null)
          {
              _laserLine.SetPosition(0, transform.position);
              _laserLine.SetPosition(1, (Vector2)transform.position + dir * _laserLength);
          }

          // Raycast damage — applied once on the first frame
          if (!damageDealt)
          {
              RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, _laserLength, _playerLayerMask);
              if (hit.collider != null)
              {
                  PlayerStats stats = hit.collider.GetComponent<PlayerStats>();
                  stats?.TakeDamage(_laserDamage);
              }
              damageDealt = true;
          }

          elapsed += Time.deltaTime;
          yield return null;
      }

      if (_laserLine != null) _laserLine.enabled = false;
  }
  ```

  Note: `PlayerStats` is in namespace `ShooterGame.Player`. The current `FinalBossEnemy.cs` does **not** have this using directive — add it at the top of the file alongside the other usings:

  ```csharp
  using ShooterGame.Player; // ADD THIS — required for PlayerStats.TakeDamage
  ```

- [ ] **Step 2: Verify the script compiles with zero errors**

  Check Unity Editor console — no compile errors expected. The `LaserAttack()` method resolves the last outstanding error.

- [ ] **Step 3: Commit**

  ```bash
  git add Assets/Scripts/Enemy/Patterns/FinalBossEnemy.cs
  git commit -m "feat: replace boss aimed burst with laser attack pattern"
  ```

---

## Task 4: Prefab Setup in Unity Editor

**This task is done manually in the Unity Editor — no code changes.**

- [ ] **Step 1: Open the FinalBoss prefab**

  In the Project window, navigate to `Assets/Prefabs/Enemies/` and double-click the FinalBoss prefab to open it in Prefab Edit Mode.

- [ ] **Step 2: Add a child GameObject for the laser LineRenderer**

  In the Hierarchy (inside prefab edit mode), right-click the FinalBoss root → **Create Empty** → rename it `LaserLine`.

- [ ] **Step 3: Add a LineRenderer component to `LaserLine`**

  With `LaserLine` selected → Inspector → **Add Component** → search `LineRenderer` → add it.

  Set these values on the new LineRenderer:
  - **Positions** Size: 2 (will be set at runtime, leave as 0,0,0)
  - **Use World Space**: ✅ checked
  - **Width** Curve: set a flat constant (e.g., 0.25)
  - **Material**: use the same glow/additive material as `_aimLine` if one exists, or **Default-Line** material as fallback

- [ ] **Step 4: Assign `_laserLine` in the Inspector**

  Select the FinalBoss root GameObject. In the Inspector, find the **Laser** header section of `FinalBossEnemy`. Drag the `LaserLine` child into the `_laserLine` field.

- [ ] **Step 5: Assign `_playerLayerMask`**

  In the same Inspector section, click the **_playerLayerMask** field dropdown and select the **Player** layer only.

- [ ] **Step 6: Save the prefab**

  Press **Ctrl+S** or click **Save** in the top-left of Prefab Edit Mode. Exit prefab edit mode.

---

## Task 5: Play Mode Verification

- [ ] **Step 1: Enter Play Mode and reach the FinalBoss**

  Start a run in Game scene. Wait for the FinalBoss to spawn (or temporarily lower the boss spawn timer in `DifficultyManager` via Inspector during Play Mode).

- [ ] **Step 2: Verify the aim phase**

  When the boss enters its laser attack cycle:
  - A thin red `_aimLine` should appear, flickering and tracking your player position for ~1 second (Phase 1) or ~0.6 second (Phase 2).
  - No bullets should fire during this phase.

- [ ] **Step 3: Verify the laser beam**

  After the aim phase ends:
  - The thin aim line disappears.
  - A wide orange beam (`_laserLine`) appears from the boss in the locked direction.
  - The beam origin follows the boss as it sweeps left/right.
  - The beam persists for 1 second, then disappears.

- [ ] **Step 4: Verify damage**

  Stand in the laser's path during the firing phase:
  - Player loses 1 life on the first frame of contact.
  - No repeated damage during the same laser shot.

- [ ] **Step 5: Verify no damage when dodging**

  Move the player out of the aimed direction before the laser fires:
  - No damage should be dealt (Raycast misses).

- [ ] **Step 6: Verify spread bullets still fire**

  Confirm the spread bullet `ShootLoop` is still active and fires normally between laser attacks.

- [ ] **Step 7: Verify Phase 2 transition**

  Bring the boss to Phase 2 transition:
  - Laser intervals and aim duration change per Phase 2 settings.
  - No ghost `_laserLine` left visible after transition (StopShootingCoroutines cleans it up).

- [ ] **Step 8: Commit final verification**

  ```bash
  git add Assets/Scenes/Game.unity  # only if scene was saved
  git commit -m "feat: boss laser attack verified in play mode"
  ```

---

## Reference: Final State of Modified Fields

After all tasks, `FinalBossEnemy.cs` should have these headers in order:

```
[Header("Movement")]
[Header("Phase 1")]
[Header("Phase 2")]
[Header("Phase 2 Flash")]
[Header("Phase 2 Shake")]
[Header("Death")]
[Header("Shared")]
[Header("Aimed Shot")]   ← keeps only interval + aimDuration fields
[Header("Aim Line")]     ← unchanged
[Header("Laser")]        ← NEW
[Header("Audio")]
[Header("VFX")]
```

`[Header("Aimed Shot")]` retains only:
```csharp
[SerializeField] private float phase1AimedInterval = 3.0f;
[SerializeField] private float phase1AimDuration   = 1.0f;
[SerializeField] private float phase2AimedInterval = 2.0f;
[SerializeField] private float phase2AimDuration   = 0.6f;
```
