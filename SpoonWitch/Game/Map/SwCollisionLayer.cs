using Eris;
using Eris.Renderer;
using ErisMath;

namespace SpoonWitch.Game.Map;

public class SwCollisionLayer
{
    public static readonly ErVec2I CellSizeTiles = new(8,8);
    public readonly ErVec2I CellSizePx;
    private readonly Dictionary<ErVec2I,SwCell> Cells = [];
    private readonly SwMap Map;
    private readonly HashSet<ErVec2I> ActiveCells = [];
    public readonly struct SwEntRect
    {
        public readonly int Id{get; init;}
        public readonly uint Mask{get; init;}
        public readonly ErRect2 Rect{get; init;}
    }
    private class SwCell
    {
        public readonly List<SwEntRect> Rects = [];
        public int[] TileIds;
        public SwCell()
        {
            TileIds = new int[CellSizeTiles.X * CellSizeTiles.Y];
            Array.Fill(TileIds, 0);
        } 
    }
    public SwCollisionLayer(SwMap map)
    {
        Map = map;
        CellSizePx = Map.TileSize * CellSizeTiles;
    }
    private static bool InBounds(ErVec2I coord)
    {
        return InBounds(coord.X, coord.Y);
    }
    public static bool InBounds(int x, int y)
    {
        return x >= 0 && x < CellSizeTiles.X && y >= 0 && y < CellSizeTiles.Y;
    }
    private static int GetIdx(ErVec2I tileCoord)
    {
        return GetIdx(tileCoord.X, tileCoord.Y);
    }
    public static int GetIdx(int x, int y)
    {
        if(!InBounds(x,y))
        {
            ErEngine.LogWarning("out of bounds tile access: (",x,",",y,")");
            return 0;
        }
        return y * CellSizeTiles.X + x;
    }
    public void SetTile(ErVec2I tileCoord, int tileId)
    {
        var cellCoord = tileCoord / CellSizeTiles;
        if(!Cells.TryGetValue(cellCoord, out var cell))
        {
            cell = new();
            Cells[cellCoord] = cell;
        }
        tileCoord -= cellCoord * CellSizeTiles;
        cell.TileIds[GetIdx(tileCoord)] = tileId;
    }
    public int GetTile(ErVec2I tileCoord)
    {
        var cellCoord = tileCoord / CellSizeTiles;
        if(!Cells.TryGetValue(cellCoord, out var cell)) return 0;
        tileCoord -= cellCoord * CellSizeTiles;
        return cell.TileIds[GetIdx(tileCoord)];
    }
    public int GetTilePx(ErVec2 position)
    {
        int xi = ErMath.FloorToInt(position.X / Map.TileSize.X);
        int yi = ErMath.FloorToInt(position.Y / Map.TileSize.Y);
        return GetTile(new(xi,yi)); 
    }
    private ErVec2I GetCellCoords(ErVec2 position)
    {
        int x = ErMath.FloorToInt(position.X / CellSizePx.X);
        int y = ErMath.FloorToInt(position.Y / CellSizePx.Y);
        return new(x,y);
    }
    // private ErVec2I GetCellCoords(ErVec2 position)
    // {
    //     int x = ErMath.FloorToInt(position.X / CellSizePx.X);
    //     int y = ErMath.FloorToInt(position.Y / CellSizePx.Y);
    //     return new(x,y);
    // }
    private ErVec2I GetTileCoords(ErVec2 position)
    {
        int x = ErMath.FloorToInt(position.X / Map.TileSize.X);
        int y = ErMath.FloorToInt(position.Y / Map.TileSize.Y);
        return new(x,y);
    }
    private ErRect2 GetTileRect(ErVec2I coord)
    {
        return new (coord.X * Map.TileSize.X, coord.Y * Map.TileSize.Y, Map.TileSize.X, Map.TileSize.Y);
    }
    private IEnumerable<ErVec2I> GetCells(ErRect2 rect)
    {
        var tl = GetCellCoords(rect.Position);
        var br = GetCellCoords(rect.Position+rect.Size);
        ActiveCells.Clear();
        for (int xi = tl.X; xi <= br.X; xi++)
        {
            for(int yi = tl.Y; yi <= br.Y; yi++)
            {
                ActiveCells.Add(new(xi,yi));
            }
        }
        foreach (var item in ActiveCells)
        {
            yield return item;
        }
    }
    private IEnumerable<(ErVec2I,SwCell)> GetActive(ErRect2 rect)
    {
        // ActiveCells.Clear();
        // ActiveCells.Add(GetCellCoords(rect.Position));
        // ActiveCells.Add(GetCellCoords(rect.Position + new ErVec2(rect.Size.X, 0)));
        // ActiveCells.Add(GetCellCoords(rect.Position + new ErVec2(0, rect.Size.Y)));
        // ActiveCells.Add(GetCellCoords(rect.Position+rect.Size));
        foreach (var cellPos in GetCells(rect))
        {
            if(!Cells.TryGetValue(cellPos, out var cell)) continue;
            yield return (cellPos,cell);
        }
    }
    private IEnumerable<(ErVec2I,SwCell)> InitActive(ErRect2 rect)
    {
        // ActiveCells.Clear();
        // ActiveCells.Add(GetCellCoords(rect.Position));
        // ActiveCells.Add(GetCellCoords(rect.Position + new ErVec2(rect.Size.X, 0)));
        // ActiveCells.Add(GetCellCoords(rect.Position + new ErVec2(0, rect.Size.Y)));
        // ActiveCells.Add(GetCellCoords(rect.Position+rect.Size));
        foreach (var cellPos in GetCells(rect))
        {
            if(!Cells.TryGetValue(cellPos, out var cell))
            {
                cell = new();
                Cells[cellPos] = cell;
            }
            yield return (cellPos,cell);
        }
    }
    private IEnumerable<ErRect2> GetTiles(int id, uint mask, ErRect2 rect)
    {
        var tl = GetTileCoords(rect.Position);
        var br = GetTileCoords(rect.Position + rect.Size);
        foreach (var (cellPos,cell) in GetActive(rect))
        {
            var posTiles = cellPos * CellSizeTiles;
            for (int xi = tl.X; xi <= br.X; xi++)
            {
                for(int yi = tl.Y; yi <= br.Y; yi++)
                {
                    ErVec2I coord = new(xi,yi);
                    ErVec2I norm = coord - posTiles;
                    if(!InBounds(norm)) continue;
                    int tileId = cell.TileIds[GetIdx(norm)];
                    var tileData = Map.GetTileData(tileId);
                    if((mask & tileData.CollisionMask) == 0) continue;
                    ErRect2 tile = GetTileRect(coord);
                    yield return tile;
                }
            }
            foreach (var item in cell.Rects)
            {
                if(item.Id == id) continue;
                if((mask & item.Mask) == 0) continue;
                if(!rect.Overlaps(item.Rect)) continue;
                yield return item.Rect;
            }
        }
    }
    private void MsHp(int id, uint mask, ErVec2 size, ref double x, double y, ref double dx)
    {
        ErRect2 rect = new(x+dx,y,size.X,size.Y);
        double maxX = double.MaxValue;
        foreach (var tile in GetTiles(id, mask, rect))
        {
            if(tile.Left < maxX) maxX = tile.Left;
        }
        if(maxX < rect.Right)
        {
            dx += maxX - rect.Right - ErMath.EPSILON;
        }
        x += dx;
    }
    private void MsHn(int id, uint mask, ErVec2 size, ref double x, double y, ref double dx)
    {
        ErRect2 rect = new(x+dx,y,size.X,size.Y);
        double minX = double.MinValue;
        foreach (var tile in GetTiles(id, mask, rect))
        {
            if(tile.Right > minX) minX = tile.Right;
        }
        if(minX > rect.Left) dx += minX - rect.Left + ErMath.EPSILON;
        x += dx;
    }
    private void MsVp(int id, uint mask, ErVec2 size, double x, ref double y, ref double dy)
    {
        ErRect2 rect = new(x,y+dy,size.X,size.Y);
        double maxY = double.MaxValue;
        foreach (var tile in GetTiles(id, mask, rect))
        {
            if(tile.Top < maxY) maxY = tile.Top;
        }
        if(maxY < rect.Bottom) dy += maxY - rect.Bottom - ErMath.EPSILON;
        y += dy;
    }
    private void MsVn(int id, uint mask, ErVec2 size, double x, ref double y, ref double dy)
    {
        ErRect2 rect = new(x,y+dy,size.X,size.Y);
        double minY = double.MinValue;
        foreach (var tile in GetTiles(id, mask, rect))
        {
            if(tile.Bottom > minY) minY = tile.Bottom;
        }
        if(minY > rect.Top) dy += minY - rect.Top + ErMath.EPSILON;
        y += dy;
    }
    public void ClearColliders()
    {
        foreach (var cell in Cells.Values) cell.Rects.Clear();
    }
    public void AddCollider(SwEntRect entRect)
    {
        foreach (var (_,cell) in InitActive(entRect.Rect))
        {
            cell.Rects.Add(entRect);
        }
    }
    // public void SetColliders(IEnumerable<SwEntRect> entRects)
    // {
    //     ClearColliders();
        // foreach (var item in entRects)
        // {
        //     foreach (var (_,cell) in InitActive(item.Rect))
        //     {
        //         cell.Rects.Add(item);
        //     }
        // }
    // }
    public void MoveAndSlide(int id, uint mask, ErVec2 size, ref ErVec2 position, ref ErVec2 velocity)
    {
        double x = position.X; double y = position.Y;
        double dx = velocity.X; double dy = velocity.Y;
        if(dx > 0) MsHp(id, mask, size, ref x, y, ref dx);
        else if(dx < 0) MsHn(id, mask, size, ref x, y, ref dx);
        if(dy > 0) MsVp(id, mask, size, x, ref y, ref dy);
        else if(dy < 0) MsVn(id, mask, size, x, ref y, ref dy);
        position = new(x,y);
        velocity = new(dx, dy);
    }
    public bool Raycast(uint mask, ErVec2 start, ErVec2 end, out ErVec2 position, out int? id)
    {
        position = default;
        id = null;
        return false;
    }
    public void DebugDraw()
    {
        foreach (var (cellPos,cell) in Cells)
        {
            var cellPosTile = cellPos * CellSizeTiles;
            for (int xi = 0; xi < CellSizeTiles.X; xi++)
            {
                for (int yi = 0; yi < CellSizeTiles.Y; yi++)
                {
                    var tileCoord = new ErVec2I(xi,yi);
                    int tileId = cell.TileIds[GetIdx(tileCoord)];
                    var tileData = Map.GetTileData(tileId);
                    if(tileData.CollisionMask == 0) continue;
                    ErEngine.Renderer.DebugDrawRect(ErColor.Red, GetTileRect(tileCoord + cellPosTile),false);
                }
            }
            foreach (var item in cell.Rects)
            {
                ErEngine.Renderer.DebugDrawRect(ErColor.Green, item.Rect, false);
            }
        }
    }
}