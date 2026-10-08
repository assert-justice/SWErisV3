using Eris;
using SpoonWitch.Game.Entity.Component;
using SpoonWitch.Rendering;

namespace SpoonWitch.Game.Entity.Actor;

public class SwTarget: SwActor
{
    public override SwCollisionMask Mask => SwCollisionMask.Sling;
    private SwSprite? Sprite;
    public override void Init()
    {
        base.Init();
        LoadSprites("sprites");
        Sprite = GetComponent<SwSpriteComponent>("sprite")?.Sprite;
        KnockbackFactor = 0;
        KnockbackTime = 0;
        InvulnTime = 0;
    }
    protected override void Update(double dt)
    {
        base.Update(dt);
        if(!IsAlive && !(Sprite?.IsPlaying ?? true)) QueueFree();
    }
    protected override void Die()
    {
        base.Die();
        Sprite?.Play();
        SwApp.CommandQueue.AddCommand(Props.Get("spawner_props/fields/on_break_json"));
    }
}
