using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "ScriptableObjects/EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("기본 스탯")]
    public float maxHp = 20f;           // 최대 체력: 0이 되면 오브젝트 비활성화
    public float damage = 5f;          // 공격력: 플레이어 피격 시 차감할 체력량
    public float moveSpeed = 2f;       // 기본 이동 속도: 순찰(Wander) 상태일 때 속도
    public float chaseSpeed = 3.5f;    // 추격 속도: 플레이어 발견 후 쫓아갈 때 속도

    [Header("인식 범위")]
    public float detectRange = 5f;     // 최초 감지 범위: 평소 상태에서 플레이어를 찾는 원형 사거리
    public float chaseRange = 10f;     // 추격 유지 범위: 추격 중 플레이어가 이 거리보다 멀어지면 포기
    [Range(0, 360)]
    public float viewAngle = 180f;     // 시야 각도: 몬스터가 전방 몇 도 안의 플레이어를 볼 수 있는지 설정

    [Header("AI 반응 및 지연")]
    public float idleDuration = 2.0f;  // 대기 시간: 순찰 중 멈췄을 때 머무르는 시간
    public float reactionTime = 0.5f;  // 반응 지연: 플레이어 첫 발견 시 멍하니 서 있는 시간 (난이도 조절용)
    public float positionUpdateDelay = 0.5f; // 추격 갱신 지연: 플레이어 위치를 계산하는 주기 (지연이 클수록 몬스터가 둔함)

    [Header("낭떠러지 제어")]
    public float checkDistance = 0.7f;  // 바닥 감지 거리: 발밑으로 쏘는 레이(Ray)의 길이 (경사로나 층고에 따라 조절)
    public float groundCheckOffset = 0.5f; // 바닥 감지 위치: 몬스터 중심에서 발밑으로 레이를 쏘는 위치 (발 위치에 맞게 조절)
    public float wanderDuration = 0.7f; // 강제 이동 시간: 낭떠러지 발견 후 반대 방향으로 이동하는 최소 시간

    [Header("공격 설정")]
    public float attackSpeed = 1f;      // 공격 속도: 초당 공격 횟수 (1이면 1초에 1번, 2면 0.5초에 1번)
    public float attackRange = 1.5f;    // 공격 박스 너비(X): 전방으로 생성될 공격 사각형의 가로 크기
    public float attackHeight = 1.5f;   // 공격 박스 높이(Y): 공격 사각형의 세로 크기 (점프한 플레이어 격추 가능 여부 결정)
    public float attackHeightOffset = 0f; // 공격 박스의 시작 높이 : 몬스터 중심에서 공격 박스가 얼마나 위에 생성될지 설정
    public float attackOffset = 0.5f;   // 공격 박스 위치: 몬스터 중심에서 얼마나 앞에서 공격이 시작될지 설정

    [Header("공격 상세 시간")]
    public float preAttackDelay = 0.3f;  // 선딜레이: 공격 시작 후 실제 판정이 발생하기까지의 예비 동작 시간
    public float postAttackDelay = 0.4f; // 후딜레이: 공격 판정 종료 후 다시 이동/추격이 가능해질 때까지의 경직 시간

    [Header("원거리 전용")]
    public GameObject projectilePrefab; // 발사체: 원거리 몬스터일 경우 생성할 화살/마법구 등의 프리팹
}