[System.Serializable]
public class PlayerStats
{
    public float maxHp = 50f;
    public float attackDamage = 10f;
    public float attackSpeed = 1f;
    public float critChance = 0.5f;
    public float critDamage = 1.5f;
    public float moveSpeed = 5f;
    public float defense = 10f;
    public float jumpForce = 15f;
    public int maxJumpCount = 1;

    public int attack
    {
        get => UnityEngine.Mathf.RoundToInt(attackDamage);
        set => attackDamage = value;
    }

    public int hp
    {
        get => UnityEngine.Mathf.RoundToInt(maxHp);
        set => maxHp = value;
    }

    public void CopyFrom(PlayerStats source)
    {
        if (source == null)
            return;

        maxHp = source.maxHp;
        attackDamage = source.attackDamage;
        attackSpeed = source.attackSpeed;
        critChance = source.critChance;
        critDamage = source.critDamage;
        moveSpeed = source.moveSpeed;
        defense = source.defense;
        jumpForce = source.jumpForce;
        maxJumpCount = source.maxJumpCount;
    }

    public void Clear()
    {
        maxHp = 0f;
        attackDamage = 0f;
        attackSpeed = 0f;
        critChance = 0f;
        critDamage = 0f;
        moveSpeed = 0f;
        defense = 0f;
        jumpForce = 0f;
        maxJumpCount = 0;
    }

    public void Add(ItemData item)
    {
        if (item == null)
            return;

        attackDamage += item.attack;
        attackSpeed += item.attackSpeed;
        critChance += item.criticalChance;
        critDamage += item.criticalDamage;
        moveSpeed += item.moveSpeed;
        defense += item.defense;
        maxHp += item.hp;
    }

    public void Remove(ItemData item)
    {
        if (item == null)
            return;

        attackDamage -= item.attack;
        attackSpeed -= item.attackSpeed;
        critChance -= item.criticalChance;
        critDamage -= item.criticalDamage;
        moveSpeed -= item.moveSpeed;
        defense -= item.defense;
        maxHp -= item.hp;
    }
}
