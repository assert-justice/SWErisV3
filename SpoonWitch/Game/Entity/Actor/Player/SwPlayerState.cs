using Eris;
using Eris.Utils;
using ErisMath;
using SpoonWitch.Game.Entity.Component.Sprite;
using SpoonWitch.Game.Entity.Component.State;

namespace SpoonWitch.Game.Entity.Actor.Player;

public abstract class SwPlayerState : SwState
{
    protected readonly SwPlayer Player;
    protected SwPlayerState(SwPlayer parent) : base(parent)
    {
        Player = parent;
        BodySprite = new(()=>parent.GetComponent<SwSprite>("body")!);
        SpoonSprite = new(()=>parent.GetComponent<SwSprite>("spoon")!);
        SlingSprite = new(()=>parent.GetComponent<SwSprite>("sling")!);
        StateMachine = new(()=>parent.GetComponent<SwStateMachine>("state_machine")!);
    }
    protected ErWrapper<SwSprite> BodySprite;
    protected ErWrapper<SwSprite> SpoonSprite;
    protected ErWrapper<SwSprite> SlingSprite;
    protected ErWrapper<SwStateMachine> StateMachine;
    public class Default : SwPlayerState
    {
        public Default(SwPlayer parent) : base(parent)
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
            if(input.GetMouseButtonDown(SDL3.SDL.MouseButtonFlags.Left)) StateMachine.Value.SetState("attack");
            ErVec2 move = new(x,y);
            if (Player.Velocity.IsNonzero())
            {
                BodySprite.Value.Play("run_2h_d");
            }
            else
            {
                BodySprite.Value.Play("idle_2h_d");
            }
            Player.Velocity = move * Player.Speed;
        }
    }
    public class Attack : SwPlayerState
    {
        public Attack(SwPlayer parent) : base(parent)
        {
        }
        public override string Name => "attack";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            SpoonSprite.Value.Visible = true;
            SpoonSprite.Value.Play();
            Player.Velocity = ErVec2.Zero;
        }
        public override void Update()
        {
            base.Update();
            if(!SpoonSprite.Value.IsPlaying) StateMachine.Value.SetState("default");
        }
        public override void EndState(string nextState)
        {
            base.EndState(nextState);
            SpoonSprite.Value.Visible = false;
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