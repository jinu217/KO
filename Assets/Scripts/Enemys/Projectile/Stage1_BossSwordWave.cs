using UnityEngine;

public sealed class Stage1_BossSwordWave : MonoBehaviour, IBossSpawnedSkillObject
{
    [SerializeField] private float speed = 9f;
    [SerializeField] private float lifeTime = 3f;
    private float damage;
    private LayerMask playerLayer;
    private int direction;
    private bool hasHit;

    public void Init(int direction, float damage, LayerMask playerLayer, float sizeMultiplier = 1f)
    {
        this.direction = direction == 0 ? 1 : direction;
        this.damage = damage;
        this.playerLayer = playerLayer;
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
        target.TakeDamage(damage, transform.position, false, false);
        Destroy(gameObject);
    }
}
