using Eris;
using Eris.Renderer;
using ErisMath;
using Prion.Node;
using SpoonWitch.Rendering;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Map.MapObject;

public class SwMapProp : SwMapObject
{
    private readonly SwSprite Sprite;
    public readonly bool IsSolid;
    public SwMapProp(PriNode data) : base(data)
    {
        if(!data.TryGet("dirpath", out string dirpath)) throw new("missing dirpath");
        if(!Fields.TryGet("texture_filepath", out string filepath)) throw new("missing texture");
        if(!ErTexture.TryFromPath(Path.Join(dirpath, filepath), out var texture)) throw new("could not load texture");
        IsSolid = Fields.TryGet("prop_collision_mode", out string collisionMode) && collisionMode == "tile_aligned";
        Sprite = new("sprite_" + Id);
        var properties = Fields.Get("properties_json");
        ErVec2 tileSize = SwPrion.GetVec2(properties, "tile_width", "tile_height", texture.Size);
        if(!properties.TryGet("randomize", out bool randomize)) randomize = false;
        SwAnimation animation = new("default", [..SwFrame.GetAllFrames(new(texture), tileSize)],tileSize,default);
        Sprite.AddAnimation(animation);
        if(randomize) Sprite.FrameIdx = ErMath.FloorToInt(Random.Shared.NextDouble() * animation.NumFrames);
    }
    public override void Load()
    {
        base.Load();
        if (IsSolid)
        {
            PriDict command = [];
            command.TrySet("verb", "set_collision_tile_rect");
            command.TrySet("tile_id", 0);
            command.TrySet("x", RectTiles.Position.X);
            command.TrySet("y", RectTiles.Position.Y);
            command.TrySet("w", RectTiles.Size.X);
            command.TrySet("h", RectTiles.Size.Y);
            SwApp.CommandStore.AddGlobalCommand(command);
        }
    }
    public override void Draw()
    {
        base.Draw();
        Sprite.Draw(RectPx.Center);
    }
}