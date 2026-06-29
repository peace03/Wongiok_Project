using UnityEngine;
using System.Collections;

public class Effect : MonoBehaviour, IEffectExecuter
{
    // 실행 함수
    public void Execute(float time)
    {
        StopAllCoroutines();
        StartCoroutine(EffectRoutine(time));
    }

    private IEnumerator EffectRoutine(float time)
    {
        gameObject.SetActive(true);
        yield return new WaitForSeconds(time);
        gameObject.SetActive(false);
    }
}