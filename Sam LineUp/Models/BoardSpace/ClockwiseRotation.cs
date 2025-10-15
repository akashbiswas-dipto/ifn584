using LineUpV3.Models.DiscSpace;

namespace LineUpV3.Models.BoardSpace
{
    // Concrete rotation strategy: rotates board 90 degrees clockwise
    internal sealed class ClockwiseRotation : IRotation
    {
        // Rotates the grid 90 degrees clockwise
        // Formula: original(r,c) -> new(c, oldRows - 1 - r)
        public (IDisc?[,] grid, int rows, int cols) Rotate(
            IDisc?[,] currentGrid, int currentRows, int currentCols)
        {
            // New dimensions: rows become cols, cols become rows
            int newRows = currentCols;
            int newCols = currentRows;
            IDisc?[,] newGrid = new IDisc?[newRows, newCols];
            
            // Apply 90-degree clockwise rotation
            for (int r = 0; r < currentRows; r++)
            {
                for (int c = 0; c < currentCols; c++)
                {
                    newGrid[c, currentRows - r - 1] = currentGrid[r, c];
                }
            }
            
            return (newGrid, newRows, newCols);
        }
    }
}