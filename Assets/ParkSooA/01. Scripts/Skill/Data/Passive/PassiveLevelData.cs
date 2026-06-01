using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
// 패시브 레벨 정보
public class PassiveLevelData
{
    [SerializeField] private PASSIVE_TRIGGER_TYPE triggerType;      // 발동 조건
    [SerializeField] private List<StatAdjustment> appliedStats;     // 바꿀 스탯 정보들
}