using UnityEngine;
using System.Collections;

public class Bullet : MonoBehaviour
{
    private readonly WaitForSeconds lifeTime = new(5f);

    private readonly float speed = 10f;

    private void OnEnable()
    {
        StartCoroutine(LifeCycle());
    }

    private void FixedUpdate() => transform.position += speed * Time.fixedDeltaTime * transform.forward;

    private IEnumerator LifeCycle()
    {
        yield return lifeTime;
        gameObject.SetActive(false);
    }
}