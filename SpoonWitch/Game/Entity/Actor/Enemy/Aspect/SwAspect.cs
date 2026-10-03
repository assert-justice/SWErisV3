using Eris;
using Prion.Node;
using SpoonWitch.Game.Entity.Component;

namespace SpoonWitch.Game.Entity.Actor.Enemy.Aspect;

public class SwAspect: SwEnemy
{
    public SwAspect()
    {
        AddGlobalHandler("boss_wake", Wake);
    }
    public override void SetProps(PriNode props)
    {
        base.SetProps(props);
        LoadSprites("anim_data/sprites");
    }
    private void Wake(PriNode command)
    {
        GetComponent<SwSpriteComponent>("body")?.Sprite.Play();
    }
}
