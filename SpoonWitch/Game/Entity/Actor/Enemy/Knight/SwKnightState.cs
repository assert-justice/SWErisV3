using Eris.Utils;
using ErisMath;
using SpoonWitch.Game.Entity.Component.Sprite;
using SpoonWitch.Game.Entity.Component.State;

namespace SpoonWitch.Game.Entity.Actor.Enemy.Knight;

public abstract class SwKnightState(SwKnight parent) : SwState(parent)
{
    protected readonly SwKnight Knight = parent;
    protected ErWrapper<SwSprite> BodySprite = new(() => parent.GetComponent<SwSprite>("body")!);
    protected ErWrapper<SwSprite> SwordSprite = new(() => parent.GetComponent<SwSprite>("sword")!);
    protected ErWrapper<SwStateMachine> StateMachine = new(() => parent.GetComponent<SwStateMachine>("state_machine")!);
    private static readonly string[][] BodyAnims = [
        [
            "move_0h_dr",
            "move_0h_d",
            "move_0h_dl",
            "move_0h_u",
        ],
        [
            "move_1h_dr",
            "move_1h_d",
            "move_1h_dl",
            "move_1h_u",
        ],
        [
            "move_2h_dr",
            "move_2h_d",
            "move_2h_dl",
            "move_2h_u",
        ],
    ];
    private class Default(SwKnight parent) : SwKnightState(parent)
    {
        public override string Name => "default";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            Knight.Velocity = ErVec2.Zero;
        }
        public override void Update()
        {
            base.Update();
            BodySprite.Value.Play("move_2h_d");
        }
    }
    private class Wandering(SwKnight parent) : SwKnightState(parent)
    {
        public override string Name => "wandering";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            Knight.Velocity = ErVec2.Zero;
        }
        public override void Update()
        {
            base.Update();
            BodySprite.Value.Play("move_2h_d");
        }
    }
    private class Knockback(SwKnight parent) : SwKnightState(parent)
    {
        public override string Name => "knockback";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            BodySprite.Value.Play("death");
            BodySprite.Value.Stop();
        }
        public override void Update()
        {
            base.Update();
            double speed = Knight.Velocity.GetLength();
            if(speed > ErMath.EPSILON) Knight.Velocity = Knight.Velocity.Normalized() * speed * 0.95;
            if(Knight.IsKnockback) return;
            if(Knight.IsAlive) StateMachine.Value.SetState(Knight.IsPassive ? "default" : "wandering");
            else StateMachine.Value.SetState("dead");
        }
    }
    private class Dead(SwKnight parent) : SwKnightState(parent)
    {
        public override string Name => "dead";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            Knight.Velocity = ErVec2.Zero;
            BodySprite.Value.Play("death");
        }
        public override void Update()
        {
            base.Update();
        }
    }
    public static SwStateMachine GetStateMachine(SwKnight parent, string name)
    {
        return new(parent, name, [
            // default
            new Default(parent),
            // wandering
            new Wandering(parent),
            // chasing
            // attacking
            // knockback
            new Knockback(parent),
            // dead
            new Dead(parent),
        ]);
    }
}