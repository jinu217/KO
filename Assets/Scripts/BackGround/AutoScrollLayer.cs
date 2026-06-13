using UnityEngine;

// 뒷배경 구름 흘러가는 기능

public class AutoScrollLayer : MonoBehaviour
{
    public float speed = 0.5f;

    private void Update()
    {
        transform.Translate(Vector3.left * speed * Time.deltaTime);
    }
}