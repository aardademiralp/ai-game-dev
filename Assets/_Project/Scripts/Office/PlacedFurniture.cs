using UnityEngine;

namespace GameDevStudio.Office
{
    /// <summary>
    /// Component attached to instantiated furniture GameObjects on the office grid.
    /// Stores persistent metadata including catalog ID, grid cell origin, rotation, and variant.
    /// </summary>
    public class PlacedFurniture : MonoBehaviour
    {
        [Header("Persistence Metadata")]
        public string FurnitureId;
        public int GridX;
        public int GridZ;
        public int Rotation;       // 0 = 0°, 1 = 90°, 2 = 180°, 3 = 270°
        public int VariantIndex;   // Level or variant index
        public string InstanceId;  // Unique instance identifier (e.g., "FURN_1_2")

        public FurnitureData Data { get; set; }
    }
}
