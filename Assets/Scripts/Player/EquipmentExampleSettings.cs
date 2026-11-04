using System;
using UnityEngine;

// Metadata for a static editable example; no runtime behavior or animation.
public sealed class EquipmentExampleSettings : MonoBehaviour
{
    [Serializable]
    public class Part
    {
        public ItemData item;
        public SpriteRenderer renderer;
        public string part;
        public Vector3 baseRootPosition;
    }
    public Part[] parts = new Part[0];
}
