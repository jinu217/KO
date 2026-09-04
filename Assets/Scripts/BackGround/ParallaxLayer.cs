using UnityEngine;

// 플레이어 움직임에 따른 뒷배경 움직임 기능

public class ParallaxLayer : MonoBehaviour
{
    public Transform playerTransform;

    [Header("X축 패럴랙스")]
    public float parallaxX = 0.2f;

    [Header("Y축 패럴랙스")]
    public float parallaxY = 0.05f;

    private Vector3 startPosition;
    private Vector3 playerStartPosition;

    void Start()
    {
        startPosition = transform.position;

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                Debug.LogError("ParallaxLayer: Player를 찾을 수 없습니다.", this);
                enabled = false;
                return;
            }
        }

        playerStartPosition = playerTransform.position;
    }

    void LateUpdate()
    {
        Vector3 playerOffset = playerTransform.position - playerStartPosition;

        transform.position = new Vector3(
            startPosition.x - playerOffset.x * Mathf.Abs(parallaxX),
            startPosition.y - playerOffset.y * Mathf.Abs(parallaxY),
            startPosition.z
        );
    }
}