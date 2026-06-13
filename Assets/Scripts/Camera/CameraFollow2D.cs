using UnityEngine;

// [2D 카메라 추적]
// - 매 LateUpdate에서 SmoothDamp로 플레이어를 부드럽게 추적
// - 축 잠금(lockX/lockY)과 오프셋으로 구도 조정 가능

// [카메라 경계]
// - 카메라가 지정된 BoxCollider2D 범위를 벗어나지 않도록 제한

public class CameraFollow2D : MonoBehaviour
{
    [Header("대상")]
    [Tooltip("따라갈 대상(미연결 시 Player 태그 오브젝트 자동 탐색)")]
    public Transform target;

    [Header("추적 설정")]
    [Tooltip("대상 기준 카메라 오프셋(x, y: 화면 중심 보정 / z: 2D 기본 거리)")]
    public Vector3 offset = new Vector3(0f, 1.2f, -10f);
    [Tooltip("추적 부드러움(작을수록 빠르게 붙고 클수록 느리게 따라감)")]
    [Range(0.01f, 1f)]
    public float smoothTime = 0.2f;

    [Header("축 잠금")]
    [Tooltip("true면 카메라 X 위치를 현재값으로 고정")]
    public bool lockX = false;
    [Tooltip("true면 카메라 Y 위치를 현재값으로 고정")]
    public bool lockY = false;

    [Header("범위 시각화")]
    [Tooltip("카메라-타겟 연결선 Gizmo 표시(OnDrawGizmosSelected)")]
    public bool showTargetLineGizmo = false;

    [Header("카메라 경계")]
    public BoxCollider2D cameraBounds;

    // SmoothDamp 내부 속도 누적값(프레임 간 가속도 보간에 사용)
    private Vector3 velocity = Vector3.zero;

    // target 미연결 시 Player 태그 오브젝트 자동 탐색
    void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) target = player.transform;
        }
    }

    // 플레이어 이동이 끝난 후 카메라 위치 보정(LateUpdate로 지터 방지)
    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;

        if (lockX) desiredPosition.x = transform.position.x;
        if (lockY) desiredPosition.y = transform.position.y;

        // 카메라 경계 제한
        if (cameraBounds != null)
        {
            Bounds bounds = cameraBounds.bounds;

            Camera cam = GetComponent<Camera>();

            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;

            desiredPosition.x = Mathf.Clamp(
                desiredPosition.x,
                bounds.min.x + halfWidth,
                bounds.max.x - halfWidth
            );

            desiredPosition.y = Mathf.Clamp(
                desiredPosition.y,
                bounds.min.y + halfHeight,
                bounds.max.y - halfHeight
            );
        }

        // smoothTime 0 입력 시 튐 방지
        float safeSmoothTime = Mathf.Max(0.01f, smoothTime);

        Vector3 smoothedPosition = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref velocity,
            safeSmoothTime
        );

        // 2D 씬에서 카메라 Z는 offset.z로 고정
        smoothedPosition.z = offset.z;

        transform.position = smoothedPosition;
    }

    // 카메라-타겟 목표 위치 연결선 Gizmo 표시(showTargetLineGizmo 켤 때만)
    private void OnDrawGizmosSelected()
    {
        if (!showTargetLineGizmo || target == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, target.position + offset);
        Gizmos.DrawWireSphere(target.position + offset, 0.15f);
    }
}
