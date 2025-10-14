using LineUpV3.Models.DiscSpace;

namespace LineUpV3.Models.BoardSpace
{
    // Rotation strategy interface: defines how to rotate the board
    internal interface IRotation
    {
        // Executes rotation and returns new grid with updated dimensions
        (IDisc?[,] grid, int rows, int cols) Rotate(
            IDisc?[,] currentGrid, 
            int currentRows, 
            int currentCols);
    }
}