using Prion.Node;

namespace SpoonWitch.Game.Map.MapData;

public readonly struct SwTileData2
{
    private enum TileFlags: uint
    {
        IsWalkable = 1,
        IsOpaque = 2,
    }
    public int Id{get; init;}
    public bool IsWalkable => (CollisionMask & (uint)TileFlags.IsWalkable) != 0;
    public bool IsOpaque => (CollisionMask & (uint)TileFlags.IsOpaque) != 0;
    public double MoveSpeedMul{get; init;}
    public uint CollisionMask{get; init;}
    public bool IsArable{get; init;}
    public bool IsAnimated{get; init;}
    public double Fps{get; init;}
    public PriDict Props{get; init;}
    public static bool TryFromData(out SwTileData2 tileData, PriNode data)
    {
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
        if((collision_mask & (uint)TileFlags.IsWalkable) != 0) move_speed_mul = 0;
        else if(data.TryGet("move_speed_mul", out double d)) move_speed_mul = d;
        if(!data.TryGet("is_animated", out bool is_animated)) is_animated = false;
        if(!data.TryGet("fps", out double fps)) fps = 4;
        tileData = new()
        {
            CollisionMask = collision_mask,
            MoveSpeedMul = move_speed_mul,
            IsArable = data.TryGet("is_arable", out b) && b,
        };
        return true;
    }
}
