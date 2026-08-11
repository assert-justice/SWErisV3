using Eris;
using Eris.Renderer;
using ErisMath;
using Prion.Node;
using SpoonWitch.Rendering;

namespace SpoonWitch.Game.Map.MapObject;

public class SwMapProp : SwMapObject
{
    private readonly SwSprite Sprite;
    public SwMapProp(PriNode data) : base(data)
    {
        if(!data.TryGet("dirpath", out string dirpath)) throw new("missing dirpath");
        if(!Fields.TryGet("texture_filepath", out string filepath)) throw new("missing texture");
        if(!ErTexture.TryFromPath(Path.Join(dirpath, filepath), out var texture)) throw new("could not load texture");
        Sprite = new("sprite_" + Id);

        var properties = Fields.Get("properties_json");
        ErVec2 tileSize = ErVec2.FromPrion(properties, "tile_width", "tile_height", texture.Size);
        if(!properties.TryGet("randomize", out bool randomize)) randomize = false;
        SwAnimation animation = new("default", [..SwFrame.GetAllFrames(new(texture), tileSize)],tileSize,default);
        Sprite.AddAnimation(animation);
        Sprite.Visible = true;
        if(randomize) Sprite.FrameIdx = ErMath.FloorToInt(Random.Shared.NextDouble() * animation.NumFrames);
        // ErEngine.Log(data);
    }
    public override void Draw()
    {
        base.Draw();
        Sprite.Draw(RectPx.Center);
    }
}