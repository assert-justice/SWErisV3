using Eris.Utils;
using SpoonWitch.Game.Entity.Component.Sprite;
using SpoonWitch.Game.Entity.Component.State;

namespace SpoonWitch.Game.Entity.Actor.Enemy.Slume;

public abstract class SwSlumeState : SwState
{
    protected readonly SwSlume Slume;
    protected ErWrapper<SwSprite> Sprite;
    protected SwSlumeState(SwSlume parent) : base(parent)
    {
        Slume = parent;
        Sprite = new(()=>Slume.GetComponent<SwSprite>("body")!);
    }
    private class Default : SwSlumeState
    {
        public Default(SwSlume parent) : base(parent)
        {
        }
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            Sprite.Value.Play("idle_d");
        }
        public override string Name => "default";
    }
    public static SwStateMachine GetStateMachine(SwSlume parent, string name)
    {
        return new(parent, name, [
            new Default(parent),
        ]);
    }
}