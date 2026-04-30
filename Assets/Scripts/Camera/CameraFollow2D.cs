using UnityEngine;

// [카메라 - 2D 추적 전용] 플레이어를 자연스럽게 따라가는 스무스 카메라
public class CameraFollow2D : MonoBehaviour
{
    [Header("대상")]
    public Transform target; // 따라갈 대상(플레이어)

    [Header("추적 설정")]
    public Vector3 offset = new Vector3(0f, 1.2f, -10f); // 대상 기준 카메라 오프셋
    [Range(0.01f, 1f)]
    public float smoothTime = 0.2f; // 작을수록 빠르게 붙고, 클수록 부드럽게 따라감

    [Header("축 잠금")]
    public bool lockX = false; // X축 고정 여부
    public bool lockY = false; // Y축 고정 여부

    private Vector3 velocity = Vector3.zero; // SmoothDamp 내부 속도 누적값

    void Start()
    {
        // target 미할당 시 Player 태그 자동 탐색
        if (target == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) target = player.transform;
        }
    }

    // 플레이어 이동이 끝난 뒤 카메라를 보정하기 위해 LateUpdate 사용
    void LateUpdate()
    {
        if (target == null) return;

        // 목표 위치 계산(오프셋 포함)
        Vector3 desiredPosition = target.position + offset;

        // 축 잠금 옵션
        if (lockX) desiredPosition.x = transform.position.x;
        if (lockY) desiredPosition.y = transform.position.y;

        // 부드럽게 이동
        Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);

        // 카메라 Z는 2D 기본 거리 유지
        smoothedPosition.z = offset.z;

        transform.position = smoothedPosition;
    }
}
