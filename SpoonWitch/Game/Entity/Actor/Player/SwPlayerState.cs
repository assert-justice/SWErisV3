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
    protected ErWrapper<SwSprite> ReticleSprite = new(() => parent.GetComponent<SwSprite>("reticle")!);
    protected ErWrapper<SwStateMachine> StateMachine = new(() => parent.GetComponent<SwStateMachine>("state_machine")!);
    protected ErWrapper<SwPlayerControls> Controls = new(() => parent.GetComponent<SwPlayerControls>("controls")!);
    // private static readonly string[] Dirs = ["dr", "d", "dl", "u"];
    // name, hands, facing
    // Todo: make this less dumb. Or not, I dgaf
    private static readonly string[][][] BodyAnims = [
        [
            [
                "idle_0h_dr",
                "idle_0h_d",
                "idle_0h_dl",
                "idle_0h_u",
            ],
            [
                "idle_1h_dr",
                "idle_1h_d",
                "idle_1h_dl",
                "idle_1h_u",
            ],
            [
                "idle_2h_dr",
                "idle_2h_d",
                "idle_2h_dl",
                "idle_2h_u",
            ],
        ],
        [
            [
                "run_0h_dr",
                "run_0h_d",
                "run_0h_dl",
                "run_0h_u",
            ],
            [
                "run_1h_dr",
                "run_1h_d",
                "run_1h_dl",
                "run_1h_u",
            ],
            [
                "run_2h_dr",
                "run_2h_d",
                "run_2h_dl",
                "run_2h_u",
            ],
        ],
    ];
    public override void Update()
    {
        base.Update();
        ReticleSprite.Value.Visible = Controls.Value.ReticleVisible;
        switch (StateMachine.Value.CurrentState.Name)
        {
            case "charging":
            case "charged":
            ReticleSprite.Value.Play("aiming");
            break;
            default:
            ReticleSprite.Value.Play("still");
            break;
        }
        ReticleSprite.Value.Offset = Controls.Value.ReticlePosition;
    }
    public class Default(SwPlayer parent) : SwPlayerState(parent)
    {
        public override string Name => "default";
        public override void Update()
        {
            base.Update();
            int animIdx = Player.Velocity.IsNonzero() ? 1 : 0;
            BodySprite.Value.Play(BodyAnims[animIdx][2][Controls.Value.LastFacingIdx]);
            Player.Velocity = Controls.Value.Move * Player.BaseSpeed;
            if(Controls.Value.AttackJustPressed) StateMachine.Value.SetState("attack");
            else if(Controls.Value.IsCharging) StateMachine.Value.SetState("charging");
        }
    }
    public class Attack(SwPlayer parent) : SwPlayerState(parent)
    {
        public override string Name => "attack";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            SpoonSprite.Value.Visible = true;
            SpoonSprite.Value.Angle = (Controls.Value.LastFacingIdx - 1) * ErMath.HALF_PI;
            SpoonSprite.Value.Play();
            BodySprite.Value.Play(BodyAnims[0][0][Controls.Value.LastFacingIdx]);
            foreach (int id in SwGame.GetRectIds(GetHurtbox()))
            {
                ErEngine.Log("hit ent ", id);
            }
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
        private ErRect2 GetHurtbox()
        {
            var dir = Controls.Value.LastFacing;
            double dis = 32;
            ErVec2 size = new(32, 32);
            var pos = Parent.Position + dir * dis;
            return ErRect2.Centered(pos, size);
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
            int animIdx = Player.Velocity.IsNonzero() ? 1 : 0;
            BodySprite.Value.Play(BodyAnims[animIdx][1][Controls.Value.LastFacingIdx]);
            Player.Velocity = Controls.Value.Move * Player.BaseSpeed * Player.ChargeSpeedMul;
            if (!Controls.Value.IsCharging)
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
        public override string Name => "charged";
        public override void BeginState(string lastState)
        {
            base.BeginState(lastState);
            SlingSprite.Value.Play("charged");
        }
        public override void Update()
        {
            base.Update();
            int animIdx = Player.Velocity.IsNonzero() ? 1 : 0;
            BodySprite.Value.Play(BodyAnims[animIdx][1][Controls.Value.LastFacingIdx]);
            Player.Velocity = Controls.Value.Move * Player.BaseSpeed * Player.ChargeSpeedMul;
            if (!Controls.Value.IsCharging)
            {
                SlingSprite.Value.Visible = false;
                SlingSprite.Value.Stop();
                StateMachine.Value.SetState("default");
            }
        }
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