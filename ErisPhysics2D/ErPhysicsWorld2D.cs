using Eris;
using Eris.Renderer;
using Eris.Utils;
using Eris.Utils.Grid2D;
using ErisMath;
using ErisPhysics2D.Collider;

namespace ErisPhysics2D;

public class ErPhysicsWorld2D
{
    public readonly ErVec2I CellSizeTiles;
    public readonly ErVec2I CellSizePx;
    public readonly ErVec2I TileSize;
    private readonly Dictionary<int, ErColliderArea> Areas = [];
    private readonly Dictionary<int, ErColliderBody> Bodies = [];
    // private readonly ErSpatialGrid2D<ErWorldCell> Grid;
    private readonly ErHashGrid2D<ErWorldCell> Grid;
    private readonly HashSet<ErVec2I> CoordSet = [];
    private readonly HashSet<int> IntSet = [];
    public ErPhysicsWorld2D(ErVec2I cellSizeTiles, ErVec2I tileSize)
    {
        CellSizeTiles = cellSizeTiles;
        TileSize = tileSize;
        CellSizePx = cellSizeTiles * tileSize;
        Grid = new();
    }
    private ErWorldCell NewCell(ErVec2I cellCoord)
    {
        return new(cellCoord, this);
    }
    public void SetTileMask(ErVec2I tileCoord, uint mask)
    {
        var cellCoord = tileCoord / CellSizeTiles;
        GetCellInit(cellCoord).SetTileMask(tileCoord, mask);
    }
    public uint GetTileMask(ErVec2I tileCoord)
    {
        var cellCoord = tileCoord / CellSizeTiles;
        if(Grid.TryGet(cellCoord, out var cell)) return cell.GetTileMask(tileCoord);
        else return uint.MaxValue;
    }
    public uint GetTileMaskAtPoint(ErVec2 point)
    {
        var tileCoord = PointToTileCoord(point);
        var cellCoord = PointToCellCoord(point);
        if(Grid.TryGet(cellCoord, out var cell)) return cell.GetTileMask(tileCoord);
        else return uint.MaxValue;
    }
    public ErVec2I PointToCellCoord(ErVec2 point)
    {
        return (point / (ErVec2)CellSizePx).FloorToInt();
    }
    public ErVec2I PointToTileCoord(ErVec2 point)
    {
        return (point / (ErVec2)TileSize).FloorToInt();
    }
    private IEnumerable<ErVec2I> GetCellCoordsTouchingRect(ErRect2 rect)
    {
        var tl = PointToCellCoord(rect.Position);
        var br = PointToCellCoord(rect.Position+rect.Size) + ErVec2I.One;
        for (int xi = tl.X; xi <= br.X; xi++)
        {
            for(int yi = tl.Y; yi <= br.Y; yi++)
            {
                yield return new(xi,yi);
            }
        }
    }
    private ErWorldCell GetCellInit(ErVec2I cellCoord)
    {
        return Grid.Get(cellCoord, NewCell);
    }
    public void AddArea(ErColliderArea area)
    {
        CoordSet.Clear();
        if(Areas.TryGetValue(area.Id, out var oldArea))
        {
            // get old overlapping cell coord hash set
            foreach (var cellCoord in GetCellCoordsTouchingRect(oldArea.Rect))
            {
                CoordSet.Add(cellCoord);
            }
        }
        Areas[area.Id] = area;
        // new plan:
        // each cell in the old cells should have the area right?
        // so loop through all the cells with the new area
        // if that cell is already in the old cells, remove it from the set
        // for all the remaining cells remove the area from each cell
        foreach (var cellCoord in GetCellCoordsTouchingRect(area.Rect))
        {
            var cell = GetCellInit(cellCoord);
            cell.Areas[area.Id] = area;
            CoordSet.Remove(cellCoord);
        }
        foreach (var cellCoord in CoordSet)
        {
            if(Grid.TryGet(cellCoord, out var cell)) cell.Areas.Remove(area.Id);
        }
    }
    public bool RemoveArea(int areaId)
    {
        if(!Areas.TryGetValue(areaId, out var area)) return false;
        area.ClearBodies();
        Areas.Remove(areaId);
        // remove from adj cells
        // pad the area rect because the area might have moved? I dunno, it's dumb
        var rect = ErRect2.Centered(area.Rect.Center, area.Rect.Size * 2);
        foreach (var cellCoord in GetCellCoordsTouchingRect(rect))
        {
            if(Grid.TryGet(cellCoord, out var cell)) cell.Areas.Remove(area.Id);
        }
        return true;
    }
    public void SetBody(ErColliderBody body)
    {
        CoordSet.Clear();
        if(Bodies.TryGetValue(body.Id, out var oldBody))
        {
            // get old overlapping cell coord hash set
            foreach (var cellCoord in GetCellCoordsTouchingRect(oldBody.Rect))
            {
                CoordSet.Add(cellCoord);
            }
        }
        Bodies[body.Id] = body;
        // new plan:
        // each cell in the old cells should have the area right?
        // so loop through all the cells with the new area
        // if that cell is already in the old cells, remove it from the set
        // for all the remaining cells remove the area from each cell
        foreach (var cellCoord in GetCellCoordsTouchingRect(body.Rect))
        {
            var cell = GetCellInit(cellCoord);
            cell.Bodies[body.Id] = body;
            CoordSet.Remove(cellCoord);
        }
        foreach (var cellCoord in CoordSet)
        {
            if(Grid.TryGet(cellCoord, out var cell)) cell.Bodies.Remove(body.Id);
        }
    }
    public bool RemoveBody(int bodyId)
    {
        if(!Bodies.TryGetValue(bodyId, out var body)) return false;
        Bodies.Remove(bodyId);
        // remove from adj cells
        foreach (var cellCoord in GetCellCoordsTouchingRect(body.Rect))
        {
            if(Grid.TryGet(cellCoord, out var cell)) cell.Bodies.Remove(body.Id);
        }
        return true;
    }
    public void UpdateAreas()
    {
        foreach (var area in Areas.Values)
        {
            area.Process(GetBodiesInRect(area.Rect));
        }
    }
    private IEnumerable<ErColliderBody> GetBodiesInRect(ErRect2 rect)
    {
        IntSet.Clear();
        foreach (var cellCoord in GetCellCoordsTouchingRect(rect))
        {
            if(!Grid.TryGet(cellCoord, out var cell)) continue;
            foreach(var body in cell.Bodies.Values)
            {
                if(IntSet.Contains(body.Id)) continue;
                IntSet.Add(body.Id);
                if(body.Rect.Overlaps(rect)) yield return body;
            }
        }
    }
    public void MoveAndSlide(double dt, ErColliderBody body)
    {
        var velocity = body.Velocity * dt;
        var position = body.Position;
        MoveAndSlide(body.Id, body.Mask, body.Size, ref position, ref velocity);
        body.Velocity = velocity / dt;
        body.Position = position;
        SetBody(body);
    }
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
    // move slide horizontal positive
    private void MsHp(int id, uint mask, ErVec2 size, ref double x, double y, ref double dx)
    {
        ErRect2 rect = new(x+dx,y,size.X,size.Y);
        double maxX = double.MaxValue;
        foreach (var tile in GetColliders(id, mask, rect))
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
        foreach (var tile in GetColliders(id, mask, rect))
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
        foreach (var tile in GetColliders(id, mask, rect))
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
        foreach (var tile in GetColliders(id, mask, rect))
        {
            if(tile.Bottom > minY) minY = tile.Bottom;
        }
        if(minY > rect.Top) dy += minY - rect.Top + ErMath.EPSILON;
        y += dy;
    }
    private IEnumerable<ErRect2> GetColliders(int id, uint mask, ErRect2 rect)
    {
        var tl = PointToTileCoord(rect.Position);
        var br = PointToTileCoord(rect.Position + rect.Size) + ErVec2I.One;
        for (int xi = tl.X; xi <= br.X; xi++)
        {
            for(int yi = tl.Y; yi <= br.Y; yi++)
            {
                ErVec2I tileCoord = new(xi, yi);
                var cellCoord = tileCoord / CellSizeTiles;
                if(!Grid.TryGet(cellCoord, out var cell)) continue;
                if((cell.GetTileMask(tileCoord) & mask) != 0) yield return new((ErVec2)(tileCoord * TileSize),(ErVec2)TileSize);
            }
        }
        IntSet.Clear();
        foreach (var cellCoord in GetCellCoordsTouchingRect(rect))
        {
            if(!Grid.TryGet(cellCoord, out var cell)) continue;
            foreach (var body in cell.Bodies.Values)
            {
                if(IntSet.Contains(body.Id)) continue;
                IntSet.Add(body.Id);
                if(body.Id == id) continue;
                if((body.Mask & mask) == 0) continue;
                if(!body.Rect.Overlaps(rect)) continue;
                yield return body.Rect;
            }
        }
    }
    // raycasting
    private IEnumerable<ErVec2I> GetLine(ErVec2 start, ErVec2 end)
    {
        double dist = (start - end).GetManhattan();
        // for (let step = 0; step <= N; step++) {
        // let t = N === 0? 0.0 : step / N;
        // points.push(round_point(lerp_point(p0, p1, t)));
        for(int step = 0; step < dist; step++)
        {
            double t = dist == 0 ? 0 : step / dist;
            yield return PointToTileCoord(ErMath.Lerp(start, end, t));
        }
    }
    public bool Raycast(uint mask, ErVec2 start, ErVec2 end)
    {
        foreach (var tileCoord in GetLine(start, end))
        {
            var cellCoord = tileCoord / CellSizeTiles;
            if(!Grid.TryGet(cellCoord, out var cell)) continue;
            if((mask & cell.GetTileMask(tileCoord)) != 0) return true;
        }
        return false;
    }
    public void DebugDraw()
    {
        foreach (var cell in Grid.Values)
        {
            foreach (var tileCoord in cell.RectTiles.GetInnerCoords())
            {
                var mask = cell.GetTileMask(tileCoord);
                if(mask == 0) continue;
                ErEngine.Renderer.DrawRect(new(tileCoord.X * TileSize.X, tileCoord.Y * TileSize.Y, TileSize.X, TileSize.Y), ErColor.Blue, filled: false);
            }
            foreach (var item in cell.Areas.Values)
            {
                // if(!Areas.ContainsKey(item.Id)) throw new("fuck off");
                if(item.OverlappingCount > 0) ErEngine.Renderer.DrawRect(item.Rect, ErColor.Red, filled: false);
                else ErEngine.Renderer.DrawRect(item.Rect, ErColor.Blue, filled: false);
            }
        }
    }
}

// using ErisMath;
// using ErisPhysics2D.Collider;

// namespace ErisPhysics2D;

// public partial class ErPhysicsWorld2D
// {
//     private abstract class ErColliderLookup<T>(ErPhysicsWorld2D world) where T: ErCollider
//     {
//         protected readonly ErPhysicsWorld2D World = world;
//         public readonly Dictionary<int, T> Lookup = [];
//         public void Set<U>(int id, U collider) where U: T, new()
//         {
//             U? cached = null;
//             if(!Lookup.TryGetValue(id, out var temp)){}
//             else if(temp is U u) cached = u;
//             else Remove(id); // removes old temp
//             if(cached is null)
//             {
//                 cached = new();
//                 Lookup[id] = cached;
//             }
//             collider.Copy(ref cached);
//             // Console.WriteLine(cached.IsDirty);
//             if (!cached.IsDirty) return;
//             Pop(id, cached);
//             Push(id, cached);
//         }
//         protected abstract void Pop(int id, T cached);
//         protected abstract void Push(int id, T cached);
//         public bool Remove(int id)
//         {
//             if(!Lookup.TryGetValue(id, out var cached)) return false;
//             Pop(id, cached);
//             cached.OnRemove();
//             Lookup.Remove(id);
//             return true;
//         }
//     }
//     private class ErBodyLookup(ErPhysicsWorld2D world) : ErColliderLookup<ErColliderBody>(world)
//     {
//         protected override void Push(int id, ErColliderBody cached)
//         {
//             // Console.WriteLine(id);
//             foreach (var cell in World.GetAndInitCells(new(cached.Position, cached.Size)))
//             {
//                 cell.Bodies[id] = cached;
//             }
//         }
//         protected override void Pop(int id, ErColliderBody cached)
//         {
//             foreach (var cell in World.GetExtantCells(new(cached.Position, cached.Size)))
//             {
//                 cell.Bodies.Remove(id);
//             }
//         }
//     }
//     private class ErAreaLookup(ErPhysicsWorld2D world) : ErColliderLookup<ErColliderArea>(world)
//     {
//         //
//         protected override void Pop(int id, ErColliderArea cached)
//         {
//             foreach (var cell in World.GetAndInitCells(new(cached.Position, cached.Size)))
//             {
//                 cell.Areas[id] = cached;
//             }
//         }
//         protected override void Push(int id, ErColliderArea cached)
//         {
//             foreach (var cell in World.GetExtantCells(new(cached.Position, cached.Size)))
//             {
//                 cell.Areas.Remove(id);
//             }
//         }
//     }
//     private readonly ErBodyLookup BodyLookup;
//     private readonly ErAreaLookup AreaLookup;
//     private readonly Dictionary<ErVec2I, ErWorldCell> Cells = [];
//     private readonly HashSet<ErVec2I> CellCoordSet = [];
//     private readonly HashSet<int> IntSet = [];
//     private readonly uint[] TileMaskLookup;
//     public readonly ErVec2I TileSizePx;
//     public readonly ErVec2I CellSizeTiles;
//     public readonly ErVec2I CellSizePx;
//     public readonly int DefaultTileIdx;
//     public Action<ErRect2,bool,uint>? DebugDrawRect;
//     public Action<ErVec2,ErVec2,bool,uint>? DebugDrawLine;
//     public ErPhysicsWorld2D(ErVec2I cellSizeTiles, ErVec2I tileSizePx, int defaultTileIdx, uint[] tileMaskLookup)
//     {
//         TileSizePx = tileSizePx;
//         CellSizeTiles = cellSizeTiles;
//         CellSizePx = CellSizeTiles * TileSizePx;
//         DefaultTileIdx = defaultTileIdx;
//         TileMaskLookup = tileMaskLookup;
//         BodyLookup = new(this);
//         AreaLookup = new(this);
//     }
//     // private bool IsTileNorm(ErVec2I tileCoords)
//     // {
//     //     return tileCoords.X >= 0 && tileCoords.X < CellSizeTiles.X && tileCoords.Y >= 0 && tileCoords.Y < CellSizeTiles.Y;
//     // }
//     // private int GetCellTileIdx(ErVec2I normTileCoord)
//     // {
//     //     if (!IsTileNorm(normTileCoord))
//     //     {
//     //         Console.WriteLine($"bad tile coord '{normTileCoord}'");
//     //         return -1;
//     //     }
//     //     return normTileCoord.Y * CellSizeTiles.X + normTileCoord.X;
//     // }
//     // Utility methods
//     private ErWorldCell? GetCell(ErVec2I cellCoord)
//     {
//         if(Cells.TryGetValue(cellCoord, out var cell)) return cell;
//         return null;
//     }
//     private ErVec2I PointToCellCoord(ErVec2 position)
//     {
//         return (position / (ErVec2)CellSizePx).FloorToInt();
//     }
//     public ErVec2I PointToTileCoord(ErVec2 position)
//     {
//         return (position / (ErVec2)TileSizePx).FloorToInt();
//     }
//     private ErRect2 GetTileRect(ErVec2I tileCoord)
//     {
//         return new (tileCoord.X * TileSizePx.X, tileCoord.Y * TileSizePx.Y, TileSizePx.X, TileSizePx.Y);
//     }
//     private HashSet<ErVec2I> GetCellCoords(ErRect2 rect)
//     {
//         var tl = PointToCellCoord(rect.Position);
//         var br = PointToCellCoord(rect.Position+rect.Size);
//         CellCoordSet.Clear();
//         for (int xi = tl.X; xi <= br.X; xi++)
//         {
//             for(int yi = tl.Y; yi <= br.Y; yi++)
//             {
//                 CellCoordSet.Add(new(xi,yi));
//             }
//         }
//         return CellCoordSet;
//     }
//     private IEnumerable<ErWorldCell> GetExtantCells(ErRect2 rect)
//     {
//         foreach (var cellPos in GetCellCoords(rect))
//         {
//             if(!Cells.TryGetValue(cellPos, out var cell)) continue;
//             yield return cell;
//         }
//     }
//     private IEnumerable<ErWorldCell> GetAndInitCells(ErRect2 rect)
//     {
//         foreach (var cellPos in GetCellCoords(rect))
//         {
//             if(!Cells.TryGetValue(cellPos, out var cell))
//             {
//                 cell = new(cellPos, this);
//                 Cells[cellPos] = cell;
//             }
//             yield return cell;
//         }
//     }
//     // Move and slide methods
//     private IEnumerable<ErRect2> GetColliders(int id, uint mask, ErRect2 rect)
//     {
//         foreach (var cell in GetExtantCells(rect))
//         {
//             foreach (var (bodyId, body) in cell.Bodies)
//             {
//                 if(bodyId == id) continue;
//                 if((body.Mask & mask) == 0) continue;
//                 var bodyRect = body.Rect;
//                 if(!rect.Overlaps(bodyRect)) continue;
//                 yield return bodyRect;
//             }
//         }
//         var tl = PointToTileCoord(rect.Position);
//         var br = PointToTileCoord(rect.Position + rect.Size);
//         ErVec2I currentCellCoords = tl / CellSizeTiles;
//         ErWorldCell? currentCell = GetCell(currentCellCoords);
//         for (int xi = tl.X; xi <= br.X; xi++)
//         {
//             for(int yi = tl.Y; yi <= br.Y; yi++)
//             {
//                 ErVec2I tileCoord = new(xi,yi);
//                 ErVec2I cellCoord = tileCoord / CellSizeTiles;
//                 if(cellCoord != currentCellCoords)
//                 {
//                     currentCellCoords = cellCoord;
//                     currentCell = GetCell(currentCellCoords);
//                 }
//                 if(currentCell is null) continue;
//                 int tileId = currentCell.GetTileId(tileCoord);
//                 if(tileId < 0) continue;
//                 uint tileMask = TileMaskLookup[tileId];
//                 if((mask & tileMask) == 0) continue;
//                 yield return GetTileRect(tileCoord);
//             }
//         }
//     }
//     private IEnumerable<(int,ErColliderBody)> GetBodies(uint mask, ErRect2 rect)
//     {
//         IntSet.Clear();
//         foreach (var cell in GetExtantCells(rect))
//         {
//             foreach (var (bodyId, body) in cell.Bodies)
//             {
//                 if(IntSet.Contains(bodyId)) continue;
//                 if((mask & body.Mask) == 0) continue;
//                 if(!rect.Overlaps(body.Rect)) continue;
//                 IntSet.Add(bodyId);
//                 yield return(bodyId, body);
//             }
//         }
//     }
//     // move slide horizontal positive
//     private void MsHp(int id, uint mask, ErVec2 size, ref double x, double y, ref double dx)
//     {
//         ErRect2 rect = new(x+dx,y,size.X,size.Y);
//         double maxX = double.MaxValue;
//         foreach (var tile in GetColliders(id, mask, rect))
//         {
//             if(tile.Left < maxX) maxX = tile.Left;
//         }
//         if(maxX < rect.Right)
//         {
//             dx += maxX - rect.Right - ErMath.EPSILON;
//         }
//         x += dx;
//     }
//     private void MsHn(int id, uint mask, ErVec2 size, ref double x, double y, ref double dx)
//     {
//         ErRect2 rect = new(x+dx,y,size.X,size.Y);
//         double minX = double.MinValue;
//         foreach (var tile in GetColliders(id, mask, rect))
//         {
//             if(tile.Right > minX) minX = tile.Right;
//         }
//         if(minX > rect.Left) dx += minX - rect.Left + ErMath.EPSILON;
//         x += dx;
//     }
//     private void MsVp(int id, uint mask, ErVec2 size, double x, ref double y, ref double dy)
//     {
//         ErRect2 rect = new(x,y+dy,size.X,size.Y);
//         double maxY = double.MaxValue;
//         foreach (var tile in GetColliders(id, mask, rect))
//         {
//             if(tile.Top < maxY) maxY = tile.Top;
//         }
//         if(maxY < rect.Bottom) dy += maxY - rect.Bottom - ErMath.EPSILON;
//         y += dy;
//     }
//     private void MsVn(int id, uint mask, ErVec2 size, double x, ref double y, ref double dy)
//     {
//         ErRect2 rect = new(x,y+dy,size.X,size.Y);
//         double minY = double.MinValue;
//         foreach (var tile in GetColliders(id, mask, rect))
//         {
//             if(tile.Bottom > minY) minY = tile.Bottom;
//         }
//         if(minY > rect.Top) dy += minY - rect.Top + ErMath.EPSILON;
//         y += dy;
//     }
//     public void MoveAndSlide<T>(double dt, int bodyId, T body) where T: ErColliderBody, new()
//     {
//         var velocity = body.Velocity * dt;
//         var position = body.Position;
//         MoveAndSlide(bodyId, body.Mask, body.Size, ref position, ref velocity);
//         body.Velocity = velocity / dt;
//         body.Position = position;
//         SetBody(bodyId, body);
//     }
//     public void MoveAndSlide(int id, uint mask, ErVec2 size, ref ErVec2 position, ref ErVec2 velocity)
//     {
//         double x = position.X; double y = position.Y;
//         double dx = velocity.X; double dy = velocity.Y;
//         if(dx > 0) MsHp(id, mask, size, ref x, y, ref dx);
//         else if(dx < 0) MsHn(id, mask, size, ref x, y, ref dx);
//         if(dy > 0) MsVp(id, mask, size, x, ref y, ref dy);
//         else if(dy < 0) MsVn(id, mask, size, x, ref y, ref dy);
//         position = new(x,y);
//         velocity = new(dx, dy);
//     }
//     // public tile getting and setting methods
//     public int GetTile(ErVec2I tileCoord)
//     {
//         var cellCoord = tileCoord / CellSizeTiles;
//         if(!Cells.TryGetValue(cellCoord, out var cell)) return DefaultTileIdx;
//         return cell.GetTileId(tileCoord);
//     }
//     public void SetTile(ErVec2I tileCoord, int tileId)
//     {
//         var cellCoord = tileCoord / CellSizeTiles;
//         if(!Cells.TryGetValue(cellCoord, out var cell))
//         {
//             cell = new(cellCoord, this);
//             Cells[cellCoord] = cell;
//         }
//         cell.SetTileId(tileCoord, tileId);
//     }
//     public void SetTileRect(ErRect2I tileRect, int tileId)
//     {
//         for(int xi = 0; xi < tileRect.Size.X; xi++)
//         {
//             for(int yi = 0; yi < tileRect.Size.Y; yi++)
//             {
//                 ErVec2I tileCoord = tileRect.Position + new ErVec2I(xi,yi);
//                 SetTile(tileCoord, tileId);
//             }
//         }
//     }
//     // public IEnumerable<int> GetTiles(IEnumerable<ErVec2I> tileCoords)
//     // {
//     //     var currentCellCoord = ErVec2I.Max;
//     //     ErWorldCell? currentCell = null;
//     //     foreach (var tileCoord in tileCoords)
//     //     {
//     //         var cellCoord = tileCoord / CellSizeTiles;
//     //         if(cellCoord != currentCellCoord)
//     //         {
//     //             currentCellCoord = cellCoord;
//     //             currentCell = GetCell(cellCoord);
//     //         }
//     //         if(currentCell is null) continue;
//     //         yield return currentCell.GetTileId(tileCoord);
//     //     }
//     // }
//     // public collider methods
//     public void SetBody<T>(int id, T body) where T: ErColliderBody, new()
//     {
//         BodyLookup.Set(id, body);
//     }
//     public bool RemoveBody(int id)
//     {
//         return BodyLookup.Remove(id);
//     }
//     public void SetArea<T>(int id, T body) where T: ErColliderArea, new()
//     {
//         AreaLookup.Set(id, body);
//     }
//     public bool RemoveArea(int id)
//     {
//         return AreaLookup.Remove(id);
//     }
//     public void Update(double dt)
//     {
//         // try to move bodies, calling on_move
//         // foreach (var (bodyId, body) in BodyLookup.Lookup)
//         // {
//         //     var pos = body.Position;// - body.Size * 0.5;
//         //     var vel = body.Velocity * dt;
//         //     MoveAndSlide(bodyId, body.Mask, body.Size, ref pos, ref vel);
//         //     body.Position = pos;// + body.Size * 0.5;
//         //     body.Velocity = vel / dt;
//         //     body.OnMove();
//         // }
//         // update area overlaps, calling on_enter, on_exit, and update
//         foreach (var area in AreaLookup.Lookup.Values)
//         {
//             area.Update(GetBodies(area.Mask, area.Rect), BodyLookup.Lookup);
//         }
//     }
//     // raycasting
//     private IEnumerable<ErVec2I> GetLine(ErVec2 start, ErVec2 end)
//     {
//         double dist = (start - end).GetManhattan();
//         // for (let step = 0; step <= N; step++) {
//         // let t = N === 0? 0.0 : step / N;
//         // points.push(round_point(lerp_point(p0, p1, t)));
//         for(int step = 0; step < dist; step++)
//         {
//             double t = dist == 0 ? 0 : step / dist;
//             yield return PointToTileCoord(ErMath.Lerp(start, end, t));
//         }
//     }
//     public bool Raycast(uint mask, ErVec2 start, ErVec2 end)
//     {
//         foreach (var coord in GetLine(start, end))
//         {
//             int tileId = GetTile(coord);
//             if(tileId < 0) continue;
//             uint tileMask = TileMaskLookup[tileId];
//             if((mask & tileMask) != 0) return true;
//         }
//         return false;
//     }
//     public bool RaycastDebug(uint mask, ErVec2 start, ErVec2 end)
//     {
//         bool res = Raycast(mask, start, end);
//         if(DebugDrawLine is not null) DebugDrawLine(start, end, res, mask);
//         if(DebugDrawRect is null) return res;
//         foreach (var coord in GetLine(start, end))
//         {
//             DebugDrawRect(GetTileRect(coord), res, mask);
//         }
//         return res;
//     }
//     public void DebugDrawTiles()
//     {
//         if(DebugDrawRect is null) return;
//         foreach (var cell in Cells.Values)
//         {
//             for (int xi = 0; xi < CellSizeTiles.X; xi++)
//             {
//                 for (int yi = 0; yi < CellSizeTiles.Y; yi++)
//                 {
//                     var coord = new ErVec2I(xi,yi) + cell.CoordTiles;
//                     int tileId = cell.GetTileId(coord);
//                     if(tileId < 0) continue;
//                     var rect = GetTileRect(coord);
//                     DebugDrawRect(rect, false, TileMaskLookup[tileId]);
//                 }
//             }
//         }
//     }
//     public void DebugDrawBodies()
//     {
//         if(DebugDrawRect is null) return;
//         foreach (var item in BodyLookup.Lookup.Values)
//         {
//             // Console.WriteLine($"{item.Rect} {item.Rect.Centered(item.Rect.Position)}");
//             DebugDrawRect(item.Rect, false, item.Mask);
//         }
//     }
//     public void DebugDrawAreas()
//     {
//         if(DebugDrawRect is null) return;
//         foreach (var item in AreaLookup.Lookup.Values)
//         {
//             DebugDrawRect(item.Rect, item.OverlappingCount > 0, item.Mask);
//         }
//     }
// }
