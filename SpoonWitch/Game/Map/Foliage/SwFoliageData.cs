using Eris;
using ErisMath;
using Prion.Node;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Map.Foliage;

public readonly struct SwFoliageData
{
    public readonly struct Entry
    {
        public int Id{get; init;}
        public string Name{get; init;}
        public string? TextureFilepath{get; init;}
        public ErVec2I FrameSize{get; init;}
        public bool IsAlive{get; init;}
    }
    public ErVec2I TileSize{get; init;}
    public Entry[] Entries{get; init;}
    public PriNode ToPri()
    {
        PriDict res = [];
        SwPrion.TrySetVec2I(res, "tile_size", TileSize);
        PriList entries = [];
        res.TrySet("entries", entries);
        foreach (var entry in Entries)
        {
            PriDict dict = [];
            if(!string.IsNullOrWhiteSpace(entry.Name)) dict.TrySet("name", entry.Name);
            if(entry.TextureFilepath is not null) dict.TrySet("source", entry.TextureFilepath);
            else dict.Add("source", PriNull.Null);
            if(entry.FrameSize != TileSize) SwPrion.TrySetVec2I(dict, "frame_size", entry.FrameSize);
            dict.TrySet("is_alive", entry.IsAlive);
            entries.Add(dict);
        }
        return res;
    }
    public static bool TryFromData(out SwFoliageData foliageData, PriNode node)
    {
        foliageData = default;
        if(!SwPrion.TryGetVec2I(out var tileSize, node.Get("tile_size"))) return ErEngine.LogWarning("foliage data missing tile size");
        if(!node.TryGet("entries", out PriList list)) return ErEngine.LogWarning("foliage data missing entries");
        Entry[] entries = new Entry[list.Count];
        for (int idx = 0; idx < entries.Length; idx++)
        {
            var entry = list.Data[idx];
            string? textureFilepath = null;
            if(entry.TryGet("source", out string s)) textureFilepath = s;
            bool isAlive = true;
            if(entry.TryGet("is_alive", out bool b)) isAlive = b;
            if(!entry.TryGet("name", out string name)) name = string.Empty;
            entries[idx] = new()
            {
                Id = idx,
                Name = name,
                TextureFilepath = textureFilepath,
                FrameSize = SwPrion.GetVec2I(entry.Get("frame_size"), defaultVec: tileSize),
                IsAlive = isAlive,
            };
        }
        foliageData = new()
        {
            TileSize = tileSize,
            Entries = entries,
        };
        return true;
    }
}
