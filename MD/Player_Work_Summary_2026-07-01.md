# Player 작업 정리 - 2026-07-01

집에서 이어서 작업할 때 빠르게 흐름을 잡기 위한 정리 문서입니다.

## 1. 현재 Player 구조 기준

현재 Player는 `PlayerInitializer`를 최상위 초기화 지점으로 두는 방향입니다.

```text
PlayerInitializer
-> 필수 컴포넌트 보장
-> 참조 캐싱
-> ServiceLocator 등록
-> 각 Player 컴포넌트 Initialize 호출
-> PlayerController 초기 상태 진입
```

역할 분리는 다음처럼 이해하면 됩니다.

```text
PlayerInitializer
= 플레이어 루트 구성과 초기화 순서 담당

PlayerController
= 입력 읽기, 상태 전환 담당

PlayerMovement
= 이동, 점프, 중력, 대쉬 계산 담당

PlayerAttack
= 플레이어 공격 방향 계산, BulletFactory 호출 담당

PlayerParry
= 패링 입력 창, 보스 패링 가능 창 확인 담당

PlayerStatus
= 체력, 데미지, 피격, 사망, 부활 처리 담당

PlayerCheckpointTracker
= 체크포인트 위치/체력/회복 아이템 스냅샷 저장 담당

PlayerHealItemInventory
= 회복 아이템 보유량과 사용 완료 처리 담당

PlayerLifeTracker
= 부활 시 목숨 차감과 목숨 이벤트 담당

PlayerExperienceTracker
= 몬스터 사망 이벤트 기반 경험치/레벨업 이벤트 담당
```

## 2. 최근 적용한 Player 정리 작업

### RequireComponent 정리

개별 Player 스크립트에 흩어져 있던 `RequireComponent`를 `PlayerInitializer` 기준으로 모았습니다.

현재 기준:

```text
PlayerInitializer = 플레이어 루트 필수 컴포넌트 보장
PlayerController = 입력/상태 전환
PlayerAttack / PlayerMovement / PlayerStatus = 각 기능만 담당
```

개별 스크립트에서는 `RequireComponent`를 제거했습니다.

- `PlayerAttack`
- `PlayerController`
- `PlayerMovement`
- `PlayerStatus`

### PlayerAttack 방어 로그 추가

`PlayerAttack`에서 필수 세팅이 비어 있으면 조용히 실패하지 않고 한 번만 경고를 띄우게 했습니다.

확인하는 항목:

```text
firePoint 누락
bulletFactory 누락
PlayerStatus 참조 누락
ownerLayer 비어 있음
```

`PlayerAttack.ownerLayer`는 Player 레이어가 들어가야 합니다.

```text
Player layer = 64
```

`bulletFactory`는 씬 오브젝트 참조라 Player 프리팹에 무리해서 넣지 않고, 씬 인스턴스에서 연결하는 방향입니다.

### Player State region 정리

`Assets/00.Scripts/Player/State/**` 내부 State 파일만 `#region / #endregion`으로 정리했습니다.

공통 region 기준:

```text
#region 필드
#region 상태 권한
#region 생성자
#region 상태 생명주기
#region 상태 전환 체크
#region 내부 계산
```

`Idle`, `Move`, `Fall`, `Dash`, `Hit`, `HealItemUse`는 상태 전환 조건을 `Check...Transition()` 메서드로 분리했습니다.

`Jump`, `Death`는 단순 상태라 불필요한 메서드 분리 없이 region만 적용했습니다.

동작 조건은 바꾸지 않았습니다.

## 3. 데미지 구조 이해

현재 `PlayerStatus`에는 `TakeDamage`가 2종류 있습니다.

```csharp
public void TakeDamage(float damage)
public void TakeDamage(DamageInfo damageInfo)
```

흐름은 다음과 같습니다.

```text
TakeDamage(float damage)
-> 패링 여부 먼저 확인
-> 최소 정보로 DamageInfo 생성
-> TakeDamage(DamageInfo damageInfo) 호출
-> 실제 HP 감소, 피격 이벤트, 사망 처리
```

즉 실제 데미지 처리 본체는 `TakeDamage(DamageInfo)`입니다.

`TakeDamage(float)`는 외부에서 숫자 데미지만 들어오는 상황을 위한 호환 입구입니다.

현재 `TakeDamage(float)` 안에서 만드는 `DamageInfo`는 정식 공격 정보라기보다 최소 정보 포장입니다.

```text
TargetObject = gameObject
HitCollider = null
AttackerObject = null
HitPoint = transform.position
HitDirection = Vector3.zero
Damage = damage
```

## 4. IDamageable / DamageInfo 논의 결론

현재 `IDamageable`은 다음 형태입니다.

```csharp
public interface IDamageable
{
    void TakeDamage(float amount);
}
```

팀 상의 전까지는 이 형태를 유지하는 방향입니다.

이유:

```text
Bullet, BulletFactory, Interfaces는 공용 영향이 큼
스킬 제작자는 단순하게 IDamageable.TakeDamage(float)를 쓰길 원함
현재 Bullet도 float 기반으로 호출 중
```

현재 타협 구조:

```text
외부 공통 호출
= IDamageable.TakeDamage(float)

Player 내부 처리
= TakeDamage(float)
-> DamageInfo로 감싸기
-> TakeDamage(DamageInfo)
```

나중에 통합 단계에서 더 정식으로 가려면 다음 방향이 맞습니다.

```csharp
public interface IDamageable
{
    void TakeDamage(DamageInfo damageInfo);
}
```

하지만 지금 바로 바꾸면 Player, Monster, Boss, Bullet, Skill 쪽까지 연쇄 수정이 생기므로 보류합니다.

## 5. 공격 판정 책임 정리

공격 판정을 가진 스크립트가 대상을 찾고 데미지를 전달합니다.

```text
Bullet
-> 총알 충돌 판정

MeleeHitBox
-> 근접 공격 범위 판정

Skill
-> 스킬 범위/투사체 판정

Trap
-> 함정 접촉 판정
```

다만 공격 스크립트가 Player/Monster/Boss 종류를 직접 구분할 필요는 없습니다.

기본 흐름:

```text
Collider 감지
-> IDamageable 찾기
-> TakeDamage 호출
```

현재 팀 합의 기준:

```text
스킬 제작자는 IDamageable.TakeDamage(float damage)만 호출해도 됨
대상의 세부 처리 방식은 각 Status가 담당
```

## 6. DamageInfo를 쓰는 이유

`DamageInfo`는 다음 정보를 한 번에 넘기기 위한 구조입니다.

```text
TargetObject = 데미지를 받는 대상
HitCollider = 실제 맞은 Collider
AttackerObject = 공격자
HitPoint = 맞은 위치
HitDirection = 공격 방향
Damage = 데미지 값
```

장점:

```text
피격 이펙트 위치를 정확히 알 수 있음
넉백 방향 계산이 쉬움
공격자 추적 가능
나중에 치명타, 속성, 거리 비례 데미지 같은 확장이 쉬움
```

하지만 현재 팀 상황에서는 외부 공용 인터페이스를 float로 유지하고, Player 내부에서만 DamageInfo 흐름을 유지하는 것이 안전합니다.

## 7. Bullet / BulletFactory 관련 주의사항

현재 방침:

```text
Bullet.cs 수정 금지
BulletFactory.cs 수정 금지
Core/Initialization/** 수정 금지
Interfaces.cs 수정 금지
```

이유:

```text
공용 영역이고 팀 상의가 필요함
스킬/몬스터/보스 쪽에 영향이 생길 수 있음
```

현재 확인된 위험:

```text
Bullet은 IDamageable.TakeDamage(float)를 호출함
Bullet은 현재 DamageInfo를 직접 만들지 않음
PlayerBulletHitEvent도 Bullet에서 발행하지 않음
BulletFactory는 IInitializable과 Start Init이 동시에 있을 수 있음
```

하지만 이 부분은 팀 상의 전까지 보류합니다.

## 8. 피격/무적 처리

현재 Player 피격 무적은 적용되어 있습니다.

```text
HitStunDuration = 0.2초
HitInvincibleDuration = 0.5초
총 무적 판정 = 피격 시점부터 0.7초
```

흐름:

```text
TakeDamage(DamageInfo)
-> 현재 상태가 데미지를 받을 수 있는지 확인
-> invincibleEndTime 확인
-> HP 감소
-> HitState 진입
-> invincibleEndTime = Time.time + 0.2 + 0.5
```

상태별 데미지 가능 여부:

```text
HitState = CanTakeDamage false
DashState = CanTakeDamage false
DeathState = CanTakeDamage false
HealItemUseState = CanTakeDamage true
```

회복 아이템 사용 중 피격되면 데미지를 받고 사용이 취소되는 구조입니다.

## 9. 집에서 이어서 볼 추천 순서

Player 스크립트를 읽을 때는 다음 순서가 좋습니다.

```text
1. PlayerInitializer
2. PlayerController
3. PlayerBaseState
4. PlayerIdleState
5. PlayerMoveState
6. PlayerJumpState
7. PlayerFallState
8. PlayerDashState
9. PlayerMovement
10. PlayerAttack
11. PlayerParry
12. PlayerStatus
13. PlayerHitState
14. PlayerDeathState
15. PlayerCheckpointTracker
16. PlayerHealItemInventory
17. PlayerHealItemUseState
18. PlayerLifeTracker
19. PlayerExperienceTracker
20. PlayerEvents / EventBus
```

처음부터 `PlayerStatus`를 보면 내용이 많아서 흐름이 꼬일 수 있으니, 초기화와 상태머신부터 보는 것을 추천합니다.

## 10. 다음 작업 후보

우선순위 높은 후보:

```text
Player 쪽 깨진 한글 주석 정리
PlayerAttack의 씬 연결 상태 확인
PlayerStatus 책임 분리 여부 검토
패링 성공/실패 흐름 실제 보스 씬에서 재확인
회복 아이템 UI/사운드 연결
경험치 레벨업 이후 스킬 선택 UI 연결
```

보류해야 할 후보:

```text
IDamageable을 DamageInfo로 변경
Bullet에서 DamageInfo 생성
BulletFactory 초기화 구조 수정
Boss/Monster 데미지 구조 통합
```

이 보류 항목들은 팀 상의 후 진행하는 것이 안전합니다.
