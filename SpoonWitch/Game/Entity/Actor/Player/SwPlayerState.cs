using Eris;
using Eris.Utils;
using ErisMath;
using Prion.Node;
using SpoonWitch.ByteStream;
using SpoonWitch.Game.Entity.Component.Sprite;
using SpoonWitch.Game.Entity.Component.State;

namespace SpoonWitch.Game.Entity.Actor.Player;

public abstract class SwPlayerState(SwPlayer parent) : SwState(parent)
{
    protected readonly SwPlayer Player = parent;
    protected ErWrapper<SwSprite> BodySprite = new(() => parent.GetComponent<SwSprite>("body")!);
    protected ErWrapper<SwSprite> SpoonSprite = new(() => parent.GetComponent<SwSprite>("spoon")!);
    protected ErWrapper<SwSprite> SlingSprite = new(() => parent.GetComponent<SwSprite>("sling")!);
    protected ErWrapper<SwStateMachine> StateMachine = new(() => parent.GetComponent<SwStateMachine>("state_machine")!);
    public class Default(SwPlayer parent) : SwPlayerState(parent)
    {
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
            if(input.GetMouseButtonDown(SDL3.SDL.MouseButtonFlags.Right)) StateMachine.Value.SetState("charging");
            if(input.GetKeyDown(SDL3.SDL.Scancode.Space)) SwApp.CommandStore.AddCommand(new("damage", new PriNumber(10), Player.Id));
            ErVec2 move = new(x,y);
            if (Player.Velocity.IsNonzero())
            {
                BodySprite.Value.Play("run_2h_d");
            }
            else
            {
                BodySprite.Value.Play("idle_2h_d");
            }
            Player.Velocity = move * Player.BaseSpeed;
        }
    }
    public class Attack(SwPlayer parent) : SwPlayerState(parent)
    {
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
    public class Charging(SwPlayer parent) : SwPlayerState(parent)
    {
        private double ChargeTime;

        public override string Name => "charging";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            SlingSprite.Value.Visible = true;
            SlingSprite.Value.Play("charging");
            ChargeTime = 1;
        }
        public override void Update()
        {
            base.Update();
            if (!ErEngine.Input.GetMouseButtonDown(SDL3.SDL.MouseButtonFlags.Right))
            {
                SlingSprite.Value.Visible = false;
                SlingSprite.Value.Stop();
                StateMachine.Value.SetState("default");
                return;
            }
            if(ChargeTime > 0) ChargeTime -= SwGame.DeltaTime;
            else StateMachine.Value.SetState("charged");
        }
        public override void Read(SwByteStream byteStream)
        {
            base.Read(byteStream);
            if(!byteStream.TryReadF64(out ChargeTime)) throw new("bad charge time");
        }
        public override void Write(SwByteStream byteStream)
        {
            base.Write(byteStream);
            byteStream.WriteF64(ChargeTime);
        }
    }
    public class Charged(SwPlayer parent) : SwPlayerState(parent)
    {
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            SlingSprite.Value.Play("charged");
        }
        public override void Update()
        {
            base.Update();
            if (!ErEngine.Input.GetMouseButtonDown(SDL3.SDL.MouseButtonFlags.Right))
            {
                SlingSprite.Value.Visible = false;
                SlingSprite.Value.Stop();
                StateMachine.Value.SetState("default");
            }
        }
        public override string Name => "charged";
    }
    public static SwStateMachine GetStateMachine(SwPlayer parent, string name)
    {
        return new(parent, name, [
            new Default(parent),
            new Attack(parent),
            new Charging(parent),
            new Charged(parent),
        ]);
    }
}