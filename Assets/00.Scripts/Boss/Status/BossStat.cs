using System;
using UnityEngine;

[Serializable]
public class BossStat
{
    [SerializeField] private float baseValue;

    private float additive;
    private float multiplier = 1f;

    public float FinalValue => (baseValue + additive) * multiplier;

    public BossStat(float baseValue = 0f)
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
