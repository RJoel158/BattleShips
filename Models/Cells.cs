namespace Battleships_Pantoja_Saavedra.Models
{
    public enum CellState { Empty, Ship, Hit, Miss }
    public class Cell
    {
        public int X { get; set; }
        public int Y { get; set; }
        public CellState State { get; set; } = CellState.Empty;
    }
}
