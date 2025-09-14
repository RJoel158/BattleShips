namespace Battleships_Pantoja_Saavedra.Models
{
    public class Board
    {
        public int Size { get; set; }
        public List<Cell> Cells { get; set; }

        public Board(int size)
        {
            Size = size;
            Cells = new List<Cell>();
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                    Cells.Add(new Cell { X = x, Y = y });
        }
    }
}
