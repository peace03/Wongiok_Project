using System;
using UnityEngine;

// 공격별 타이밍 값을 코드 흐름과 분리하기 위한 데이터 묶음.
// 루주후드 새 공격을 만들 때 Telegraph/Attack/Recovery/Feedback 시간을 한 곳에서 읽게 만들기 위한 기반이다.
[Serializable]
public class AttackTimingData
{
    [Tooltip("공격 전 사전신호가 유지되는 시간")]
    [Min(0f)] public float telegraphDuration = 0.3f;

    [Tooltip("공격 애니메이션 또는 실제 타격 구간을 목표 시간에 맞출 때 사용하는 시간")]
    [Min(0f)] public float attackDuration = 0f;

    [Tooltip("공격 후 다음 상태로 넘어가기 전 대기 시간")]
    [Min(0f)] public float recoveryDuration = 0.3f;

    [Tooltip("패링/타격 성공 시 정지할 프레임 수. 0이면 공통 BossPatternBase 값을 사용할 수 있다.")]
    [Min(0)] public int hitStopFrames = 0;

    [Tooltip("이 공격의 사전신호 정점에서 불릿타임을 요청할지 여부")]
    public bool useTelegraphSlowMo = true;
}
