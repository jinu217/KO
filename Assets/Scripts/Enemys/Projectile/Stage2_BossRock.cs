using System.Collections;
using UnityEngine;

// 2스테이지 메인 보스 3스킬 전용 돌 파동.
// 생성 순서대로 솟아오른 뒤 내려가며 사라지고, 플레이어에게 한 번만 피해를 준다.
public sealed class Stage2_BossRock : MonoBehaviour, IBossSpawnedSkillObject
{
    [SerializeField, Tooltip("돌이 위로 솟는 거리")]
    private float riseHeight = 1.8f;
    [SerializeField, Tooltip("돌이 솟는 데 걸리는 시간")]
    private float riseDuration = 0.18f;
    [SerializeField, Tooltip("정점에서 머무는 시간")]
    private float holdDuration = 0.22f;
    [SerializeField, Tooltip("돌이 내려가는 데 걸리는 시간")]
    private float fallDuration = 0.28f;

    private float damage;
    private LayerMask playerLayer;
    private bool hasHit;

    public void Init(int direction, float initializedDamage, LayerMask initializedPlayerLayer, float sizeMultiplier = 1f, float projectileSpeed = 0f)
    {
        damage = initializedDamage;
        playerLayer = initializedPlayerLayer;
        transform.localScale *= Mathf.Max(0.01f, sizeMultiplier);
        StartCoroutine(Play());
    }

    private IEnumerator Play()
    {
        Vector3 peak = transform.position;
        Vector3 hidden = peak - Vector3.up * riseHeight;
        transform.position = hidden;

        yield return Move(hidden, peak, riseDuration);
        if (holdDuration > 0f)
            yield return new WaitForSeconds(holdDuration);
        yield return Move(peak, hidden, fallDuration);
        Destroy(gameObject);
    }

    private IEnumerator Move(Vector3 from, Vector3 to, float duration)
    {
        if (duration <= 0f)
        {
            transform.position = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        transform.position = to;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit || (playerLayer.value & (1 << other.gameObject.layer)) == 0)
            return;

        IDamageable target = other.GetComponentInParent<IDamageable>();
        if (target == null)
            return;

        hasHit = true;
        target.TakeDamage(damage, transform.position, true, false);
    }
}
