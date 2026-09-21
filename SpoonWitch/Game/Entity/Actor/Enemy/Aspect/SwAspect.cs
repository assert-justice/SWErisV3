using Eris;
using Prion.Node;
using SpoonWitch.Game.Entity.Component;

namespace SpoonWitch.Game.Entity.Actor.Enemy.Aspect;

public class SwAspect: SwEnemy
{
    public SwAspect()
    {
        TryLoadSprites("game_data/entities/actors/aspect/aspect_anim_data.json");
        AddGlobalHandler("boss_wake", Wake);
    }
    public override void SetProps(PriNode props)
    {
        base.SetProps(props);
        ErEngine.Log(props);
    }
    public override void Ready()
    {
        base.Ready();
    }
    public override void Update()
    {
        base.Update();
    }
    private void Wake(PriNode command)
    {
        GetComponent<SwSpriteComponent>("body")?.Sprite.Play();
    }
}
