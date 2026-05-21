using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    public ItemData weapon;
    public ItemData head;
    public ItemData body;
    public ItemData leftShoulder;
    public ItemData rightShoulder;
    public ItemData leftHand;
    public ItemData rightHand;
    public ItemData leftFoot;
    public ItemData rightFoot;


    public void Equip(ItemData item)
    {
        switch(item.equipType)
        {
            case EquipmentType.Weapon:
                weapon=item;
                break;
                
            case EquipmentType.Head:
                head=item;
                break;

            case EquipmentType.Body:
                body=item;
                break;

            case EquipmentType.LeftShoulder:
                leftShoulder=item;
                break;

            case EquipmentType.RightShoulder:
                rightShoulder=item;
                break;

            case EquipmentType.LeftHand:
                leftHand=item;
                break;

            case EquipmentType.RightHand:
                rightHand=item;
                break;

            case EquipmentType.LeftFoot:
                leftFoot=item;
                break;

            case EquipmentType.RightFoot:
                rightFoot=item;
                break;
        }
    }
}