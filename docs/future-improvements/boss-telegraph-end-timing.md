# Boss Telegraph End-Timing Refactor

## 목적

현재 보스 사전신호는 공격 패턴마다 `PlayTelegraph(telegraphTime, TelegraphType.RingDrawer)`를 직접 호출하고, 각 공격마다 사전신호 지속시간을 수작업으로 맞춘다.

이 방식은 신데렐라처럼 공격 수가 적을 때는 관리 가능하지만, 루주후드처럼 공격 애니메이션과 패턴이 늘어나면 매 공격마다 "언제 사전신호를 시작해야 하는지"를 직접 계산해야 한다. 결과적으로 애니메이션 길이, 공격 속도, 타격 프레임이 바뀔 때마다 사전신호 값을 다시 조정해야 한다.

추후 개선 목표는 사전신호의 기준을 "시작 타이밍"이 아니라 "종료 타이밍"으로 바꾸는 것이다.

## 현재 구조

현재 흐름은 대략 아래와 같다.

```text
Chase
-> PlayTelegraph(수동 입력한 지속시간)
-> 공격 애니메이션 재생
-> 공격 판정
-> Recovery
```

예시:

```csharp
new Leaf(() => PlayTelegraph(A_telegraphTime, TelegraphType.RingDrawer))
new Leaf(() => PlayAnim_Speed((int)Animation.AttackA, A_attackDuration))
```

문제점:

- 공격마다 사전신호 지속시간을 직접 입력해야 한다.
- 애니메이션 길이 또는 공격 속도가 바뀌면 사전신호 타이밍이 어긋날 수 있다.
- 루주후드처럼 공격 패턴이 많아질수록 BT 조립 코드가 반복된다.
- "플레이어가 반응해야 하는 정점"이 코드 구조에 명확히 드러나지 않는다.

## 개선 방향

사전신호는 공격 준비 애니메이션이 시작될 때 자동으로 시작하고, 공격의 핵심 타이밍에 맞춰 종료되도록 한다.

중요한 기준은 "공격 패턴 전체 시작"이 아니라 "실제 공격 준비/wind-up 애니메이션 시작"이다. Chase는 플레이어와의 거리 때문에 매번 시간이 달라질 수 있으므로, Chase 시작과 동시에 사전신호를 켜면 타이밍이 불안정해질 수 있다.

추천 흐름:

```text
Chase 완료
-> 공격 준비/wind-up 애니메이션 시작
-> 사전신호 자동 시작
-> 설정된 종료 지점 도달
-> 사전신호 정점 + 불릿타임
-> 공격 판정
-> Recovery
```

## 권장 데이터 구조

`AttackTimingData`를 확장해서 공격별 타이밍을 한 곳에 모은다.

```csharp
[Serializable]
public class AttackTimingData
{
    public float telegraphEndNormalizedTime = 0.6f;
    public float telegraphEndSeconds = -1f;
    public float attackDuration = 0f;
    public float recoveryDuration = 0.3f;
    public int hitStopFrames = 0;
    public bool useTelegraphSlowMo = true;
}
```

필드 의도:

- `telegraphEndNormalizedTime`: 공격 애니메이션 기준 몇 % 지점에서 사전신호가 정점에 도달할지 결정한다.
- `telegraphEndSeconds`: 초 단위로 직접 지정하고 싶을 때 사용한다. `-1`이면 사용하지 않는다.
- `attackDuration`: `PlayAnim_Speed`처럼 공격 애니메이션을 특정 시간 안에 끝내야 할 때 사용한다.
- `recoveryDuration`: 공격 후 후딜레이 시간이다.
- `hitStopFrames`: 공격별 히트스탑을 따로 조정해야 할 때 사용한다.
- `useTelegraphSlowMo`: 해당 공격의 사전신호 정점에서 불릿타임을 사용할지 결정한다.

계산 우선순위:

```text
telegraphEndSeconds >= 0
-> 초 단위 직접 지정값 사용

그 외
-> animationLength * telegraphEndNormalizedTime 사용
```

## 권장 공통 메서드

`BossPatternBase`에 공격 애니메이션과 사전신호를 함께 실행하는 helper를 추가한다.

예상 형태:

```csharp
protected NodeState PlayTelegraphedAnim(
    int animNum,
    AttackTimingData timing,
    TelegraphType telegraphType)
```

메서드 책임:

1. 공격 애니메이션 준비 상태를 확인한다.
2. 사전신호가 아직 시작되지 않았다면 한 번만 시작한다.
3. 애니메이션 길이와 `AttackTimingData`를 기준으로 사전신호 지속시간을 계산한다.
4. 공격 애니메이션을 재생한다.
5. 애니메이션이 완료되면 `Success`를 반환한다.

주의:

- 사전신호 시작은 edge trigger 방식이어야 한다. 매 프레임 `PlaySignal`이 다시 호출되면 안 된다.
- 사전신호 수축은 현재처럼 `Time.unscaledDeltaTime` 기준을 유지한다.
- 패링 히트스탑은 계속 사전신호 불릿타임보다 우선되어야 한다.

## 루주후드 적용 방식

루주후드 공격 레퍼런스가 들어오면 새 공격부터 이 방식을 적용한다.

예상 BT:

```text
Chase or 거리 유지
-> PlayTelegraphedAnim(AttackA, aimedShotTiming, RingDrawer or LaserSight)
-> 공격 판정/투사체 생성
-> Recovery
-> StateDone
```

루주후드는 사격형 보스일 가능성이 있으므로 사전신호 타입은 공격별로 다르게 둘 수 있다.

- 조준 사격: `LaserSight`
- 근접 반격: `RingDrawer`
- 범위 공격: `GroundMarker`

## 신데렐라 적용 방식

신데렐라는 이미 동작이 거의 완성되어 있으므로 바로 전면 교체하지 않는다.

권장 순서:

1. 루주후드 신규 공격에 먼저 `PlayTelegraphedAnim` 구조를 적용한다.
2. 구조가 안정되면 신데렐라 A/B/Ultimate의 `PlayTelegraph` 직접 호출을 점진적으로 대체한다.
3. 기존 serialized field 이름은 필요하면 유지하거나 `[FormerlySerializedAs]`를 사용한다.

## 구현 시 체크리스트

- `AttackTimingData`에 사전신호 종료 기준 필드 추가
- `BossPatternBase`에 사전신호 지속시간 계산 메서드 추가
- `BossPatternBase`에 `PlayTelegraphedAnim` helper 추가
- `RingDrawer`는 기존 unscaled time 수축 방식 유지
- `TimeControlManager`의 HitStop 우선 정책 유지
- 루주후드 공격 1개에 먼저 적용
- Play Mode에서 사전신호 정점과 실제 공격 판정 타이밍 확인

## 다음 작업 요청 예시

다음에 Codex에게 아래처럼 요청하면 된다.

```text
docs/future-improvements/boss-telegraph-end-timing.md 내용을 기준으로,
루주후드 공격부터 사전신호를 종료 타이밍 기준으로 자동 시작/종료하는 구조를 구현해줘.
신데렐라는 기존 동작을 유지하고, 공통 helper와 AttackTimingData 확장부터 적용해줘.
```
