# 애니메이션 동기화 패링 사전신호 설계

## 1. 목적

보스 공격 애니메이션과 `RingDrawer` 사전신호가 서로 다른 타이머를 사용하면서 발생하는 패링 타이밍 오차를 제거한다.

최종 동작은 다음과 같다.

- 링이 수축하는 동안은 공격 예고만 제공하며 패링을 받지 않는다.
- 링이 정점에 도달한 프레임부터 패링 가능 윈도우를 연다.
- 링 수축 진행도, 정점, 패링 시작은 모두 공격 Animation Clip의 타임라인을 기준으로 한다.
- 공격 애니메이션의 길이, 재생 속도, 분노 배속 또는 전역 시간 배율이 바뀌어도 별도의 초 단위 보정 없이 동기화된다.

## 2. 현재 구조와 문제

현재는 BT의 공격 전 노드가 `PlayTelegraph(float baseTime, ...)`를 호출하고, `RingDrawer.PlaySignal(float windowDuration)`이 `Time.unscaledDeltaTime` 코루틴으로 링을 수축시킨다. 이후 공격 애니메이션이 실행되며 `BossAnimationKeyReceiver.EnableParry()`와 `DisableParry()` Animation Event가 `CanParryEvent`를 발행한다.

따라서 다음 두 시계가 분리되어 있다.

| 영역 | 현재 시간 기준 |
| --- | --- |
| 링 수축 | `Time.unscaledDeltaTime`과 Inspector의 `*_telegraphTime` |
| 공격 및 패링 키 | Animator 상태 시간, `anim.speed`, Animation Event |

두 시계는 같은 프레임에 시작하더라도 애니메이션 전환, 공격 배속, 분노 상태, 슬로모션 등의 영향을 다르게 받아 어긋날 수 있다. `*_telegraphTime`을 눈대중으로 다시 맞추는 방식으로는 클립이나 속도가 변경될 때마다 재조정이 필요하다.

## 3. 핵심 결정

`parrytest.cs`의 핵심 원칙인 "Animator 진행도로 사전신호를 직접 제어한다"를 적용한다. 단순한 `normalizedTime` 전체를 사용하지 않고, 각 Animation Clip에 `ParryTelegraphProgress` float 커브를 둔다.

커브의 계약은 다음과 같다.

- 예고 시작 키: `0`
- 링 정점 및 `EnableParry` 이벤트 키: `1`
- `0`과 `1` 사이의 커브 모양: 해당 공격의 시각적 수축 속도

패링 시작 지점을 별도 Inspector 숫자로 복제하지 않고 Animation Clip 안에서 커브 키와 `EnableParry` 이벤트를 같은 프레임에 배치한다. 클립 타임라인이 유일한 시간 기준이 된다.

## 4. 시간 흐름

```text
BT 공격 진입
  -> 사전신호 준비 및 링 표시
  -> 공격 Animation 상태 진입
  -> ParryTelegraphProgress 0에서 1까지 증가
       이 구간은 CanParry = false
  -> 링 정점 프레임
       1. 커브 값 1
       2. EnableParry Animation Event
       3. CanParryEvent(true)
       4. RingDrawer가 정점을 고정하고 중심점 연출 및 Telegraph SlowMo 요청
       5. PlayerParry가 보스 패링 가능 윈도우 활성화
  -> 패링 가능 윈도우
  -> DisableParry Animation Event
  -> CanParryEvent(false) 및 사전신호 정리
  -> 공격 종료
```

정점 이전에는 패링이 절대 활성화되지 않는다. 정점과 패링 시작은 동일 프레임의 사건으로 취급한다. 프레임 단위 판정에서 플레이어가 링 정점보다 먼저 패링할 수 있는 상태는 존재하지 않는다.

## 5. 구성요소별 책임

### 5.1 Animation Clip

패링 타이밍의 원본 데이터를 소유한다.

- `ParryTelegraphProgress` 커브를 `0 -> 1`로 작성한다.
- 커브가 `1`이 되는 프레임에 기존 `EnableParry()` 이벤트를 둔다.
- 패링 종료 프레임에 기존 `DisableParry()` 이벤트를 둔다.
- 커브의 보간 형태로 공격별 예고 감각을 조정한다.
- 패링 시작/종료 위치를 변경할 때는 같은 Animation Clip 안에서 커브 키와 이벤트를 함께 이동한다.

권장 커브 형태는 후반으로 갈수록 약간 빨라지는 Ease In이다. 플레이어가 초반에는 공격을 인식하고, 정점에 가까워질수록 긴장감을 느낄 수 있다. 게임 규칙에는 영향을 주지 않으며 공격별로 Linear를 사용해도 된다.

### 5.2 `RingDrawer`

시간을 생성하지 않고 Animator 진행도를 표시한다.

권장 공개 API는 다음과 같다.

```csharp
public void BeginSignal(Animator sourceAnimator);
public void StopSignal();
```

내부 책임은 다음과 같다.

- `BeginSignal()`에서 진행도, 정점 도달 플래그, 이전 코루틴을 초기화한다.
- 활성 상태에서는 `LateUpdate()`에서 `sourceAnimator.GetFloat("ParryTelegraphProgress")`를 읽는다.
- 읽은 값을 `0..1`로 제한하고 반지름을 계산한다.
- `radius = Mathf.Lerp(startRadius, contactRadius, progress)` 공식을 사용한다.
- 진행도는 한 공격 안에서 감소하지 않도록 내부적으로 이전 값 이상을 유지한다.
- `CanParryEvent(true)`를 받았고 현재 사전신호가 활성 상태라면 정점을 정확히 `1`로 고정한다.
- 정점 처리는 공격마다 한 번만 실행하며 중심점 깜빡임과 `SlowMoEvent`를 요청한다.
- `CanParryEvent(false)`, 공격 취소, 보스 비활성화에서는 `StopSignal()`로 정리한다.

기존 `Sequence(float dur)` 수축 코루틴과 `PlaySignal(float windowDuration)`의 시간 인자는 제거 대상이다. `DotFlash()` 및 정점 이후의 실시간 연출 대기는 시각 효과이므로 유지할 수 있다. 단, 이 대기값은 패링 시작 시점을 결정하지 않는다.

### 5.3 `BossAnimationKeyReceiver`

기존 Animation Event 진입점과 이벤트 발행 책임을 유지한다.

- `EnableParry()`는 정점 프레임에만 호출한다.
- `EnableParry()`가 발행한 `CanParryEvent(true)`는 플레이어 패링 판정과 `RingDrawer` 정점 처리가 함께 사용한다.
- `DisableParry()`는 패링 종료 프레임에 `CanParryEvent(false)`를 발행한다.

새로운 패링 타이머나 별도 상태 계산은 추가하지 않는다.

### 5.4 `PlayerParry`

현재 역할을 유지한다.

- 플레이어 입력 윈도우와 보스 패링 가능 윈도우가 모두 열렸을 때만 성공한다.
- 링 수축 중에는 `CanParryEvent(true)`가 발행되지 않으므로 보스 패링이 성공하지 않는다.
- 정점 이후 기존 `isBossParryWindowOpen` 흐름을 그대로 사용한다.

### 5.5 BT와 보스 패턴

BT는 공격 순서만 담당하고 타이밍을 계산하지 않는다.

- 사전신호 노드는 `RingDrawer.BeginSignal(anim)`으로 표시 상태만 준비한다.
- 이어지는 공격 애니메이션 노드가 실제 진행도를 제공한다.
- `BossController`, `StatefulSequence`, `IBossLogics` 흐름은 유지한다.
- `PlayTelegraph(float baseTime, TelegraphType type)`은 최종적으로 시간 인자가 없는 형태로 축소한다.

권장 최종 형태:

```csharp
protected abstract NodeState PlayTelegraph(TelegraphType type);
```

`PlayTelegraph()`가 반환한 직후 같은 BT 평가에서 공격 애니메이션 전환을 요청하는 현재 순서는 유지한다. 따라서 구조를 다시 작성하지 않고 시간 소유권만 Animator로 이동한다.

## 6. 상태 전환 및 정리 규칙

| 상황 | 필수 처리 |
| --- | --- |
| 정상 공격 시작 | 진행도 `0`, 정점 플래그 `false`, 링 표시 |
| 정점 도달 | 진행도 `1` 고정, SlowMo 1회, 패링 활성화 |
| 정상 패링 종료 | `CanParryEvent(false)`, 링 숨김 |
| 정점 전 공격 취소 | 패링을 열지 않고 즉시 링 숨김 |
| 정점 후 공격 취소 | 패링을 닫고 링 숨김 |
| 패링 성공 | 기존 `Parryed()` 처리와 함께 패링 종료 및 링 정리 |
| BT 상태 재진입 | 이전 신호를 정리한 뒤 새 신호를 `0`부터 시작 |
| 보스 `OnDisable`/사망 | 패링 이벤트를 `false`로 만들고 모든 사전신호 연출 중지 |

`LogicInit()`, 패링 성공 경로, 보스 비활성화 경로에서 `StopSignal()`이 보장되어야 한다. 이는 BT가 매 프레임 재평가되거나 공격이 중간에 끊겨도 이전 공격의 링과 패링 상태가 남지 않게 한다.

## 7. Animator 전환 시 안전 규칙

Animator 전환 블렌딩 중에는 float 커브 값도 섞일 수 있다. 다음 규칙으로 시각적 역행과 이전 공격 값 유입을 방지한다.

1. `ParryTelegraphProgress` Animator 파라미터 기본값을 `0`으로 둔다.
2. 모든 패링 공격 클립의 첫 커브 값을 `0`으로 둔다.
3. `BeginSignal()`은 내부 표시 진행도를 반드시 `0`으로 초기화한다.
4. 한 공격 안에서는 `max(previousProgress, sampledProgress)`를 사용해 링이 다시 벌어지지 않게 한다.
5. `CanParryEvent(true)`를 받으면 샘플 값과 무관하게 진행도를 `1`로 고정한다.
6. 정점 이후에는 커브 값이 감소해도 링을 다시 펼치지 않는다.

전환 블렌딩이 너무 길어 정점 전에 공격 모션이 늦게 보이는 경우에는 코드 시간을 보정하지 않고 Animator 전환 시간을 조정한다. 시각과 판정의 기준을 다시 분리하지 않는다.

## 8. 직렬화 데이터 마이그레이션

현재 `Cinderella_Patterns`의 `A_telegraphTime`, `B_telegraphTime`, 궁극기별 telegraph 시간은 prefab/scene에 직렬화되어 있을 수 있다. 한 번에 삭제하지 않는다.

1. 1차 변경에서는 필드를 유지하되 새 동기화 로직에서는 사용하지 않는다.
2. Inspector 혼동을 막기 위해 필요하면 `[HideInInspector]`로 숨기고 마이그레이션 주석을 남긴다.
3. 모든 패링 공격 클립에 커브가 적용되고 관련 prefab/scene이 검증된 뒤 별도 정리 작업으로 제거한다.

새로운 공격별 초 단위 설정이나 별도 타이밍 프로필은 추가하지 않는다.

## 9. 실패 안전 동작

- Animator 또는 진행도 파라미터가 없으면 사전신호를 시작하지 않고 경고를 한 번만 남긴다.
- 커브가 정점까지 도달하지 않더라도 `EnableParry()` 이벤트를 받으면 링을 `1`로 스냅해 판정과 시각을 같은 프레임에 맞춘다.
- `EnableParry()` 이벤트 없이 커브만 `1`에 도달하면 시각 정점까지만 표현하고 패링은 열지 않는다. 게임 판정은 항상 기존 Animation Event가 권한을 가진다.
- `EnableParry()`가 중복 호출되어도 SlowMo와 중심점 연출은 공격당 한 번만 실행한다.
- 패링 종료 이벤트가 누락된 경우에도 공격 종료, `LogicInit()`, `OnDisable()`에서 강제로 닫는다.

## 10. 적용 순서

1. Animator에 `ParryTelegraphProgress` float 파라미터를 추가한다.
2. 대표 공격 하나에 `0 -> 1` 커브를 추가하고 `EnableParry` 이벤트와 정점 키를 맞춘다.
3. `RingDrawer`를 Animator 진행도 표시 방식으로 변경한다.
4. `PlayTelegraph`가 시간 대신 Animator 참조로 신호를 준비하도록 변경한다.
5. 패링 성공, 공격 취소, `LogicInit()`, `OnDisable()` 정리 경로를 연결한다.
6. 대표 공격으로 동기화와 취소 동작을 검증한다.
7. 나머지 패링 공격 클립으로 커브를 확장한다.
8. 모든 prefab/scene 검증 후 기존 telegraph 시간 필드 제거 여부를 결정한다.

## 11. 검증 기준

### 기능 검증

- 링 수축 중 패링 입력은 보스 패링으로 성공하지 않는다.
- 링 정점 프레임부터 패링 입력이 성공한다.
- `DisableParry` 이벤트 이후에는 패링이 성공하지 않는다.
- 패링 성공 시 공격 콜라이더와 패링 윈도우가 정상적으로 정리된다.
- 공격 취소 또는 상태 전환 후 링이 남지 않는다.

### 동기화 검증

- 기본 애니메이션 속도에서 정점과 `EnableParry`가 같은 프레임이다.
- `anim.speed`를 `0.5`, `1`, `2`로 바꿔도 같은 애니메이션 키에서 정점에 도달한다.
- 분노 배속에서도 별도 telegraph 시간 수정이 필요 없다.
- `Time.timeScale` 변화 중에도 링과 애니메이션의 상대 진행도가 유지된다.
- Animator 전환 중 링이 뒤로 벌어지거나 이전 공격의 정점 값에서 시작하지 않는다.

### 프로젝트 검증

- 대표 보스 prefab과 관련 scene의 직렬화 참조가 유지된다.
- `dotnet restore Assembly-CSharp.csproj` 후 `dotnet build Assembly-CSharp.csproj`가 성공한다.
- Unity Play Mode에서 Animation Event, 커브 값, 패링 성공 프레임을 함께 확인한다.

## 12. 완료 조건

- 패링 가능 공격의 링 정점과 `EnableParry` 키가 Animation Clip 안에서 일치한다.
- 링 정점 전에는 보스 패링 윈도우가 열리지 않는다.
- 패턴 스크립트에서 telegraph 초 값을 눈대중으로 조절할 필요가 없다.
- 애니메이션 속도를 변경해도 사전신호와 패링 키가 자연스럽게 동기화된다.
- 기존 BT, 패링 판정, prefab-facing API의 동작 의도가 유지된다.

## 13. 범위 제외

- 플레이어 패링 입력 윈도우 길이 변경
- 패링 성공 판정 공식 변경
- 공격 콜라이더 키프레임 변경
- 새로운 Manager, Singleton 또는 전역 타이밍 시스템 도입
- 패링과 무관한 `LaserSight` 사전신호 변경
