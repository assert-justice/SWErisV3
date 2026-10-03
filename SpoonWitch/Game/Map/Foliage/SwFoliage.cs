using Eris;
using Eris.Renderer;
using Eris.Utils.Grid2D;
using ErisMath;
using SpoonWitch.Rendering;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Map.Foliage;

public class SwFoliage
{
    private readonly struct FoliageTile
    {
        public int FoliageId{get; init;}
        public int FirstFrameIdx{get; init;}
        public int LastFrameIdx{get; init;}
        public bool IsAlive{get; init;}
    }
    private readonly SwMap Map;
    private readonly ErHashGrid2D<FoliageTile> FoliageGrid = new();
    private readonly FoliageTile?[][] FoliageTiles;
    private readonly List<SwFrame> Frames = [];
    private readonly HashSet<ErVec2I> QueuedTileCoords = [];
    private readonly ErVec2I TileSizeFTiles;
    public SwFoliage(SwMap map)
    {
        Map = map;
        TileSizeFTiles = map.TileSize / map.FoliageData.TileSize;
        FoliageTiles = new FoliageTile?[map.FoliageData.Entries.Length][];
        foreach (var entry in map.FoliageData.Entries)
        {
            if(entry.TextureFilepath is null) continue;
            if(!ErTexture.TryFromPath(entry.TextureFilepath, out var texture)) {ErEngine.LogWarning("bad foliage texture path ", entry.TextureFilepath); continue;}
            SwTextureStore store = new(texture);
            var frameSize = (ErVec2)entry.FrameSize;
            SwTileSplitter tileSplitter = new(texture.Size, frameSize);
            // reserve variations
            FoliageTiles[entry.Id] = new FoliageTile?[tileSplitter.GridSize.X];
            for (int variantIdx = 0; variantIdx < tileSplitter.GridSize.X; variantIdx++)
            {
                int firstFrameIdx = Frames.Count;
                for (int frameIdx = 0; frameIdx < tileSplitter.GridSize.Y; frameIdx++)
                {
                    if(!tileSplitter.TryGetTile(out var frameRect, new ErVec2I(variantIdx, frameIdx))) throw new("should be unreachable");
                    Frames.Add(new(store, frameRect));
                }
                int lastFrameIdx = Frames.Count - 1;
                if(lastFrameIdx >= firstFrameIdx)
                {
                    FoliageTiles[entry.Id][variantIdx] = new()
                    {
                        FoliageId = entry.Id,
                        FirstFrameIdx = firstFrameIdx,
                        LastFrameIdx = lastFrameIdx,
                        IsAlive = entry.IsAlive,
                    };
                }
            }
        }
    }
    public void Draw()
    {
        UpdateQueuedTiles();
        var tileSize = (ErVec2)Map.FoliageData.TileSize;
        foreach (var (fCoord, fTile) in FoliageGrid.Entries)
        {
            var pos = tileSize * (ErVec2)fCoord;
            var frame = Frames[fTile.FirstFrameIdx];
            frame.Draw(pos);
        }
    }
    public void QueueTileUpdate(ErVec2I tileCoord)
    {
        QueuedTileCoords.Add(tileCoord);
    }
    public void SeedArea(ErRect2I tileRect)
    {
        foreach (var tileCoord in tileRect.GetInnerCoords())
        {
            SeedTile(tileCoord);
        }
    }
    public void SeedTile(ErVec2I tileCoord)
    {
        UpdateQueuedTiles();
        int tileId = Map.GetTopTileId(tileCoord);
        int fId = -1;
        double arable = 0;
        if(tileId >= 0 && Map.TileData[tileId].Arable > 0)
        {
            fId = 0;
            arable = Map.TileData[tileId].Arable;
        }
        var fRect = new ErRect2I(tileCoord * TileSizeFTiles, TileSizeFTiles);
        foreach (var fCoord in fRect.GetInnerCoords())
        {
            SetFoliageTile(fId, fCoord, arable);
        }
    }
    public void LifeSimArea(ErRect2I tileRect, int steps)
    {
        UpdateQueuedTiles();
        var fRect = tileRect * TileSizeFTiles;
        Queue<(int fId, ErVec2I fCoord)> deltas = [];
        for (int stepIdx = 0; stepIdx < steps; stepIdx++)
        {
            foreach (var fCoord in fRect.GetInnerCoords())
            {
                int fId = -1;
                if(FoliageGrid.TryGet(fCoord, out var fTile)) fId = fTile.FoliageId;
                int nextId = fId;
                int adjLiving = 0;
                var neighbors = fCoord.GetAdj();
                foreach (var n in neighbors)
                {
                    if(FoliageGrid.TryGet(n, out fTile) && fTile.IsAlive) adjLiving++;
                }
                if(fId < 0 && adjLiving == 3) nextId = 0;
                else if(fId >= 0 && (adjLiving < 3 || adjLiving > 3)) nextId = -1;
                if(fId != nextId) deltas.Enqueue((nextId, fCoord));
            }
            while(deltas.TryDequeue(out var delta))
            {
                SetFoliageTile(delta.fId, delta.fCoord);
            }
        }
    }
    public void TrimArea(ErRect2I tileRect)
    {
        UpdateQueuedTiles();
        var fRect = tileRect * TileSizeFTiles;
        foreach (var fCoord in fRect.GetInnerCoords())
        {
            if(!FoliageGrid.ContainsCoord(fCoord)) continue;
            var neighbors = fCoord.GetAdj();
            foreach (var n in neighbors)
            {
                var tileCoord = n / TileSizeFTiles;
                int tileId = Map.GetTopTileId(tileCoord);
                if(tileId < 0 || Map.TileData[tileId].Arable <= 0) FoliageGrid.Remove(fCoord);
            }
        }
    }
    private void SetFoliageTile(int fId, ErVec2I fCoord)
    {
        if(fId < 0)
        {
            FoliageGrid.Remove(fCoord);
            return;
        }
        ErRandom random = new((ulong)fCoord.GetHashCode());
        var variants = FoliageTiles[fId];
        if(variants is null || variants.Length == 0){ErEngine.LogWarning("bad foliage variant, fId: ", fId); return;}
        var tile = random.PickRandom(variants, out int variantIdx);
        if(tile is null) {ErEngine.LogWarning("bad foliage tile idx, fId: ", fId, " variant idx: ", variantIdx); return;}
        FoliageGrid.Set(fCoord, tile.Value);
    }
    private void SetFoliageTile(int fId, ErVec2I fCoord, double arable)
    {
        if(fId < 0)
        {
            FoliageGrid.Remove(fCoord);
            return;
        }
        ErRandom random = new((ulong)fCoord.GetHashCode());
        if(random.Random() > arable)
        {
            FoliageGrid.Remove(fCoord);
            return;
        }
        var variants = FoliageTiles[fId];
        if(variants is null || variants.Length == 0){ErEngine.LogWarning("bad foliage variant, fId: ", fId); return;}
        var tile = random.PickRandom(variants, out int variantIdx);
        if(tile is null) {ErEngine.LogWarning("bad foliage tile idx, fId: ", fId, " variant idx: ", variantIdx); return;}
        FoliageGrid.Set(fCoord, tile.Value);
    }
    private void UpdateQueuedTiles()
    {
        if(QueuedTileCoords.Count == 0) return;
        foreach (var tileCoord in QueuedTileCoords)
        {
            ErRandom random = new((ulong)tileCoord.GetHashCode());
            var fTileCoord = tileCoord*TileSizeFTiles;
            ErRect2I fTileRect = new(fTileCoord, TileSizeFTiles);
            int tileId = Map.GetTopTileId(tileCoord);
            if(tileId < 0 || Map.TileData[tileId].Arable <= 0)
            {
                // clear coords
                foreach (var fCoord in fTileRect.GetInnerCoords())
                {
                    FoliageGrid.Remove(fCoord);
                }
                continue;
            }
            var tileData = Map.TileData[tileId];
            // for now just hardcode this
            int fId = 0;
            foreach (var fCoord in fTileRect.GetInnerCoords())
            {
                if(random.Random() > tileData.Arable) SetFoliageTile(-1, fCoord);
                else SetFoliageTile(fId, fCoord);
            }
        }
        QueuedTileCoords.Clear();
    }
    // private static IEnumerable<ErVec2I> GetNeighbors(ErVec2I fTileCoord)
    // {
    //     for (int xi = -1; xi < 2; xi++)
    //     {
    //         for (int yi = -1; yi < 2; yi++)
    //         {
    //             if(xi == 0 && yi == 0) continue;
    //             ErVec2I coord = new ErVec2I(xi,yi) + fTileCoord;
    //             yield return coord;
    //         }
    //     }
    // }
}

// using Eris;
// using Eris.Renderer;
// using ErisMath;
// using SpoonWitch.Rendering;

// namespace SpoonWitch.Game.Map.Foliage;

// public class SwFoliage
// {
//     private static readonly string GrassPath = "game_data/map/props/grass_tufts.png";
//     private static readonly ErVec2 GrassSize = new(7,7);
//     private static readonly ErVec2I FoliageTileSize = new(8,8);
//     private static readonly ErVec2I TileSizePx = new(32,32);
//     private static ErVec2I TileSizeFTiles;
//     private static readonly double GrassChance = 0.5;
//     private readonly Dictionary<ErVec2I,(int typeId,ErVec2 pos,SwFrame? frame)> FoliageLookup = [];
//     private readonly Dictionary<int, List<SwFrame>> FrameLookup = [];
//     private readonly Queue<(ErVec2I coord, int tileId)> SetQueue = [];
//     public SwFoliage()
//     {
//         if(!ErTexture.TryFromPath(GrassPath, out var texture))
//         {
//             ErEngine.LogWarning("bad grass path");
//             return;
//         }
//         TileSizeFTiles = TileSizePx / FoliageTileSize;
//         List<SwFrame> frames =  [..SwFrame.GetAllFrames(new(texture), GrassSize)];
//         FrameLookup[0] = frames;
//     }
//     public void SetArable(ErVec2I tileCoord, bool arable)
//     {
//         if (arable)
//         {
//             foreach (var fTileCoord in GetCoordsInTile(tileCoord))
//             {
//                 SetQueue.Enqueue((fTileCoord, IsAliveInit(fTileCoord) ? 0 : -1));
//             }
//             Update();
//         }
//         else
//         {
//             foreach (var fTileCoord in GetCoordsInTile(tileCoord))
//             {
//                 FoliageLookup.Remove(fTileCoord);
//             }
//         }
//     }
//     private void Update()
//     {
//         while(SetQueue.TryDequeue(out var result))
//         {
//             var (coord,typeId) = result;
//             ErVec2 pos = (ErVec2)(coord * FoliageTileSize);
//             SwFrame? frame = null;
//             if(FrameLookup.TryGetValue(typeId, out var frames) && frames.Count > 0)
//             {
//                 int frameIdx = ErMath.Mod(coord.GetHashCode(), frames.Count);
//                 frame = frames[frameIdx];
//             }
//             FoliageLookup[coord] = (typeId, pos, frame);
//         }
//     }
//     public void Draw()
//     {
//         Update();
//         foreach (var (_, pos, frame) in FoliageLookup.Values)
//         {
//             frame?.Draw(pos);
//         }
//     }
//     public void LifeSimTrim()
//     {
//         Update();
//         foreach (var (coord, value) in FoliageLookup)
//         {
//             int tileId = value.typeId;
//             int nextId = tileId;
//             int adjLiving = 0;// CountLivingNeighbors(coord);
//             var neighbors = GetNeighbors(coord);
//             foreach (var item in neighbors)
//             {
//                 if(!FoliageLookup.TryGetValue(item, out var val))
//                 {
//                     adjLiving = 0;
//                     break;
//                 }
//                 if(val.typeId >= 0) adjLiving++;
//             }
//             if(tileId < 0)
//             {
//                 if(adjLiving == 3) nextId = 0;
//             }
//             else if(adjLiving < 3 || adjLiving > 3) nextId = -1;
//             if(tileId != nextId) SetQueue.Enqueue((coord,nextId));
//         }
//         Update();
//     }
//     private int CountLivingNeighbors(ErVec2I fTileCoord)
//     {
//         int living = 0;
//         foreach (var coord in GetNeighbors(fTileCoord))
//         {
//             if(!FoliageLookup.TryGetValue(coord, out var value)) continue;
//             if(value.typeId < 0) continue;
//             living++;
//         }
//         // for (int xi = -1; xi < 2; xi++)
//         // {
//         //     for (int yi = -1; yi < 2; yi++)
//         //     {
//         //         if(xi == 0 && yi == 0) continue;
//         //         ErVec2I coord = new ErVec2I(xi,yi) + fTileCoord;
//         //         if(!FoliageLookup.TryGetValue(coord, out var value)) continue;
//         //         if(value.typeId < 0) continue;
//         //         living++;
//         //     }
//         // }
//         return living;
//     }
//     private static bool IsAliveInit(ErVec2I fTileCoord)
//     {
//         double val = (double) Math.Abs(fTileCoord.GetHashCode()) / int.MaxValue;
//         return val < GrassChance;
//     }
//     private static IEnumerable<ErVec2I> GetCoordsInTile(ErVec2I tileCoord)
//     {
//         ErVec2I fTileCoord = tileCoord * TileSizeFTiles;
//         for (int xi = 0; xi < TileSizeFTiles.X; xi++)
//         {
//             for (int yi = 0; yi < TileSizeFTiles.Y; yi++)
//             {
//                 ErVec2I coord = fTileCoord + new ErVec2I(xi,yi);
//                 yield return coord;
//             }
//         }
//     }
//     private static IEnumerable<ErVec2I> GetNeighbors(ErVec2I fTileCoord)
//     {
//         for (int xi = -1; xi < 2; xi++)
//         {
//             for (int yi = -1; yi < 2; yi++)
//             {
//                 if(xi == 0 && yi == 0) continue;
//                 ErVec2I coord = new ErVec2I(xi,yi) + fTileCoord;
//                 yield return coord;
//             }
//         }
//     }
// }