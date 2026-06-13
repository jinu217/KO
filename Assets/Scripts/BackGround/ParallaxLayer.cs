using UnityEngine;

// 플레이어 움직임에 따른 뒷배경 움직임 기능

public class ParallaxLayer : MonoBehaviour
{
    public Transform cameraTransform;

    [Header("X축 패럴랙스")]
    public float parallaxX = 0.2f;

    [Header("Y축 패럴랙스")]
    public float parallaxY = 0.05f;

    private Vector3 startPosition;
    private Vector3 cameraStartPosition;

    void Start()
    {
        startPosition = transform.position;
        cameraStartPosition = cameraTransform.position;
    }

    void LateUpdate()
    {
        Vector3 cameraOffset = cameraTransform.position - cameraStartPosition;

        transform.position = new Vector3(
            startPosition.x + cameraOffset.x * parallaxX,
            startPosition.y + cameraOffset.y * parallaxY,
            startPosition.z
        );
    }
}