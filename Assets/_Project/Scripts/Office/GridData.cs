using UnityEngine;

namespace GameDevStudio.Office
{
    /// <summary>
    /// Pure C# grid model — no MonoBehaviour.
    /// Owns the cell array and handles coordinate conversion.
    /// </summary>
    public class GridData
    {
        public int Width  { get; }
        public int Height { get; }

        private readonly GridCell[,] _cells;

        public GridData(int width, int height)
        {
            Width  = width;
            Height = height;
            _cells = new GridCell[width, height];

            for (int x = 0; x < width;  x++)
            for (int z = 0; z < height; z++)
                _cells[x, z] = new GridCell(x, z);
        }

        public bool     IsValid (int x, int z) => x >= 0 && x < Width && z >= 0 && z < Height;
        public GridCell GetCell (int x, int z) => IsValid(x, z) ? _cells[x, z] : null;

        /// <summary>World-space centre of a cell given the grid's bottom-left origin.</summary>
        public Vector3 CellToWorld(int x, int z, Vector3 origin) =>
            new Vector3(origin.x + x + 0.5f, origin.y, origin.z + z + 0.5f);

        /// <summary>
        /// Converts a world position to grid coordinates.
        /// Returns false when the position is outside the grid.
        /// </summary>
        public bool WorldToCell(Vector3 worldPos, Vector3 origin, out int x, out int z)
        {
            x = Mathf.FloorToInt(worldPos.x - origin.x);
            z = Mathf.FloorToInt(worldPos.z - origin.z);
            return IsValid(x, z);
        }
    }
}
