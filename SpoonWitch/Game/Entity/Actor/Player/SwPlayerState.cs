using Eris;
using ErisMath;
using SpoonWitch.Game.Entity.Component.Sprite;
using SpoonWitch.Game.Entity.Component.State;

namespace SpoonWitch.Game.Entity.Actor.Player;

public abstract class SwPlayerState : SwState
{
    protected readonly SwActor Actor;
    protected SwPlayerState(SwActor parent) : base(parent)
    {
        Actor = parent;
    }
    private SwSprite? _BodySprite;
    protected SwSprite? BodySprite
    {
        get
        {
            if(_BodySprite is null && Parent.TryGetComponent("body", out SwSprite body)) _BodySprite = body;
            return _BodySprite;
        }
    }
    private SwSprite? _SpoonSprite;
    protected SwSprite? SpoonSprite
    {
        get
        {
            if(_SpoonSprite is null && Parent.TryGetComponent("spoon", out SwSprite spoon)) _SpoonSprite = spoon;
            return _SpoonSprite;
        }
    }
    private SwSprite? _SlingSprite;
    protected SwSprite? SlingSprite
    {
        get
        {
            if(_SlingSprite is null && Parent.TryGetComponent("sling", out SwSprite sling)) _SlingSprite = sling;
            return _SlingSprite;
        }
    }
    private SwStateMachine? _StateMachine;
    protected SwStateMachine? StateMachine
    {
        get
        {
            if(_StateMachine is null && Parent.TryGetComponent("state_machine", out SwStateMachine machine)) _StateMachine = machine;
            return _StateMachine;
        }
    }
    public class Default : SwPlayerState
    {
        public Default(SwActor parent) : base(parent)
        {
        }
        public override string Name => "default";
        public override void Update()
        {
            base.Update();
            // handle movement
            var input = ErEngine.Input;
            double x = 0;
            double y = 0;
            if(input.GetKeyDown(SDL3.SDL.Scancode.A)) x-=1;
            if(input.GetKeyDown(SDL3.SDL.Scancode.D)) x+=1;
            if(input.GetKeyDown(SDL3.SDL.Scancode.W)) y-=1;
            if(input.GetKeyDown(SDL3.SDL.Scancode.S)) y+=1;
            if(input.GetMouseButtonDown(SDL3.SDL.MouseButtonFlags.Left)) StateMachine?.SetState("attack");
            ErVec2 move = new(x,y);
            // ErEngine.Log(Parent.Velocity);
            if (Actor.Velocity.IsNonzero())
            {
                BodySprite?.Play("run_2h_d");
            }
            else
            {
                BodySprite?.Play("idle_2h_d");
            }
            Actor.Velocity = move * Actor.Speed;
        }
    }
    public class Attack : SwPlayerState
    {
        public Attack(SwActor parent) : base(parent)
        {
        }
        public override string Name => "attack";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            SpoonSprite?.Visible = true;
            SpoonSprite?.Play();
        }
        public override void Update()
        {
            base.Update();
            if(SpoonSprite is not null && SpoonSprite.IsPlaying){}
            else StateMachine?.SetState("default");
        }
        public override void EndState(string nextState)
        {
            base.EndState(nextState);
            SpoonSprite?.Visible = false;
        }
    }
    public static SwStateMachine GetPlayerStateMachine(SwPlayer parent, string name)
    {
        return new(parent, name, [
            new Default(parent),
            new Attack(parent),
        ]);
    }
}