# Animation-Synced Parry Telegraph Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Move Cinderella's ring telegraph timing from an unscaled-duration coroutine to the Animator's `ParryTelegraphProgress` curve while preserving Animation Event ownership of the parry window.

**Architecture:** `RingDrawer` will sample the assigned `Animator` in `LateUpdate()` and render a monotonic ring radius. `CanParryEvent(true)` remains the only authority that opens the parry window and will snap the visual to the apex while firing the existing one-shot feedback. `BossPatternBase` will expose a time-free telegraph contract and stop active ring signals on every cancellation/reset lifecycle path; Cinderella will prepare the ring without timing values, while RougeHood will retain its own LaserSight timer internally.

**Tech Stack:** Unity C#, `Animator.GetFloat`, Unity Animation Events, existing `EventBus<T>`, `dotnet restore/build` with `Assembly-CSharp.csproj`.

## Global Constraints

- Keep `PlayerParry.cs`, `BossAnimationKeyReceiver.cs`, and `Struct_Boss.cs` unchanged.
- Preserve serialized Cinderella telegraph fields in the first migration, but do not use them for ring timing.
- Preserve existing `BossController`, BT, prefab-facing names, Animation Event names, and LaserSight timing behavior.
- Modify only `RingDrawer.cs`, `BossPatternBase.cs`, `Cinderella_Patterns.cs`, and `Legacy/RougeHood_Patterns.cs`, plus this plan document.
- Do not add dependencies, stage files, commit, reset, or discard unrelated user changes.
- Treat `EnableParry()` as the gameplay authority; a curve reaching `1` alone may render the apex but must not open parry.

---

### Task 1: Establish the failing API contract and baseline

**Files:**
- Test: no gameplay test harness exists for these boss scripts; use the explicit PowerShell contract check below.
- Inspect: `Assets/03_Code/03_Boss/Telegraph/RingDrawer.cs`, `Assets/03_Code/03_Boss/FSM/BossPatternBase.cs`, `Assets/03_Code/03_Boss/FSM/Cinderella_Patterns.cs`, `Assets/03_Code/03_Boss/Legacy/RougeHood_Patterns.cs`.

**Interfaces:**
- The desired contract is `BeginSignal(Animator)`, `StopSignal()`, and `PlayTelegraph(TelegraphType)`.

- [x] **Step 1: Run the baseline project compilation**

Run:

```powershell
dotnet restore Assembly-CSharp.csproj
dotnet build Assembly-CSharp.csproj
```

Expected: the current checkout compiles or reports only pre-existing diagnostics; record any baseline errors before changing code.

- [x] **Step 2: Run the failing desired-contract check**

Run:

```powershell
$ring = Get-Content -Raw -Encoding UTF8 'Assets/03_Code/03_Boss/Telegraph/RingDrawer.cs'
$base = Get-Content -Raw -Encoding UTF8 'Assets/03_Code/03_Boss/FSM/BossPatternBase.cs'
if ($ring -notmatch 'public\s+void\s+BeginSignal\(Animator\s+sourceAnimator\)') { throw 'RED: RingDrawer.BeginSignal(Animator) is missing.' }
if ($ring -notmatch 'public\s+void\s+StopSignal\(\)') { throw 'RED: RingDrawer.StopSignal() is missing.' }
if ($base -notmatch 'PlayTelegraph\(TelegraphType\s+type\)') { throw 'RED: time-free PlayTelegraph(TelegraphType) is missing.' }
```

Expected: FAIL because the old timer-based API is still present. This is the test-first contract failure for the refactor.

---

### Task 2: Convert RingDrawer to Animator-driven rendering

**Files:**
- Modify: `Assets/03_Code/03_Boss/Telegraph/RingDrawer.cs`

**Interfaces:**
- Consumes: `BeginSignal(Animator sourceAnimator)`, `StopSignal()`, and `CanParryEvent` from the existing global event bus.
- Produces: a ring that samples `ParryTelegraphProgress`, never regresses during one signal, and runs apex feedback once per signal.

- [x] **Step 1: Remove the timer API and state**

Remove the old `PlaySignal(float windowDuration)` and `Sequence(float dur)` coroutine, the `UnityEngine.InputSystem` import, and timer-specific comments. Keep `DotFlash()` and its realtime visual wait because it is post-apex presentation only.

- [x] **Step 2: Add Animator sampling state and event lifecycle**

Add these fields and lifecycle methods:

```csharp
private const string ParryTelegraphProgressName = "ParryTelegraphProgress";
private static readonly int ParryTelegraphProgressId = Animator.StringToHash(ParryTelegraphProgressName);

private Animator sourceAnimator;
private float previousProgress;
private bool isPlaying;
private bool hasReachedApex;
private bool hasWarnedInvalidAnimator;

private void OnEnable()
{
    EventBus<CanParryEvent>.action += HandleCanParry;
}

private void OnDisable()
{
    EventBus<CanParryEvent>.action -= HandleCanParry;
    StopAllCoroutines();
    isPlaying = false;
    sourceAnimator = null;
}
```

Keep the existing `Init()` renderer setup and initialize `radius` to `startRadius`.

- [x] **Step 3: Implement the time-free signal API**

Implement the following behavior:

```csharp
public void BeginSignal(Animator sourceAnimator)
{
    StopAllCoroutines();
    this.sourceAnimator = sourceAnimator;
    previousProgress = 0f;
    hasReachedApex = false;
    radius = startRadius;

    if (sourceAnimator == null || !HasProgressParameter(sourceAnimator))
    {
        WarnInvalidAnimator(sourceAnimator);
        isPlaying = false;
        return;
    }

    isPlaying = true;
    ringDrawerSimple.DrawCircle();
    DrawCircle();
}

public void StopSignal()
{
    StopAllCoroutines();
    isPlaying = false;
    sourceAnimator = null;
    previousProgress = 0f;
    hasReachedApex = false;
    radius = startRadius;

    if (lr != null)
        DrawCircle();

    if (gameObject.activeSelf)
        gameObject.SetActive(false);
}
```

`HasProgressParameter(Animator)` must inspect `sourceAnimator.parameters` for `ParryTelegraphProgress`; `WarnInvalidAnimator` must log only once for the `RingDrawer` instance and must distinguish a missing Animator/parameter from normal gameplay.

- [x] **Step 4: Implement LateUpdate sampling**

Use the following logic each frame while a valid signal is active:

```csharp
private void LateUpdate()
{
    if (!isPlaying || sourceAnimator == null || lr == null)
        return;

    float sampledProgress = Mathf.Clamp01(sourceAnimator.GetFloat(ParryTelegraphProgressId));
    float progress = Mathf.Max(previousProgress, sampledProgress);
    previousProgress = progress;
    radius = Mathf.Lerp(startRadius, contactRadius, progress);
    float bright = Mathf.Lerp(0.6f, 1.4f, progress);
    lr.startColor = lr.endColor = goldColor * bright;
    DrawCircle();
}
```

Do not trigger apex feedback from `progress == 1`; only the Animation Event's `CanParryEvent(true)` may do that.

- [x] **Step 5: Implement one-shot apex handling**

`HandleCanParry(CanParryEvent data)` must call `StopSignal()` for `false`. For `true`, it must return unless `isPlaying` is active and `hasReachedApex` is false; then set `hasReachedApex = true`, set `previousProgress = 1f`, set `radius = contactRadius`, redraw, start one `DotFlash()` coroutine when configured, and publish the existing `SlowMoEvent` once when `useSlowMo` is enabled.

- [x] **Step 6: Run the contract check**

Run the Task 1 PowerShell contract command again.

Expected: PASS for `BeginSignal(Animator)`, `StopSignal()`, and the time-free base contract after the dependent files are updated in Task 3.

---

### Task 3: Update the base contract and Cinderella BT callers

**Files:**
- Modify: `Assets/03_Code/03_Boss/FSM/BossPatternBase.cs`
- Modify: `Assets/03_Code/03_Boss/FSM/Cinderella_Patterns.cs`

**Interfaces:**
- Consumes: `telegraphDrawer.BeginSignal(anim)` and `telegraphDrawer.StopSignal()`.
- Produces: `protected abstract NodeState PlayTelegraph(TelegraphType type)` and no Cinderella attack-specific telegraph duration calls.

- [x] **Step 1: Change the abstract API and remove obsolete adjustment logic**

Change:

```csharp
protected abstract NodeState PlayTelegraph(float baseTime, TelegraphType type);
```

to:

```csharp
protected abstract NodeState PlayTelegraph(TelegraphType type);
```

Remove `GetAdjustedTelegraphTime(float baseTime)` entirely.

- [x] **Step 2: Add signal cleanup to the base lifecycle**

Make `OnDisable()` a block method that unsubscribes `ParryKeyDown`, calls `telegraphDrawer?.StopSignal()`, and publishes `new CanParryEvent(false)`. Call `telegraphDrawer?.StopSignal()` at the start of `LogicInit()`. Call it from `Parryed()` before publishing `CanParryEvent(false)` and disabling the attack collider.

- [x] **Step 3: Make Cinderella prepare the signal without a duration**

Replace the override body with:

```csharp
protected override NodeState PlayTelegraph(TelegraphType type)
{
    if (telegraph != null) telegraph.SetActive(true);
    telegraphDrawer?.BeginSignal(anim);
    telegraphExcuted = true;
    return NodeState.Success;
}
```

- [x] **Step 4: Remove duration arguments at all Cinderella callers**

Use `PlayTelegraph(TelegraphType.RingDrawer)` for A, B, and each Ultimate combo step. Remove the local Ultimate `time` selection block. Keep `A_telegraphTime`, `B_telegraphTime`, and `ULTI_telegraphTime1/2/3` serialized and unused for this migration.

- [x] **Step 5: Verify no obsolete Cinderella/base references remain**

Run:

```powershell
rg -n "PlayTelegraph\(float|GetAdjustedTelegraphTime|PlaySignal\(" 'Assets/03_Code/03_Boss/FSM/BossPatternBase.cs' 'Assets/03_Code/03_Boss/FSM/Cinderella_Patterns.cs' 'Assets/03_Code/03_Boss/Telegraph/RingDrawer.cs'
```

Expected: no output.

---

### Task 4: Preserve RougeHood LaserSight timing under the new override

**Files:**
- Modify: `Assets/03_Code/03_Boss/Legacy/RougeHood_Patterns.cs`

**Interfaces:**
- Consumes: the new `PlayTelegraph(TelegraphType)` base contract.
- Produces: unchanged `LaserSight` start, `curTelegraphTime` accumulation, blink threshold, timeout, and stop behavior.

- [x] **Step 1: Update the BT call and override signature**

Change the call to `PlayTelegraph(TelegraphType.LaserSight)` and change the override to:

```csharp
protected override NodeState PlayTelegraph(TelegraphType type)
```

- [x] **Step 2: Keep the existing LaserSight timer source**

Inside the LaserSight branch, replace `baseTime` only with the existing serialized `laserSightDuration` in these comparisons:

```csharp
if (curTelegraphTime > laserSightDuration - blinkTimingBeforeAttack && laserSight.GetAimLock() == false)
    laserSight.LockAim();

if (curTelegraphTime > laserSightDuration)
{
    laserSight.StopAiming();
    return NodeState.Success;
}
```

Keep `curTelegraphTime += Time.deltaTime`, `StartAiming()`, and all bullet behavior unchanged.

- [x] **Step 3: Verify all overrides match the new contract**

Run:

```powershell
rg -n -C 1 "PlayTelegraph\(" 'Assets/03_Code/03_Boss'
```

Expected: only `PlayTelegraph(TelegraphType type)` declarations/calls remain, with no time argument at any call site.

---

### Task 5: Compile and review the final diff

**Files:**
- Verify: the four C# files above and the plan file.

- [x] **Step 1: Restore and build the correct Unity project assembly**

Run:

```powershell
dotnet restore Assembly-CSharp.csproj
dotnet build Assembly-CSharp.csproj
```

Expected: exit code `0`; report any existing warnings separately from errors.

- [x] **Step 2: Run final contract and scope checks**

Run:

```powershell
rg -n "BeginSignal\(Animator|StopSignal\(\)|ParryTelegraphProgress|CanParryEvent|SlowMoEvent|PlayTelegraph\(" 'Assets/03_Code/03_Boss/Telegraph/RingDrawer.cs' 'Assets/03_Code/03_Boss/FSM/BossPatternBase.cs' 'Assets/03_Code/03_Boss/FSM/Cinderella_Patterns.cs' 'Assets/03_Code/03_Boss/Legacy/RougeHood_Patterns.cs'
git diff -- 'Assets/03_Code/03_Boss/Telegraph/RingDrawer.cs' 'Assets/03_Code/03_Boss/FSM/BossPatternBase.cs' 'Assets/03_Code/03_Boss/FSM/Cinderella_Patterns.cs' 'Assets/03_Code/03_Boss/Legacy/RougeHood_Patterns.cs'
```

Confirm that no Animation/Scene/ProjectSettings user changes were edited by this task, and that `PlayerParry.cs`, `BossAnimationKeyReceiver.cs`, and `Struct_Boss.cs` remain untouched.

- [x] **Step 3: Update the execution checklist without staging or committing**

Mark completed items in this plan only after the build and diff checks provide evidence. Do not run `git add`, `git commit`, `git reset`, or `git checkout`.

## Self-Review

- Spec coverage: Ring sampling, monotonic progress, event-authoritative apex, one-shot SlowMo, lifecycle cleanup, serialized-field preservation, and RougeHood compatibility are covered by Tasks 2-4.
- Placeholder scan: no unresolved placeholder marker or unspecified implementation step is required.
- Type consistency: all producers and consumers use `PlayTelegraph(TelegraphType type)`; `RingDrawer` exposes `BeginSignal(Animator)` and `StopSignal()` exactly.
- Scope: no new manager, timer profile, dependency, Animation Clip edit, or player-side change is introduced.
- Asset validation note: `Mma Kick_L.anim` and `SpinShard.anim` contain `ParryTelegraphProgress`, but `Ultimate1_L.anim` and `Ultimate2_L.anim` currently do not; those user-owned Animation Clip edits remain outside this script-only implementation.
