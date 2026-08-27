using UnityEngine;

namespace GameDevStudio.Office
{
    public enum FurnitureType { Desk, Chair, Computer, Storage, Decoration }

    /// <summary>
    /// ScriptableObject that defines a furniture type.
    /// Create via: Assets → Create → GameDevStudio → Furniture Data
    /// Default catalog instances are also created at runtime by FurniturePlacer.
    /// </summary>
    [CreateAssetMenu(fileName = "FurnitureData", menuName = "GameDevStudio/Furniture Data")]
    public class FurnitureData : ScriptableObject
    {
        [Header("Identity")]
        public string        furnitureName = "Furniture";
        public FurnitureType furnitureType = FurnitureType.Desk;

        [Header("Grid Footprint (cells)")]
        public int sizeX = 1;   // width along X axis
        public int sizeZ = 1;   // depth along Z axis

        [Header("Economy")]
        public int price = 100;

        [Header("Visual")]
        public Color      color        = Color.white;
        public float      visualHeight = 0.6f;   // placed object height in world units
        public GameObject prefab;                // optional stylized model prefab reference
    }
}
