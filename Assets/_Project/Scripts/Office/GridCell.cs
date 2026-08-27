namespace GameDevStudio.Office
{
    public enum CellState { Empty, Occupied }

    /// <summary>Single tile data. Extended later for furniture, NavMesh baking, etc.</summary>
    public class GridCell
    {
        public int       X     { get; }
        public int       Z     { get; }
        public CellState State { get; set; }

        public GridCell(int x, int z)
        {
            X = x; Z = z; State = CellState.Empty;
        }
    }
}
