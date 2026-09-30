using Eris;
using ErisMath;
using Prion.Node;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Map.MapData;

public readonly struct SwTileData
{
    private enum TileFlags: uint
    {
        IsWalkable = 1,
        IsOpaque = 2,
    }
    public readonly struct Entry
    {
        public int Id{get; init;}
        public string Name{get; init;}
        public string? TextureFilepath{get; init;}
        public bool IsVisible => TextureFilepath is not null;
        public bool IsWalkable => (CollisionMask & (uint)TileFlags.IsWalkable) != 0;
        public bool IsOpaque => (CollisionMask & (uint)TileFlags.IsOpaque) != 0;
        public double MoveSpeedMul{get; init;}
        public uint CollisionMask{get; init;}
        public double Arable{get; init;}
        public bool IsAnimated{get; init;}
        public double Fps{get; init;}
        public PriDict Props{get; init;}
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
            dict.TrySet("collision_mask", entry.CollisionMask);
            dict.TrySet("is_walkable", entry.IsWalkable);
            dict.TrySet("is_opaque", entry.IsOpaque);
            if(!entry.IsWalkable) dict.TrySet("move_speed_mul", 0);
            else if(entry.MoveSpeedMul != 1) dict.TrySet("move_speed_mul", entry.MoveSpeedMul);
            if(entry.IsAnimated) dict.TrySet("is_animated", true);
            if(entry.Fps != 4) dict.TrySet("fps", entry.Fps);
            if(entry.Arable > 0) dict.TrySet("arable", entry.Arable);
            entries.Add(dict);
        }
        return res;
    }
    public static bool TryFromData(out SwTileData tileData, PriNode node)
    {
        tileData = default;
        if(!SwPrion.TryGetVec2I(out var tileSize, node.Get("tile_size"))) return ErEngine.LogWarning("cannot read tile size");
        if(!node.TryGet("entries", out PriList list)) return false;
        var entries = new Entry[list.Count];
        for (int idx = 0; idx < entries.Length; idx++)
        {
            PriNode data = list.Data[idx];
            if(!data.TryGet("name", out string name)) name = string.Empty;
            if(!data.TryGet("collision_mask", out uint collision_mask)) collision_mask = 0;
            if(data.TryGet("is_walkable", out bool b))
            {
                if(b) collision_mask |= (uint)TileFlags.IsWalkable;
                else collision_mask &= (uint)~TileFlags.IsWalkable;
            }
            if(data.TryGet("is_opaque", out b))
            {
                if(b) collision_mask |= (uint)TileFlags.IsOpaque;
                else collision_mask &= (uint)~TileFlags.IsOpaque;
            }
            double move_speed_mul = 1;
            double d;
            if((collision_mask & (uint)TileFlags.IsWalkable) != 0) move_speed_mul = 0;
            else if(data.TryGet("move_speed_mul", out d)) move_speed_mul = d;

            if(!data.TryGet("is_animated", out bool is_animated)) is_animated = false;
            if(!data.TryGet("fps", out double fps)) fps = 4;
            string? textureFilepath = null;
            double arable = data.TryGet("arable", out d) ? d : 0;
            if(data.TryGet("source", out string s)) textureFilepath = s;
            entries[idx] = new()
            {
                Id = idx,
                Name = name,
                TextureFilepath = textureFilepath,
                CollisionMask = collision_mask,
                MoveSpeedMul = move_speed_mul,
                Arable = arable,
                IsAnimated = is_animated,
                Fps = fps,
            };
        }
        tileData = new()
        {
            TileSize = tileSize,
            Entries = entries,
        };
        return true;
    }
}
