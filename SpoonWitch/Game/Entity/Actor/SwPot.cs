using Eris.Renderer;

namespace SpoonWitch.Game.Entity.Actor;

public class SwPot: SwActor
{
    public override SwCollisionMask Mask => SwCollisionMask.BlocksNav | SwCollisionMask.Spoon;
    private ErTexture? Texture;
    public override void Init()
    {
        base.Init();
        KnockbackFactor = 0;
        ErTexture.TryFromPath("game_data/map/props/pot.png", out Texture);
    }
    protected override void Draw()
    {
        base.Draw();
        Texture?.Draw(Position - Texture.Size * 0.5);
    }
    protected override void Update(double dt)
    {
        base.Update(dt);
        if(!IsAlive && !IsInvuln) QueueFree();
    }
}
