[System.Serializable]
public class PlayerStats
{
    public int attack;
    public float attackSpeed;

    public float critChance;
    public float critDamage;

    public float moveSpeed;

    public int defense;
    public int hp;

    public void Clear()
    {
        attack = 0;
        attackSpeed = 0;

        critChance = 0;
        critDamage = 0;

        moveSpeed = 0;

        defense = 0;
        hp = 0;
    }

    public void Add(ItemData item)
    {
        attack += item.attack;
        attackSpeed += item.attackSpeed;

        critChance += item.critChance;
        critDamage += item.critDamage;

        moveSpeed += item.moveSpeed;

        defense += item.defense;
        hp += item.hp;
    }
}