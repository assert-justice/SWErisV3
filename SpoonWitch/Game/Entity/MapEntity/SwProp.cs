using Eris;
using Eris.Renderer;
using ErisMath;

namespace SpoonWitch.Game.Entity.MapEntity;

public class SwProp: SwMapEntity
{
    private ErTexture? Texture;
    public override void Init()
    {
        base.Init();
        var fields = Props.Get("fields");
        if(fields.TryGet("texture_filepath", out string texture_filepath))
        {
            if(!ErTexture.TryFromPath(texture_filepath, out Texture))
            {
                ErEngine.LogWarning("unable to read texture from path: ", texture_filepath);
            }
        }
        if(fields.TryGet("prop_collision_mode", out string mode))
        {
            if(mode == "tile_aligned")
            {
                uint mask = (uint)SwCollisionMask.Solid;
                foreach (var tileCoord in RectTiles.GetInnerCoords())
                {
                    Game.PhysicsWorld.SetTileMask(tileCoord,mask);
                }
            }
            else ErEngine.LogWarning("unsupported prop collision mode: ", mode);
        }
    }
    protected override void Draw()
    {
        base.Draw();
        if(Texture is null) return;
        ErRect2 texRect = ErRect2.Centered(Position,Texture.Size);
        Texture.Draw(texRect.Position);
    }
}
