using UnityEngine;
using System.Collections;

public class IdleShake : MonoBehaviour
{
    public float shakeAmount = 0.03f;
    public float shakeDuration = 0.25f;

    public float minInterval = 6f;
    public float maxInterval = 12f;

    private Vector3 originalPos;

    void Start()
    {
        originalPos = transform.localPosition;
        StartCoroutine(ShakeRoutine());
    }

    IEnumerator ShakeRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));

            float timer = 0f;

            while (timer < shakeDuration)
            {
                timer += Time.deltaTime;

                Vector2 offset = Random.insideUnitCircle * shakeAmount;
                transform.localPosition = originalPos + (Vector3)offset;

                yield return null;
            }

            transform.localPosition = originalPos;
        }
    }
}