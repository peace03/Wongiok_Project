using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RingDrawerSimple : MonoBehaviour
{
    [SerializeField] private int segments = 24;
    [SerializeField] private float radius = 0.3f;
    [SerializeField] private float width = 0.3f;
    private LineRenderer lr;

    public void Init()
    {
        lr = GetComponent<LineRenderer>();
        lr.loop = true;
        lr.useWorldSpace = false;
        lr.widthMultiplier = width;
        lr.positionCount = segments;
        DrawCircle();
    }

    public void DrawCircle()
    {
        for (int i = 0; i < segments; i++)
        {
            float ang = (float)i / segments * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(ang) * radius, Mathf.Sin(ang) * radius, 0f));
        }
    }
}