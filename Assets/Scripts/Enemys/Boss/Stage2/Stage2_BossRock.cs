using System.Collections;
using UnityEngine;

// Stage2 메인보스 3스킬의 돌 하나를 제어한다.
// 바닥 아래에서 상승하고, 잠시 유지된 뒤 다시 내려가며 사라진다.
public class Stage2_BossRock : MonoBehaviour
{
    private float damage;
    private LayerMask playerLayer;
    private Vector2 hitboxSize;
    private float riseHeight;
    private float riseDuration;
    private float holdDuration;
    private float fallDuration;
    private Vector2 damageSource;
    private bool hasHitPlayer;

    public void Init(
        float damage,
        LayerMask playerLayer,
        Vector2 hitboxSize,
        float riseHeight,
        float riseDuration,
        float holdDuration,
        float fallDuration,
        Vector2 damageSource)
    {
        this.damage = damage;
        this.playerLayer = playerLayer;
        this.hitboxSize = hitboxSize;
        this.riseHeight = Mathf.Max(0f, riseHeight);
        this.riseDuration = Mathf.Max(0.01f, riseDuration);
        this.holdDuration = Mathf.Max(0f, holdDuration);
        this.fallDuration = Mathf.Max(0.01f, fallDuration);
        this.damageSource = damageSource;

        StartCoroutine(RockRoutine());
    }

    private IEnumerator RockRoutine()
    {
        Vector3 raisedPosition = transform.position;
        Vector3 hiddenPosition = raisedPosition - Vector3.up * riseHeight;
        transform.position = hiddenPosition;

        yield return MoveRock(hiddenPosition, raisedPosition, riseDuration);

        float timer = 0f;
        while (timer < holdDuration)
        {
            TryHitPlayer();
            timer += Time.deltaTime;
            yield return null;
        }

        yield return MoveRock(raisedPosition, hiddenPosition, fallDuration);
        Destroy(gameObject);
    }

    private IEnumerator MoveRock(Vector3 from, Vector3 to, float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            transform.position = Vector3.Lerp(from, to, Mathf.Clamp01(timer / duration));
            TryHitPlayer();
            yield return null;
        }

        transform.position = to;
    }

    private void TryHitPlayer()
    {
        if (hasHitPlayer)
            return;

        Collider2D hit = Physics2D.OverlapBox(transform.position, hitboxSize, 0f, playerLayer);
        if (hit == null)
            return;

        IDamageable target = hit.GetComponentInParent<IDamageable>();
        if (target == null)
            return;

        target.TakeDamage(damage, damageSource, false, false);
        hasHitPlayer = true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.gray;
        Gizmos.DrawWireCube(transform.position, hitboxSize);
    }
}
