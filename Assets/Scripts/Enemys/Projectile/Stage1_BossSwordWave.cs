using UnityEngine;

public sealed class Stage1_BossSwordWave : MonoBehaviour, IBossSpawnedSkillObject
{
    [SerializeField] private float speed = 9f;
    [SerializeField] private float lifeTime = 3f;
    [SerializeField, Tooltip("일반 검기의 넉백 힘")]
    private Vector2 knockbackForce = new Vector2(4f, 1.5f);
    private float damage;
    private LayerMask playerLayer;
    private int direction;
    private bool hasHit;
    private Vector2 currentKnockbackForce;

    public void Init(int direction, float damage, LayerMask playerLayer, float sizeMultiplier = 1f, float projectileSpeed = 0f)
    {
        this.direction = direction == 0 ? 1 : direction;
        this.damage = damage;
        this.playerLayer = playerLayer;
        if (projectileSpeed > 0f) speed = projectileSpeed;
        currentKnockbackForce = sizeMultiplier > 1f ? knockbackForce * 1.35f : knockbackForce;
        transform.localScale *= sizeMultiplier;
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null) renderer.flipX = this.direction < 0;
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.position += Vector3.right * direction * speed * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit || (playerLayer.value & (1 << other.gameObject.layer)) == 0) return;
        IDamageable target = other.GetComponentInParent<IDamageable>();
        if (target == null) return;
        hasHit = true;
        target.TakeDamage(damage, transform.position, true, false);
        Rigidbody2D targetBody = other.GetComponentInParent<Rigidbody2D>();
        if (targetBody != null)
        {
            targetBody.linearVelocity = Vector2.zero;
            targetBody.AddForce(new Vector2(direction * currentKnockbackForce.x, currentKnockbackForce.y), ForceMode2D.Impulse);
        }
        Destroy(gameObject);
    }
}
