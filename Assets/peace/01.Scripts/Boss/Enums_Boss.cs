using System;

/// <summary>
/// [시스템 아키텍처: 계층형 상태(FSM) 정의]
/// 보스의 최상위 생명주기 및 행동 트리의 Root를 결정하는 상태(State)입니다.
/// </summary>
public enum State
{
    Spawn, Idle, Attack, Ultimate, Groggy, Defeated
}

/// <summary>
/// [테스트 및 기획 데이터 주입용 (Dependency Injection)]
/// 인스펙터에서 보스의 패턴을 강제하거나(A, B, C) 무작위(ALL)로 돌릴 수 있게 만드는 디버깅/기획용 제어 스위치입니다.
/// </summary>
public enum ExcuteAttackType_InGame { A, B, C, ALL }

/// <summary>
/// [논리 상태 (Logical State)]
/// 실제 코드 내부(C#)에서 보스가 어떤 공격 페이즈에 있는지 식별하는 타입입니다.
/// (예: C_2는 점프 공격(C) 중 파동이 터지는 특수 하위 페이즈를 의미함)
/// </summary>
public enum AttackType { A, B, C, C_2, D }

/// <summary>
/// [데이터 브릿지 (Data Bridge) & 매직 스트링 제거]
/// C#의 엄격한 타입(Enum) 세계와 유니티 인스펙터/딕셔너리의 유연한 문자열(String)/인덱스(Int) 세계를 
/// 안전하게 연결해주는 정적 매핑(Static Mapping) 래퍼(Wrapper) 클래스입니다.
/// </summary>
public static class BossAttackIds
{
    // 1. 매직 스트링 상수화: 오타로 인한 NullReferenceException을 컴파일 단계에서 원천 차단합니다.
    public const string A = "A";
    public const string B = "B";
    public const string C = "C";
    public const string C_2 = "C_2";
    public const string D = "D";

    /// <summary>
    /// [Enum -> String 변환]
    /// C# 8.0의 switch expression을 사용하여, 내부적으로 최적화된 점프 테이블(Jump Table)로 컴파일됩니다.
    /// 로직에서 판별한 AttackType을 딕셔너리 키(String)로 변환할 때 사용됩니다.
    /// </summary>
    public static string FromAttackType(AttackType type)
    {
        return type switch
        {
            AttackType.A => A,
            AttackType.B => B,
            AttackType.C => C,
            AttackType.C_2 => C_2,
            AttackType.D => D,
            _ => string.Empty
        };
    }

    /// <summary>
    /// [String -> Int 변환 (하위 호환성)]
    /// 과거 List 기반의 인덱스 매칭을 사용하던 레거시 시스템을 지원하기 위한 파서(Parser)입니다.
    /// </summary>
    public static int ToDefaultIndex(string attackId)
    {
        return attackId switch
        {
            A => 0,
            B => 1,
            C => 2,
            C_2 => 3,
            D => 4,
            _ => -1 // 유효하지 않은 값에 대한 안전한 방어(Fail-safe) 반환
        };
    }

    /// <summary>
    /// [Int -> String 변환]
    /// 레거시 인덱스 번호를 기반으로 신규 시스템의 String Key를 복원해냅니다.
    /// </summary>
    public static string FromDefaultIndex(int index)
    {
        return index switch
        {
            0 => A,
            1 => B,
            2 => C,
            3 => C_2,
            4 => D,
            _ => string.Empty
        };
    }
}

/// <summary>
/// [시각 상태 (Visual State)]
/// 유니티 Animator의 파라미터(Integer)에 직접 주입될 해시/정수 매핑용 Enum입니다.
/// 논리 상태(AttackType)와 시각 상태(Animation)를 분리(Decoupling)하여 단일 책임 원칙을 준수합니다.
/// </summary>
public enum Animation
{
    Idle,
    AttackA, AttackB, AttackC,
    Ultimate1, Ultimate2, Ultimate3,
    Chase,
    Parry,
    Groggy,
    Walking
}

/// <summary>
/// [1D 벡터 최적화 매핑]
/// 횡스크롤 게임에서 보스의 X축 벡터(1, -1) 연산을 돕기 위한 직관적인 방향 상태입니다.
/// </summary>
public enum Facing { Left, Right }