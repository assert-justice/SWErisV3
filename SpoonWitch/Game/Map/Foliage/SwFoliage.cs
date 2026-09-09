using Eris;
using Eris.Renderer;
using ErisMath;
using SpoonWitch.Rendering;

namespace SpoonWitch.Game.Map.Foliage;

public class SwFoliage
{
    private static readonly string GrassPath = "game_data/map/props/grass_tufts.png";
    private static readonly ErVec2 GrassSize = new(7,7);
    private static readonly ErVec2I FoliageTileSize = new(8,8);
    private static readonly ErVec2I TileSizePx = new(32,32);
    private static ErVec2I TileSizeFTiles;
    private static readonly double GrassChance = 0.5;
    private readonly Dictionary<ErVec2I,(int typeId,ErVec2 pos,SwFrame? frame)> FoliageLookup = [];
    private readonly Dictionary<int, List<SwFrame>> FrameLookup = [];
    private readonly Queue<(ErVec2I coord, int tileId)> SetQueue = [];
    public SwFoliage()
    {
        if(!ErTexture.TryFromPath(GrassPath, out var texture))
        {
            ErEngine.LogWarning("bad grass path");
            return;
        }
        TileSizeFTiles = TileSizePx / FoliageTileSize;
        List<SwFrame> frames =  [..SwFrame.GetAllFrames(new(texture), GrassSize)];
        FrameLookup[0] = frames;
    }
    public void SetArable(ErVec2I tileCoord, bool arable)
    {
        if (arable)
        {
            foreach (var fTileCoord in GetCoordsInTile(tileCoord))
            {
                SetQueue.Enqueue((fTileCoord, IsAliveInit(fTileCoord) ? 0 : -1));
            }
            Update();
        }
        else
        {
            foreach (var fTileCoord in GetCoordsInTile(tileCoord))
            {
                FoliageLookup.Remove(fTileCoord);
            }
        }
    }
    private void Update()
    {
        while(SetQueue.TryDequeue(out var result))
        {
            var (coord,typeId) = result;
            ErVec2 pos = (ErVec2)(coord * FoliageTileSize);
            SwFrame? frame = null;
            if(FrameLookup.TryGetValue(typeId, out var frames) && frames.Count > 0)
            {
                int frameIdx = ErMath.Mod(coord.GetHashCode(), frames.Count);
                frame = frames[frameIdx];
            }
            FoliageLookup[coord] = (typeId, pos, frame);
        }
    }
    public void Draw()
    {
        Update();
        foreach (var (_, pos, frame) in FoliageLookup.Values)
        {
            frame?.Draw(pos);
        }
    }
    public void LifeSim()
    {
        Update();
        foreach (var (coord, value) in FoliageLookup)
        {
            int tileId = value.typeId;
            int nextId = tileId;
            int adjLiving = CountLivingNeighbors(coord);
            if(tileId < 0)
            {
                if(adjLiving == 3) nextId = 0;
            }
            else if(adjLiving < 3 || adjLiving > 3) nextId = -1;
            if(tileId != nextId) SetQueue.Enqueue((coord,nextId));
        }
        Update();
    }
    private int CountLivingNeighbors(ErVec2I fTileCoord)
    {
        int living = 0;
        for (int xi = -1; xi < 2; xi++)
        {
            for (int yi = -1; yi < 2; yi++)
            {
                if(xi == 0 && yi == 0) continue;
                ErVec2I coord = new ErVec2I(xi,yi) + fTileCoord;
                if(!FoliageLookup.TryGetValue(coord, out var value)) continue;
                if(value.typeId < 0) continue;
                living++;
            }
        }
        return living;
    }
    private static bool IsAliveInit(ErVec2I fTileCoord)
    {
        double val = (double) Math.Abs(fTileCoord.GetHashCode()) / int.MaxValue;
        return val < GrassChance;
    }
    private static IEnumerable<ErVec2I> GetCoordsInTile(ErVec2I tileCoord)
    {
        ErVec2I fTileCoord = tileCoord * TileSizeFTiles;
        for (int xi = 0; xi < TileSizeFTiles.X; xi++)
        {
            for (int yi = 0; yi < TileSizeFTiles.Y; yi++)
            {
                ErVec2I coord = fTileCoord + new ErVec2I(xi,yi);
                yield return coord;
            }
        }
    }
}