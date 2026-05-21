using UnityEngine;

// [타일맵 함정]
// - Tilemap Collider 2D / Composite Collider 2D와 함께 사용
// - 플레이어가 함정 타일맵에 닿아 있는 동안 tickInterval마다 데미지
// - 첫 접촉 시 즉시 데미지
// - 쿨타임은 플레이어 부재 중에도 계속 진행
public class TrapTilemap : MonoBehaviour
{
    [Header("데미지 설정")]
    [SerializeField] private float damage = 5f;
    [SerializeField] private float tickInterval = 0.5f;

    private PlayerController player;
    private bool playerOnTrap;
    private float lastDamageTime = float.NegativeInfinity;

    private void Update()
    {
        if (!playerOnTrap)
            return;

        if (player == null)
            return;

        if (Time.time < lastDamageTime + tickInterval)
            return;

        player.TakeDamageEnvironmental(damage);
        lastDamageTime = Time.time;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player"))
            return;

        player = collision.gameObject.GetComponentInParent<PlayerController>();
        playerOnTrap = true;

        if (player != null)
        {
            player.TakeDamageEnvironmental(damage);
            lastDamageTime = Time.time;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player"))
            return;

        playerOnTrap = false;
        player = null;
    }
}