# Boss Parry Hitbox Flow Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 기존 이벤트 기반 보스 공격 구조를 유지하면서 동일한 공격 콜라이더가 패링 범위 감지와 실제 피해 판정을 순서대로 담당하도록 구현한다.

**Architecture:** `CanParryEvent`가 패링 시간과 공격 ID를 전달하고, `AttackColliders_Y`가 해당 콜라이더를 범위 감지 상태로 활성화한다. 이후 `ColliderToggleEvent(true)`가 `BossHitBox`를 피해 상태로 전환하며, `BossHitBox`는 저장된 플레이어와 기존 `isTriggered`를 사용해 한 공격당 한 번만 피해를 적용한다.

**Tech Stack:** Unity, C#, PhysX Trigger Callback, Animation Event, 프로젝트 기존 `EventBus<T>`

## Global Constraints

- `Assets/03_Code/01_Player/PlayerParry.cs`는 플레이어 개발자 소유 파일이므로 수정하지 않는다.
- 구현 시작 시 `PlayerParry.cs`의 현재 해시 `b0de7545345374f1954188459e83c94662359f2e`를 기록하고 구현 종료 후 동일한지 확인한다.
- `ColliderToggleEvent.state == false`이면 활성화된 보스 공격 콜라이더를 반드시 비활성화한다.
- `ParryKeyDown`은 플레이어 측에서 패링 시간과 공간을 모두 만족했을 때 발행하는 성공 이벤트로 취급한다.
- `AttackFinishEvent`만 한 공격의 종료 경계로 사용하며, `isTriggered`는 이 이벤트에서만 `false`로 초기화한다.
- 애니메이션 이벤트 순서는 `EnableParry -> DisableParry -> EnableAttack -> DisableAttack`으로 통일한다.
- `DisableParry`는 패링 시간만 닫고 범위 감지 콜라이더는 끄지 않는다.
- 기존 serialized field, 공격 ID, BT/FSM, `EventBus<T>` 구조를 보존한다.
- 새 Manager, Singleton, 런타임 검색, 외부 의존성 및 테스트 어셈블리를 추가하지 않는다.
- Git stage, commit, push는 사용자가 별도로 승인하기 전까지 수행하지 않는다.

## File Map

### 수정할 코드 파일

- `Assets/03_Code/03_Boss/Struct_Boss.cs`: `CanParryEvent`에 `AttackId` 추가.
- `Assets/03_Code/03_Boss/BossAnimationKeyReceiver.cs`: 이벤트 payload 갱신 및 `OnCollider/OffCollider`를 `EnableAttack/DisableAttack`으로 변경.
- `Assets/03_Code/03_Boss/FSM/BossController.cs`: 강제 패링 종료 시 현재 공격 ID 포함.
- `Assets/03_Code/03_Boss/FSM/BossPatternBase.cs`: 비활성화·패링 종료 시 현재 공격 ID 포함.
- `Assets/03_Code/03_Boss/Collider/BossHitBox.cs`: 범위 추적, 피해 상태, 공통 중복 피격 방어 구현.
- `Assets/03_Code/03_Boss/Collider/AttackColliders_Y.cs`: 패링 범위 활성화와 피해 상태 전환을 기존 공격 ID 매핑에 연결.

### 수정할 Animation Clip

- `Assets/01_Art/Dev/Boss/Animation/Mma Kick_L.anim`
- `Assets/01_Art/Dev/Boss/Animation/SpinShard.anim`
- `Assets/01_Art/Dev/Boss/Animation/Ultimate1_L.anim`
- `Assets/01_Art/Dev/Boss/Animation/Ultimate2_L.anim`
- `Assets/01_Art/Dev/Boss/Animation/Jump Attack.anim`

### 수정하지 않을 파일

- `Assets/03_Code/01_Player/PlayerParry.cs`
- `Assets/03_Code/01_Player/PlayerStatus.cs`
- 보스 BT 구조, prefab, scene, Animator Controller

---

### Task 1: 이벤트 계약과 애니메이션 수신기 갱신

**Files:**
- Modify: `Assets/03_Code/03_Boss/Struct_Boss.cs`
- Modify: `Assets/03_Code/03_Boss/BossAnimationKeyReceiver.cs`
- Modify: `Assets/03_Code/03_Boss/FSM/BossController.cs`
- Modify: `Assets/03_Code/03_Boss/FSM/BossPatternBase.cs`

**Interfaces:**
- Produces: `CanParryEvent(string attackId, bool canParry)`, `EnableAttack()`, `DisableAttack()`
- Preserves: `ColliderToggleEvent(string attackId, bool state)`

- [ ] **Step 1: 변경 전 호출부를 확인한다**

Run:

```powershell
rg -n "new CanParryEvent|OnCollider|OffCollider" Assets/03_Code
```

Expected: 기존 `CanParryEvent(bool)` 호출부와 `BossAnimationKeyReceiver.OnCollider/OffCollider`가 검색된다.

- [ ] **Step 2: `CanParryEvent`에 공격 ID를 추가한다**

`Struct_Boss.cs`의 구조체를 다음 계약으로 변경한다.

```csharp
public struct CanParryEvent
{
    public string AttackId { get; private set; }
    public bool CanParry { get; private set; }

    public CanParryEvent(string attackId, bool canParry)
    {
        AttackId = attackId;
        CanParry = canParry;
    }
}
```

- [ ] **Step 3: `BossAnimationKeyReceiver`의 이벤트 이름과 payload를 갱신한다**

```csharp
public void EnableParry()
{
    EventBus<CanParryEvent>.Publish(
        new CanParryEvent(bossPatternLogic.GetAttackId(), true));
}

public void DisableParry()
{
    EventBus<CanParryEvent>.Publish(
        new CanParryEvent(bossPatternLogic.GetAttackId(), false));
}

public void EnableAttack()
{
    if (bossPatternLogic.IsParryed) return;
    if (!bossPatternLogic.IsAttacking()) return;

    EventBus<ColliderToggleEvent>.Publish(
        new ColliderToggleEvent(bossPatternLogic.GetAttackId(), true));
}

public void DisableAttack()
{
    EventBus<ColliderToggleEvent>.Publish(
        new ColliderToggleEvent(bossPatternLogic.GetAttackId(), false));
}
```

기존 `OnCollider()`와 `OffCollider()` 메서드는 남기지 않는다. Animation Clip을 같은 작업 묶음에서 갱신하여 문자열 기반 이벤트 참조가 끊긴 상태로 종료되지 않게 한다.

- [ ] **Step 4: 나머지 `CanParryEvent` 발행부에 공격 ID를 전달한다**

`BossController`에서는 다음 형태를 사용한다.

```csharp
EventBus<CanParryEvent>.Publish(
    new CanParryEvent(logics.GetAttackId(), false));
```

`BossPatternBase`의 `OnDisable()` 및 `Parryed()`에서는 다음 형태를 사용한다.

```csharp
EventBus<CanParryEvent>.Publish(
    new CanParryEvent(attackId, false));
```

- [ ] **Step 5: 생성자 호출 누락을 검사한다**

Run:

```powershell
rg -n "new CanParryEvent\([^,\)]*\)" Assets/03_Code
```

Expected: 결과 없음. 모든 호출부가 `attackId, bool` 두 인자를 사용한다.

- [ ] **Step 6: 리뷰 체크포인트**

`PlayerParry`와 `RingDrawer`는 기존 `data.CanParry`를 그대로 읽으므로 수정하지 않았는지 확인한다. Git 작업은 수행하지 않는다.

---

### Task 2: `BossHitBox`에 범위와 피해 상태 분리

**Files:**
- Modify: `Assets/03_Code/03_Boss/Collider/BossHitBox.cs`

**Interfaces:**
- Consumes: `PlayerParry.SetInBossAttackRange(bool isInside)`, `IDamageable.TakeDamage(float damage)`, `AttackFinishEvent`
- Produces: `BeginRangeCheck()`, `EnterDamagePhase()`, `ClearRangeState()`

- [ ] **Step 1: 기존 중복 피격 경계를 기록한다**

Run:

```powershell
rg -n "isTriggered|OnTriggerEnter|AttackFinishEvent" Assets/03_Code/03_Boss/Collider/BossHitBox.cs
```

Expected: `OnTriggerEnter`에서 `isTriggered`를 설정하고 `AttackFinishEvent`에서 초기화하는 현재 흐름이 검색된다.

- [ ] **Step 2: 히트박스 상태와 현재 플레이어 참조를 추가한다**

```csharp
private enum HitBoxPhase
{
    Inactive,
    RangeCheck,
    Damage
}

private HitBoxPhase phase = HitBoxPhase.Inactive;
private Collider currentPlayerCollider;
private IDamageable currentTarget;
private PlayerParry currentPlayerParry;
```

- [ ] **Step 3: 공개 상태 전환 API를 추가한다**

```csharp
public void BeginRangeCheck()
{
    ClearTrackedPlayer();
    phase = HitBoxPhase.RangeCheck;
}

public void EnterDamagePhase()
{
    phase = HitBoxPhase.Damage;
    TryApplyDamage();
}

public void ClearRangeState()
{
    ClearTrackedPlayer();
    phase = HitBoxPhase.Inactive;
}
```

`BeginRangeCheck()`와 `ClearRangeState()`에서는 `isTriggered`를 변경하지 않는다.

- [ ] **Step 4: Trigger Callback을 범위 추적으로 변경한다**

```csharp
private void OnTriggerEnter(Collider other)
{
    if (!other.CompareTag("Player")) return;

    currentPlayerCollider = other;
    currentTarget = other.GetComponent<IDamageable>();
    currentPlayerParry = other.GetComponent<PlayerParry>();
    currentPlayerParry?.SetInBossAttackRange(true);

    if (phase == HitBoxPhase.Damage)
    {
        TryApplyDamage();
    }
}

private void OnTriggerExit(Collider other)
{
    if (other != currentPlayerCollider) return;

    ClearTrackedPlayer();
}
```

- [ ] **Step 5: 두 피해 진입 경로에 공통 중복 방어를 적용한다**

```csharp
private void TryApplyDamage()
{
    if (isTriggered) return;
    if (currentTarget == null) return;

    isTriggered = true;
    currentTarget.TakeDamage(AtkPower.GetAtkPower(AtkType));
}

private void ClearTrackedPlayer()
{
    currentPlayerParry?.SetInBossAttackRange(false);
    currentPlayerCollider = null;
    currentTarget = null;
    currentPlayerParry = null;
}
```

`isTriggered = true`는 `TakeDamage()`보다 먼저 실행한다. 피해 처리 중 다른 이벤트가 연쇄 발행되어도 동일 공격이 재진입하지 않게 하기 위함이다.

- [ ] **Step 6: 공격 종료에서만 중복 방어를 초기화한다**

기존 `SetIsTriggered()`를 다음 책임으로 확장한다.

```csharp
private void HandleAttackFinished(AttackFinishEvent data)
{
    ClearRangeState();
    isTriggered = false;
}
```

`OnEnable/OnDisable` 구독 메서드 이름도 `HandleAttackFinished`로 맞춘다. `OnDisable()`에서는 구독 해제 후 `ClearRangeState()`를 호출하되 `isTriggered`의 공격 단위 초기화는 `AttackFinishEvent`에 둔다.

- [ ] **Step 7: 코드 구조를 검사한다**

Run:

```powershell
rg -n "isTriggered = false|isTriggered = true|TryApplyDamage|EnterDamagePhase|SetInBossAttackRange" Assets/03_Code/03_Boss/Collider/BossHitBox.cs
```

Expected:

- `isTriggered = true`는 `TryApplyDamage()` 한 곳에만 존재한다.
- 런타임 `isTriggered = false`는 `HandleAttackFinished()` 한 곳에만 존재한다.
- `TryApplyDamage()`는 `EnterDamagePhase()`와 Damage 상태의 `OnTriggerEnter()`에서 호출된다.

- [ ] **Step 8: 리뷰 체크포인트**

한 공격에서 먼저 범위에 들어온 경우와 공격 활성화 뒤 들어온 경우가 동일한 `TryApplyDamage()`와 `isTriggered`를 공유하는지 검토한다. Git 작업은 수행하지 않는다.

---

### Task 3: `AttackColliders_Y`에 범위 준비와 피해 전환 연결

**Files:**
- Modify: `Assets/03_Code/03_Boss/Collider/AttackColliders_Y.cs`

**Interfaces:**
- Consumes: `CanParryEvent.AttackId`, `BossHitBox.BeginRangeCheck()`, `BossHitBox.EnterDamagePhase()`, `BossHitBox.ClearRangeState()`
- Preserves: 기존 explicit binding과 legacy list 양쪽의 공격 ID 조회

- [ ] **Step 1: 이벤트 구독을 확장한다**

```csharp
private void OnEnable()
{
    RebuildBindings();
    EventBus<CanParryEvent>.action += HandleCanParry;
    EventBus<ColliderToggleEvent>.action += ToggleCollider;
    EventBus<BossFacingChangeEvent>.action += ChangeColliderPos;
    EventBus<ParryKeyDown>.action += OffCollider;
    EventBus<AttackFinishEvent>.action += HandleAttackFinished;
}

private void OnDisable()
{
    EventBus<CanParryEvent>.action -= HandleCanParry;
    EventBus<ColliderToggleEvent>.action -= ToggleCollider;
    EventBus<BossFacingChangeEvent>.action -= ChangeColliderPos;
    EventBus<ParryKeyDown>.action -= OffCollider;
    EventBus<AttackFinishEvent>.action -= HandleAttackFinished;
    DisableAllColliders();
}
```

- [ ] **Step 2: `CanParryEvent(true)`에서 범위 감지를 시작한다**

```csharp
private void HandleCanParry(CanParryEvent data)
{
    if (!data.CanParry) return;
    if (!TryGetCollider(data.AttackId, out BoxCollider targetCollider)) return;

    BossHitBox hitBox = targetCollider.GetComponent<BossHitBox>();
    hitBox?.BeginRangeCheck();
    targetCollider.enabled = true;
}
```

`CanParryEvent(false)`에서는 콜라이더를 끄지 않는다. `DisableParry`와 `EnableAttack` 사이에도 범위 안의 플레이어 참조가 유지되어야 한다.

- [ ] **Step 3: `ColliderToggleEvent`의 true/false 의미를 구현한다**

```csharp
public void ToggleCollider(ColliderToggleEvent data)
{
    if (!data.state)
    {
        DisableAllColliders();
        return;
    }

    if (!TryGetCollider(data.attackId, out BoxCollider targetCollider)) return;

    BossHitBox hitBox = targetCollider.GetComponent<BossHitBox>();
    hitBox?.EnterDamagePhase();
    targetCollider.enabled = true;
}
```

`EnterDamagePhase()`를 먼저 호출하고 콜라이더를 활성화한다. 패링 공격처럼 이미 활성화된 경우 저장된 플레이어에게 즉시 피해를 시도하고, 일반 공격처럼 꺼져 있던 경우 활성화 후 발생하는 `OnTriggerEnter()`가 Damage 상태에서 피해를 시도한다.

- [ ] **Step 4: 모든 종료 경로를 동일한 정리 메서드에 연결한다**

```csharp
public void OffCollider(ParryKeyDown data)
{
    DisableAllColliders();
}

private void HandleAttackFinished(AttackFinishEvent data)
{
    DisableAllColliders();
}
```

기존 `DisableAllColliders()`의 각 분기에서 콜라이더를 끄기 전에 다음 순서를 적용한다.

```csharp
private void DisableCollider(BoxCollider attackCollider)
{
    if (attackCollider == null) return;

    attackCollider.GetComponent<BossHitBox>()?.ClearRangeState();
    attackCollider.enabled = false;
}
```

explicit binding과 legacy list 양쪽 모두 직접 `enabled = false`를 쓰지 않고 `DisableCollider()`를 호출한다.

- [ ] **Step 5: 이벤트 책임을 검색으로 검증한다**

Run:

```powershell
rg -n "HandleCanParry|EnterDamagePhase|DisableAllColliders|AttackFinishEvent|ParryKeyDown" Assets/03_Code/03_Boss/Collider/AttackColliders_Y.cs
```

Expected:

- `CanParryEvent(true)`는 범위 감지를 시작한다.
- `ColliderToggleEvent(true)`는 Damage 상태로 전환한다.
- `ColliderToggleEvent(false)`, `ParryKeyDown`, `AttackFinishEvent`는 모두 `DisableAllColliders()`로 수렴한다.

- [ ] **Step 6: 리뷰 체크포인트**

explicit binding과 legacy list 중 하나만 수정되는 회귀가 없는지 검토한다. Git 작업은 수행하지 않는다.

---

### Task 4: Animation Event 이름 마이그레이션

**Files:**
- Modify: `Assets/01_Art/Dev/Boss/Animation/Mma Kick_L.anim`
- Modify: `Assets/01_Art/Dev/Boss/Animation/SpinShard.anim`
- Modify: `Assets/01_Art/Dev/Boss/Animation/Ultimate1_L.anim`
- Modify: `Assets/01_Art/Dev/Boss/Animation/Ultimate2_L.anim`
- Modify: `Assets/01_Art/Dev/Boss/Animation/Jump Attack.anim`

**Interfaces:**
- Consumes: `BossAnimationKeyReceiver.EnableAttack()`, `BossAnimationKeyReceiver.DisableAttack()`
- Removes: Animation Event 문자열 `OnCollider`, `OffCollider`

- [ ] **Step 1: 변경 전 이벤트 개수를 확인한다**

Run:

```powershell
rg -n "functionName: (OnCollider|OffCollider)" Assets/01_Art/Dev/Boss/Animation --glob "*.anim"
```

Expected: 총 12개 이벤트가 검색된다.

- [ ] **Step 2: 이벤트 함수 이름을 정확히 치환한다**

각 Animation Clip YAML에서 다음 두 값만 변경한다.

```yaml
functionName: OnCollider
```

to:

```yaml
functionName: EnableAttack
```

and:

```yaml
functionName: OffCollider
```

to:

```yaml
functionName: DisableAttack
```

이벤트의 `time`, `floatParameter`, `intParameter`, `objectReferenceParameter`, `data`는 변경하지 않는다.

- [ ] **Step 3: 이전 이름이 남지 않았는지 확인한다**

Run:

```powershell
rg -n "functionName: (OnCollider|OffCollider)" Assets --glob "*.anim"
```

Expected: 결과 없음.

- [ ] **Step 4: 새 이름 개수를 확인한다**

Run:

```powershell
rg -n "functionName: (EnableAttack|DisableAttack)" Assets/01_Art/Dev/Boss/Animation --glob "*.anim"
```

Expected: 총 12개 이벤트가 검색되며 기존 이벤트 시간과 일치한다.

- [ ] **Step 5: 리뷰 체크포인트**

Animation Clip의 이벤트 함수 이름 이외 YAML 변경이 없는지 `git diff --word-diff=plain`으로 확인한다. Git stage/commit은 수행하지 않는다.

---

### Task 5: 컴파일 및 플레이 모드 검증

**Files:**
- Verify only: `Assembly-CSharp.csproj`
- Verify only: 관련 boss prefab/scene

**Interfaces:**
- Verifies: 이벤트 생성자, Animation Event 메서드, Trigger 상태 전환, 한 공격당 1회 피해

- [ ] **Step 1: `PlayerParry.cs`가 변경되지 않았는지 확인한다**

Run:

```powershell
git hash-object Assets/03_Code/01_Player/PlayerParry.cs
```

Expected: `b0de7545345374f1954188459e83c94662359f2e`

해시가 다르면 구현 작업을 중단하고 해당 변경을 되돌리지 않은 채 사용자에게 보고한다.

- [ ] **Step 2: 프로젝트를 복원한다**

Run:

```powershell
dotnet restore Assembly-CSharp.csproj
```

Expected: exit code 0.

- [ ] **Step 3: C# 컴파일을 검증한다**

Run:

```powershell
dotnet build Assembly-CSharp.csproj --no-restore
```

Expected: `Build succeeded.`, error 0.

- [ ] **Step 4: 이벤트 이름 정합성을 다시 검사한다**

Run:

```powershell
rg -n "public void (EnableParry|DisableParry|EnableAttack|DisableAttack)" Assets/03_Code/03_Boss/BossAnimationKeyReceiver.cs
rg -n "functionName: (EnableParry|DisableParry|EnableAttack|DisableAttack)" Assets/01_Art/Dev/Boss/Animation --glob "*.anim"
rg -n "functionName: (OnCollider|OffCollider)" Assets --glob "*.anim"
```

Expected: 수신기 네 메서드와 새 Animation Event가 검색되고, 마지막 명령은 결과가 없다.

- [ ] **Step 5: Unity Play Mode에서 패링 공격을 검증한다**

다음 시나리오를 각각 새 공격으로 실행한다.

1. `EnableParry` 전에 플레이어가 범위 밖이면 `IsInBossAttackRange`가 false로 유지된다.
2. `EnableParry` 후 플레이어가 들어오면 범위 상태가 true가 된다.
3. 플레이어가 범위 안에 계속 있는 상태에서 `EnableAttack`이 호출되면 피해가 정확히 한 번 적용된다.
4. `EnableAttack` 이후 플레이어가 들어와도 피해가 정확히 한 번 적용된다.
5. 같은 공격에서 플레이어가 나갔다 다시 들어와도 `isTriggered` 때문에 추가 피해가 없다.
6. `DisableAttack`에서 콜라이더가 꺼지고 `OnTriggerExit` 호출 여부와 무관하게 범위 상태가 정리된다.
7. `AttackFinishEvent` 이후 다음 공격에서는 다시 정확히 한 번 피해를 받을 수 있다.
8. 시간과 공간을 만족해 `ParryKeyDown`이 발행되면 콜라이더가 꺼지고 피해가 들어오지 않는다.

- [ ] **Step 6: 패링 이벤트가 없는 일반 공격을 검증한다**

`Jump Attack`처럼 `EnableParry/DisableParry`가 없는 공격에서 다음을 확인한다.

1. `EnableAttack`이 Damage 상태를 먼저 설정한 뒤 콜라이더를 활성화한다.
2. 플레이어가 콜라이더 안에 있으면 `OnTriggerEnter()`를 통해 한 번 피해를 받는다.
3. `DisableAttack`에서 콜라이더가 비활성화된다.
4. `AttackFinishEvent` 이후 다음 공격의 `isTriggered`가 정상 초기화된다.

- [ ] **Step 7: 변경 범위를 검토한다**

Run:

```powershell
git status --short
git diff --stat
```

Expected: File Map에 적힌 코드·Animation Clip·계획 문서 외 새 변경이 없어야 한다. 기존 사용자 변경은 사용자 소유 상태로 그대로 보존한다.

## Deferred Integration Items

- `PlayerParry.TryParry()`의 현재 `isBossParryWindowOpen || isInBossAttackRange` 조건을 AND로 바꾸는 작업은 플레이어 개발자에게 전달하며 이 계획에서 수정하지 않는다.
- `PlayerParry.TryConsumeBossParry()`의 시간·공간 AND 검증도 동일하게 플레이어 개발자 담당으로 보류한다.
- 복수 플레이어 Collider를 위한 `HashSet<Collider>` 또는 진입 카운터는 현재 단일 판정 Collider 전제를 유지하므로 추가하지 않는다.
- 동시에 여러 보스 히트박스가 겹치는 상황을 위한 source-aware `SetInBossAttackRange`는 추가하지 않는다.
- 패링 입력과 성공 확정 시점을 분리하는 pending-input 구조는 현재 `ParryKeyDown` 계약을 벗어나므로 추가하지 않는다.

## Completion Criteria

- Animation Event 순서와 이름이 `EnableParry -> DisableParry -> EnableAttack -> DisableAttack`으로 통일되어 있다.
- `CanParryEvent(true, attackId)`가 해당 공격 콜라이더를 범위 감지 상태로 켠다.
- `ColliderToggleEvent(true)`가 저장된 플레이어 또는 뒤늦게 진입한 플레이어에게 동일한 `TryApplyDamage()`를 통해 피해를 시도한다.
- `isTriggered`가 두 피해 진입 경로 모두를 방어하고 `AttackFinishEvent`에서만 초기화된다.
- `ColliderToggleEvent(false)`, `ParryKeyDown`, `AttackFinishEvent`가 콜라이더와 범위 상태를 명시적으로 정리한다.
- `PlayerParry.cs`의 파일 해시가 구현 전후 동일하다.
- `dotnet build Assembly-CSharp.csproj --no-restore`가 오류 없이 성공한다.
- Unity Play Mode의 패링 공격과 일반 공격 검증 시나리오가 모두 통과한다.
