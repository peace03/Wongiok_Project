using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(LineRenderer))]
public class RingDrawer : MonoBehaviour
{
    [SerializeField] private RingDrawerSimple ringDrawerSimple;

    [Header("중심점 연출")]
    [SerializeField] private LineRenderer centerDotLine;
    [SerializeField] private int blinkCount = 3;
    [SerializeField] private Color blinkColor = new Color(4f, 4f, 4f);
    [SerializeField] private Color normalColor = new Color(1.8f, 1.3f, 0.4f);

    [Header("해상도 및 굵기")]
    [SerializeField] private int segments = 48;
    [SerializeField] private float width = 0.08f;

    [Header("패링 수축 설정")]
    [SerializeField] private float startRadius = 3f;
    [SerializeField] private float contactRadius = 0.5f;  // ★ 중심 원 크기에 맞춰
    [SerializeField] private Color goldColor = new Color(1.8f, 1.3f, 0.4f);

    [Header("닿는 순간 연출")]
    [SerializeField] private Transform centerDot;
    [SerializeField] private SpriteRenderer centerDotSprite; // 색 번쩍용 (없으면 비워둬)
    [SerializeField] private float dotFlashScale = 1.8f;
    [SerializeField] private Color flashColor = new Color(3f, 2.5f, 1.5f); // 닿을 때 색 (강하게)

    [Tooltip("패링 정점 도달 시 일시적인 엔진 시간 왜곡 여부")]
    [SerializeField] private bool useSlowMo = true;
    [SerializeField] private float slowMoScale = 0.2f;
    [SerializeField] private float slowMoDuration = 0.15f;

    private LineRenderer lr;
    private float radius;
    private bool isPlaying = false;   // 중복 방지

    public void Init()
    {
        lr = GetComponent<LineRenderer>();
        lr.loop = true;
        lr.useWorldSpace = false;
        lr.widthMultiplier = width;
        lr.positionCount = segments;
        radius = startRadius;
        ringDrawerSimple.Init();
        gameObject.SetActive(false); //끄고 시작
    }

    //void Update()
    //{
    //    if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
    //        PlaySignal(0.35f);
    //}

    public void PlaySignal(float windowDuration)
    {
        if (isPlaying) return;        // ★ 재생 중이면 무시 (슬로모션 꼬임 방지)
        ringDrawerSimple.DrawCircle();
        DrawCircle();
        StopAllCoroutines();
        StartCoroutine(Sequence(windowDuration));
    }

    private IEnumerator Sequence(float dur)
    {
        isPlaying = true;

        // 1) 수축
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = t / dur;
            radius = Mathf.Lerp(startRadius, contactRadius, p);
            DrawCircle();
            float bright = Mathf.Lerp(0.6f, 1.4f, p);
            lr.startColor = lr.endColor = goldColor * bright;
            yield return null;
        }
        radius = contactRadius;
        DrawCircle();
        Debug.Log("정점 도달! 여기서 깜빡+슬로모션 시작");   // ← 이 줄 추가

        // 2) 닿는 순간 = 정점
        if (centerDot != null) StartCoroutine(DotFlash());


        // 2) 닿는 순간 = 정점 (번쩍 + 슬로모션 동시에)
        if (centerDot != null) StartCoroutine(DotFlash());

        if (useSlowMo)
        {
            EventBus<SlowMoEvent>.Publish(new SlowMoEvent(slowMoScale, slowMoDuration));
            //Time.timeScale = slowMoScale;
            //yield return new WaitForSecondsRealtime(slowMoDuration);
            //Time.timeScale = 1f;       // ★ 무조건 1로 복구
        }

        yield return new WaitForSecondsRealtime(slowMoDuration);

        isPlaying = false;
        gameObject.SetActive(false);
    }

    private IEnumerator DotFlash()
    {
        if (centerDotLine == null) yield break;

        float blinkDur = 0.08f;

        for (int i = 0; i < blinkCount; i++)
        {
            centerDotLine.startColor = centerDotLine.endColor = blinkColor;
            yield return new WaitForSecondsRealtime(blinkDur);

            centerDotLine.startColor = centerDotLine.endColor = normalColor;
            yield return new WaitForSecondsRealtime(blinkDur);
        }
    }


    void DrawCircle()
    {
        for (int i = 0; i < segments; i++)
        {
            float ang = (float)i / segments * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(ang) * radius, Mathf.Sin(ang) * radius, 0f));
        }
    }
}