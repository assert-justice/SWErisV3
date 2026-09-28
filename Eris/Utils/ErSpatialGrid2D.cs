using ErisMath;

namespace Eris.Utils;

// public interface IErCell2D
// {
//     public ErVec2I CellCoord{get;}
// }
public class ErSpatialGrid2D<T>
{
    public readonly ErVec2I CellSize;
    public readonly Func<ErVec2I, T> CellFactory;
    private readonly Dictionary<ErVec2I, T> Cells = [];
    // private readonly HashSet<ErVec2I> CoordSet = [];
    private T? LastCell;
    private ErVec2I LastCellCoord;
    public ErSpatialGrid2D(ErVec2I cellSize, Func<ErVec2I, T> cellFactory)
    {
        CellSize = cellSize;
        CellFactory = cellFactory;
    }
    public ErVec2I PointToCellCoord(ErVec2 point)
    {
        return (ErVec2I)point / CellSize;
    }
    public IEnumerable<ErVec2I> GetCellCoordsTouchingRect(ErRect2 rect)
    {
        var tl = PointToCellCoord(rect.Position);
        var br = PointToCellCoord(rect.Position+rect.Size);
        for (int xi = tl.X; xi <= br.X; xi++)
        {
            for(int yi = tl.Y; yi <= br.Y; yi++)
            {
                yield return new(xi,yi);
            }
        }
    }
    public IEnumerable<ErVec2I> GetCellCoordsWithinRect(ErRect2 rect)
    {
        foreach (var cellCoord in GetCellCoordsTouchingRect(rect))
        {
            if(!rect.Contains((ErRect2)GetCellRect(cellCoord))) continue;
            yield return cellCoord;
        }
    }
    public ErRect2I GetCellRect(ErVec2I cellCoord)
    {
        return new(cellCoord * CellSize, CellSize);
    }
    public void SetCell(ErVec2I cellCoord, T cell)
    {
        Cells[cellCoord] = cell;
    }
    public bool TryGetCell(ErVec2I cellCoord, out T cell)
    {
        cell = default!;
        if(cellCoord == LastCellCoord && LastCell is not null)
        {
            cell = LastCell;
            return true;
        }
        if(!Cells.TryGetValue(cellCoord, out var c)) return false;
        cell = c;
        LastCell = cell;
        LastCellCoord = cellCoord;
        return true;
    }
    public T GetCellInit(ErVec2I cellCoord)
    {
        if(TryGetCell(cellCoord, out var cell)) return cell;
        cell = CellFactory(cellCoord);
        Cells[cellCoord] = cell;
        LastCell = cell;
        LastCellCoord = cellCoord;
        return cell;
    }
    public IEnumerable<T> GetAllCells()
    {
        foreach (var item in Cells.Values)
        {
            yield return item;
        }
    }
}
