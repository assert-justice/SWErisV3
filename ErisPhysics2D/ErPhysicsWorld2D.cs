using ErisMath;
using ErisPhysics2D.Collider;

namespace ErisPhysics2D;

public partial class ErPhysicsWorld2D
{
    private abstract class ErColliderLookup<T>(ErPhysicsWorld2D world) where T: ErCollider
    {
        protected readonly ErPhysicsWorld2D World = world;
        public readonly Dictionary<int, T> Lookup = [];
        public void Set<U>(int id, U collider) where U: T, new()
        {
            U? cached = null;
            if(!Lookup.TryGetValue(id, out var temp)){}
            else if(temp is U u) cached = u;
            else Remove(id); // removes old temp
            if(cached is null)
            {
                cached = new();
                Lookup[id] = cached;
            }
            collider.Copy(ref cached);
            // Console.WriteLine(cached.IsDirty);
            if (!cached.IsDirty) return;
            Pop(id, cached);
            Push(id, cached);
        }
        protected abstract void Pop(int id, T cached);
        protected abstract void Push(int id, T cached);
        public bool Remove(int id)
        {
            if(!Lookup.TryGetValue(id, out var cached)) return false;
            Pop(id, cached);
            cached.OnRemove();
            return true;
        }
    }
    private class ErBodyLookup(ErPhysicsWorld2D world) : ErColliderLookup<ErColliderBody>(world)
    {
        protected override void Push(int id, ErColliderBody cached)
        {
            // Console.WriteLine(id);
            foreach (var cell in World.GetAndInitCells(new(cached.Position, cached.Size)))
            {
                cell.Bodies[id] = cached;
            }
        }
        protected override void Pop(int id, ErColliderBody cached)
        {
            foreach (var cell in World.GetExtantCells(new(cached.Position, cached.Size)))
            {
                cell.Bodies.Remove(id);
            }
        }
    }
    private class ErAreaLookup(ErPhysicsWorld2D world) : ErColliderLookup<ErColliderArea>(world)
    {
        //
        protected override void Pop(int id, ErColliderArea cached)
        {
            foreach (var cell in World.GetAndInitCells(new(cached.Position, cached.Size)))
            {
                cell.Areas[id] = cached;
            }
        }
        protected override void Push(int id, ErColliderArea cached)
        {
            foreach (var cell in World.GetExtantCells(new(cached.Position, cached.Size)))
            {
                cell.Areas.Remove(id);
            }
        }
    }
    private readonly ErBodyLookup BodyLookup;
    private readonly ErAreaLookup AreaLookup;
    private readonly Dictionary<ErVec2I, ErWorldCell> Cells = [];
    private readonly HashSet<ErVec2I> CellCoordSet = [];
    private readonly HashSet<int> IntSet = [];
    private readonly uint[] TileMaskLookup;
    public readonly ErVec2I TileSizePx;
    public readonly ErVec2I CellSizeTiles;
    public readonly ErVec2I CellSizePx;
    public readonly int DefaultTileIdx;
    public Action<ErRect2,bool,uint>? DebugDrawRect;
    public Action<ErVec2,ErVec2,bool,uint>? DebugDrawLine;
    public ErPhysicsWorld2D(ErVec2I cellSizeTiles, ErVec2I tileSizePx, int defaultTileIdx, uint[] tileMaskLookup)
    {
        TileSizePx = tileSizePx;
        CellSizeTiles = cellSizeTiles;
        CellSizePx = CellSizeTiles * TileSizePx;
        DefaultTileIdx = defaultTileIdx;
        TileMaskLookup = tileMaskLookup;
        BodyLookup = new(this);
        AreaLookup = new(this);
    }
    // private bool IsTileNorm(ErVec2I tileCoords)
    // {
    //     return tileCoords.X >= 0 && tileCoords.X < CellSizeTiles.X && tileCoords.Y >= 0 && tileCoords.Y < CellSizeTiles.Y;
    // }
    // private int GetCellTileIdx(ErVec2I normTileCoord)
    // {
    //     if (!IsTileNorm(normTileCoord))
    //     {
    //         Console.WriteLine($"bad tile coord '{normTileCoord}'");
    //         return -1;
    //     }
    //     return normTileCoord.Y * CellSizeTiles.X + normTileCoord.X;
    // }
    // Utility methods
    private ErWorldCell? GetCell(ErVec2I cellCoord)
    {
        if(Cells.TryGetValue(cellCoord, out var cell)) return cell;
        return null;
    }
    private ErVec2I PointToCellCoord(ErVec2 position)
    {
        return (position / (ErVec2)CellSizePx).FloorToInt();
    }
    private ErVec2I PointToTileCoord(ErVec2 position)
    {
        return (position / (ErVec2)TileSizePx).FloorToInt();
    }
    private ErRect2 GetTileRect(ErVec2I tileCoord)
    {
        return new (tileCoord.X * TileSizePx.X, tileCoord.Y * TileSizePx.Y, TileSizePx.X, TileSizePx.Y);
    }
    private HashSet<ErVec2I> GetCellCoords(ErRect2 rect)
    {
        var tl = PointToCellCoord(rect.Position);
        var br = PointToCellCoord(rect.Position+rect.Size);
        CellCoordSet.Clear();
        for (int xi = tl.X; xi <= br.X; xi++)
        {
            for(int yi = tl.Y; yi <= br.Y; yi++)
            {
                CellCoordSet.Add(new(xi,yi));
            }
        }
        return CellCoordSet;
    }
    private IEnumerable<ErWorldCell> GetExtantCells(ErRect2 rect)
    {
        foreach (var cellPos in GetCellCoords(rect))
        {
            if(!Cells.TryGetValue(cellPos, out var cell)) continue;
            yield return cell;
        }
    }
    private IEnumerable<ErWorldCell> GetAndInitCells(ErRect2 rect)
    {
        foreach (var cellPos in GetCellCoords(rect))
        {
            if(!Cells.TryGetValue(cellPos, out var cell))
            {
                cell = new(cellPos, this);
                Cells[cellPos] = cell;
            }
            yield return cell;
        }
    }
    // Move and slide methods
    private IEnumerable<ErRect2> GetColliders(int id, uint mask, ErRect2 rect)
    {
        var tl = PointToTileCoord(rect.Position);
        var br = PointToTileCoord(rect.Position + rect.Size);
        ErVec2I currentCellCoords = tl / CellSizeTiles;
        ErWorldCell? currentCell = GetCell(currentCellCoords);
        for (int xi = tl.X; xi <= br.X; xi++)
        {
            for(int yi = tl.Y; yi <= br.Y; yi++)
            {
                ErVec2I tileCoord = new(xi,yi);
                ErVec2I cellCoord = tileCoord / CellSizeTiles;
                if(cellCoord != currentCellCoords)
                {
                    currentCellCoords = cellCoord;
                    currentCell = GetCell(currentCellCoords);
                }
                if(currentCell is null) continue;
                int tileId = currentCell.GetTileId(tileCoord);
                uint tileMask = TileMaskLookup[tileId];
                if((mask & tileMask) == 0) continue;
                yield return GetTileRect(tileCoord);
            }
        }
        foreach (var cell in GetExtantCells(rect))
        {
            foreach (var (bodyId, body) in cell.Bodies)
            {
                if(bodyId == id) continue;
                if((body.Mask & mask) == 0) continue;
                var bodyRect = body.Rect;
                if(!rect.Overlaps(bodyRect)) continue;
                yield return bodyRect;
            }
        }
    }
    private IEnumerable<(int,ErColliderBody)> GetBodies(uint mask, ErRect2 rect)
    {
        foreach (var cell in GetExtantCells(rect))
        {
            foreach (var (bodyId, body) in cell.Bodies)
            {
                if((mask & body.Mask) == 0) continue;
                if(!rect.Overlaps(body.Rect)) continue;
                yield return(bodyId, body);
            }
        }
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
    private void MoveAndSlide(int id, uint mask, ErVec2 size, ref ErVec2 position, ref ErVec2 velocity)
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
    // public tile getting and setting methods
    public int GetTile(ErVec2I tileCoord)
    {
        var cellCoord = tileCoord / CellSizeTiles;
        if(!Cells.TryGetValue(cellCoord, out var cell)) return DefaultTileIdx;
        return cell.GetTileId(tileCoord);
    }
    public void SetTile(ErVec2I tileCoord, int tileId)
    {
        var cellCoord = tileCoord / CellSizeTiles;
        if(!Cells.TryGetValue(cellCoord, out var cell))
        {
            cell = new(cellCoord, this);
            Cells[cellCoord] = cell;
        }
        cell.SetTileId(tileCoord, tileId);
    }
    // public IEnumerable<int> GetTiles(IEnumerable<ErVec2I> tileCoords)
    // {
    //     var currentCellCoord = ErVec2I.Max;
    //     ErWorldCell? currentCell = null;
    //     foreach (var tileCoord in tileCoords)
    //     {
    //         var cellCoord = tileCoord / CellSizeTiles;
    //         if(cellCoord != currentCellCoord)
    //         {
    //             currentCellCoord = cellCoord;
    //             currentCell = GetCell(cellCoord);
    //         }
    //         if(currentCell is null) continue;
    //         yield return currentCell.GetTileId(tileCoord);
    //     }
    // }
    // public collider methods
    public void SetBody<T>(int id, T body) where T: ErColliderBody, new()
    {
        BodyLookup.Set(id, body);
    }
    public bool RemoveBody(int id)
    {
        return BodyLookup.Remove(id);
    }
    public void SetArea<T>(int id, T body) where T: ErColliderArea, new()
    {
        AreaLookup.Set(id, body);
    }
    public bool RemoveArea(int id)
    {
        return AreaLookup.Remove(id);
    }
    public void Update(double dt)
    {
        // try to move bodies, calling on_move
        foreach (var (bodyId, body) in BodyLookup.Lookup)
        {
            var pos = body.Position - body.Size * 0.5;
            var vel = body.Velocity * dt;
            MoveAndSlide(bodyId, body.Mask, body.Size, ref pos, ref vel);
            body.Position = pos + body.Size * 0.5;
            body.Velocity = vel / dt;
            body.OnMove();
        }
        // update area overlaps, calling on_enter, on_exit, and update
        foreach (var area in AreaLookup.Lookup.Values)
        {
            area.Update(GetBodies(area.Mask, area.Rect), BodyLookup.Lookup);
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
        foreach (var coord in GetLine(start, end))
        {
            int tileId = GetTile(coord);
            uint tileMask = TileMaskLookup[tileId];
            if((mask & tileMask) != 0) return true;
        }
        return false;
    }
    public bool RaycastDebug(uint mask, ErVec2 start, ErVec2 end)
    {
        bool res = Raycast(mask, start, end);
        if(DebugDrawLine is not null) DebugDrawLine(start, end, res, mask);
        if(DebugDrawRect is null) return res;
        foreach (var coord in GetLine(start, end))
        {
            DebugDrawRect(GetTileRect(coord), res, mask);
        }
        return res;
    }
    public void DebugDrawTiles()
    {
        if(DebugDrawRect is null) return;
        foreach (var cell in Cells.Values)
        {
            for (int xi = 0; xi < CellSizeTiles.X; xi++)
            {
                for (int yi = 0; yi < CellSizeTiles.Y; yi++)
                {
                    var coord = new ErVec2I(xi,yi) + cell.CoordTiles;
                    int tileId = cell.GetTileId(coord);
                    var rect = GetTileRect(coord);
                    DebugDrawRect(rect, false, TileMaskLookup[tileId]);
                }
            }
        }
    }
    public void DebugDrawBodies()
    {
        if(DebugDrawRect is null) return;
        foreach (var item in BodyLookup.Lookup.Values)
        {
            // Console.WriteLine($"{item.Rect} {item.Rect.Centered(item.Rect.Position)}");
            DebugDrawRect(item.Rect.Centered(), false, item.Mask);
        }
    }
    public void DebugDrawAreas()
    {
        if(DebugDrawRect is null) return;
        foreach (var item in AreaLookup.Lookup.Values)
        {
            DebugDrawRect(item.Rect.Centered(), item.OverlappingCount > 0, item.Mask);
        }
    }
}
