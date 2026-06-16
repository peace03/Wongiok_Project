using UnityEngine;
using System.Collections;

public class Bullet : MonoBehaviour
{
    private float speed = 10f;

    private void OnEnable()
    {
        StartCoroutine(Fire());
    }

    private void FixedUpdate() => transform.position += speed * Time.fixedDeltaTime * transform.forward;

    private IEnumerator Fire()
    {
        yield return new WaitForSeconds(10f);
        gameObject.SetActive(false);
    }
}