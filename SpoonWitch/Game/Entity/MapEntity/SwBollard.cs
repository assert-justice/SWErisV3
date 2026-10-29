using Eris;
using ErisMath;
using Prion.Node;
using SpoonWitch.Game.Entity.Component;

namespace SpoonWitch.Game.Entity.MapEntity;

public class SwBollard: SwMapEntity
{
    private SwSpriteComponent Sprite = null!;
    private bool WasUp = false;
    public SwBollard()
    {
        AddGlobalHandler("bollard_update", HandleBollardUpdate);
    }
    public override void Init()
    {
        base.Init();
        LoadSprites("sprites");
        Sprite = GetComponent<SwSpriteComponent>("sprite")!;
        Sprite.Sprite.Visible = false;
        Sprite.Sprite.Centered = false;
        if(!Props.TryGet("fields/start_up", out bool startUp)) startUp = true;
        bool isUp;
        if(Game.Flags.TryGetValue(Iid, out int flag)) isUp = flag != 0;
        else
        {
            Game.Flags[Iid] = startUp ? 1 : 0;
            isUp = startUp;
        }
        SetIsUp(isUp);
    }
    private void HandleBollardUpdate(PriNode _)
    {
        bool isUp = Game.Flags.TryGetValue(Iid, out int i) && i != 0;
        // bool wasUp = Body.Mask != 0;
        if(isUp != WasUp) SetIsUp(isUp);
    }
    private void SetIsUp(bool isUp)
    {
        WasUp = isUp;
        uint mask = isUp ? (uint)SwCollisionMask.BlocksNav : 0;
        foreach (var tileCoord in RectTiles.GetInnerCoords())
        {
            Game.PhysicsWorld.SetTileMask(tileCoord, mask);
        }
        // Body.Mask = isUp ? (uint)SwCollisionMask.BlocksNav : 0;
        // if(isUp) Game.PhysicsWorld.MoveAndSlide(0.16, Body);
        // else Game.PhysicsWorld.RemoveBody(Body.Id);
        Sprite.Sprite.Play(isUp ? "extend" : "retract");
    }
    protected override void Draw()
    {
        base.Draw();
        Sprite.Sprite.Visible = true;
        foreach (var item in RectTiles.GetInnerCoords())
        {
            var p = (ErVec2)(item * Game.Map.TileSize);
            Sprite.Sprite.Draw(p);
        }
        Sprite.Sprite.Visible = false;
    }
}
