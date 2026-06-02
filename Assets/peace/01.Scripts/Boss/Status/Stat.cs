using System;
using UnityEngine;

[Serializable]
public class Stat //StatModifier 패턴 적용
{
    [SerializeField] private float baseValue;

    private float additive; //Serializable의 직렬화 대상이 아닌경우 0으로 세팅해버림
    private float multiplier;

    public float FinalValue => (baseValue + additive) * multiplier;

    public Stat(float baseValue = 0f)
    {
        this.baseValue = baseValue;
    }

    public void SetBaseValue(float value)
    {
        baseValue = value;
    }

    public void AddValue(float value)
    {
        additive += value;
    }

    public void AddMultiplier(float value)
    {
        multiplier += value;
    }

    public void ResetModifiers()
    {
        additive = 0f;
        multiplier = 1f;
    }
}
