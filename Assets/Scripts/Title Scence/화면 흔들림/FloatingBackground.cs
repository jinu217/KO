using UnityEngine;

public class FloatingBackground : MonoBehaviour
{
    public Vector2 moveAmount = new Vector2(0.2f, 0.1f);
    public Vector2 speed = new Vector2(0.3f, 0.2f);

    private Vector3 startPos;
    private float randomOffset;

    void Start()
    {
        startPos = transform.localPosition;
        randomOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        float x = Mathf.Sin((Time.time + randomOffset) * speed.x) * moveAmount.x;
        float y = Mathf.Cos((Time.time + randomOffset) * speed.y) * moveAmount.y;

        transform.localPosition = startPos + new Vector3(x, y, 0);
    }
}