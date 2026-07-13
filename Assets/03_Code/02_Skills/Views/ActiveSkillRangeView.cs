using UnityEngine;
using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public class RangeView
{
    [Header(" - 범위 보기(구)")]
    [SerializeField] private bool viewS;
    [Header(" - 범위 보기(부채꼴)")]
    [SerializeField] private bool viewH;
    [Header(" - 색깔")]
    [SerializeField] private Color viewColor;
    [Header(" - 반지름")]
    [SerializeField] private float radius;
    [Header(" - 각도")]
    [SerializeField] private float angle;

    public bool ViewS => viewS;
    public bool ViewH => viewH;
    public Color ViewColor => viewColor;
    public float Radius => radius;
    public float Angle => angle;
}

public class ActiveSkillRangeView : MonoBehaviour
{
    [Header("※ 감지 대상 : 범위 안에 콜라이더의 일부가 들어오면 대상이 됨 ※")]
    [SerializeField] private List<RangeView> rangeViews = new();

    private void OnDrawGizmosSelected()
    {
        foreach(var rangeView in rangeViews)
        {
            if(rangeView.ViewS)
            {
                Gizmos.color = rangeView.ViewColor;
                Gizmos.DrawWireSphere(transform.position, rangeView.Radius);
            }

            if(rangeView.ViewH)
            {
#if UNITY_EDITOR
                Handles.color = rangeView.ViewColor;
                Handles.DrawWireArc(transform.position, transform.right,
                    Quaternion.AngleAxis(-rangeView.Angle * 0.5f, transform.right) * transform.forward,
                                                                        rangeView.Angle, rangeView.Radius);
                Handles.DrawLine(transform.position,
                    transform.position + Quaternion.AngleAxis(-rangeView.Angle * 0.5f, transform.right)
                                                                    * transform.forward * rangeView.Radius);
                Handles.DrawLine(transform.position,
                    transform.position + Quaternion.AngleAxis(rangeView.Angle * 0.5f, transform.right)
                                                                    * transform.forward * rangeView.Radius);
#endif
            }
        }
    }
}