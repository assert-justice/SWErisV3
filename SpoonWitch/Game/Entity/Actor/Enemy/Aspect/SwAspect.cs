using Eris;
using Eris.Input.Virtual;
using ErisMath;
using Prion.Node;
using SpoonWitch.Game.Effect;
using SpoonWitch.Game.Entity.Actor.Enemy.Aspect.State;
using SpoonWitch.Game.Entity.Actor.Enemy.Aspect.State.ArmState;
using SpoonWitch.Game.Entity.Component;
using SpoonWitch.Game.Entity.Component.Ik;
using SpoonWitch.Game.Entity.Component.Ik.AspectLegs;
using SpoonWitch.Game.Entity.Component.Ik.Tentacle;
using SpoonWitch.Game.Entity.Component.State;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Entity.Actor.Enemy.Aspect;

public class SwAspect: SwEnemy
{
    public double BaseArmSpeed = 200;
    public double WanderSpeedMul = 0.5;
    public double WanderRadius = 128;
    public readonly SwClock TimeoutClock;
    public readonly SwClock LeftArmClock;
    public readonly SwClock RightArmClock;
    public SwArmStateMachine LeftArmState = null!;
    public SwArmStateMachine RightArmState = null!;
    private SwStateMachine<SwAspect> StateMachine = null!;
    public SwAspect()
    {
        AddGlobalHandler("boss_wake", Wake);
        TimeoutClock = AddClock();
        LeftArmClock = AddClock();
        RightArmClock = AddClock();
    }
    public override void SetProps(PriNode props)
    {
        base.SetProps(props);
    }
    public override void Init()
    {
        base.Init();
        StateMachine = RegisterComponent(SwAspectState.GetStateMachine(this, "state_machine"));
        StateMachine.SetDefaultState("asleep");
        var rightArmComp = RegisterComponent(new SwTentacleComponent(this, "right_arm"));
        var leftArmComp = RegisterComponent(new SwTentacleComponent(this, "left_arm"));
        var leftArmState = RegisterComponent(SwAspectArmState.GetStateMachine(this, "left_arm_state", leftArmComp));
        var rightArmState = RegisterComponent(SwAspectArmState.GetStateMachine(this, "right_arm_state", rightArmComp));
        leftArmState.WanderRect = new(-128,-96,128,96);
        rightArmState.WanderRect = new(0,-96,128,96);
        leftArmState.AttackRect = new(-128,-96,128,192);
        rightArmState.AttackRect = new(0,-96,128,192);
        var leftVisionArea = RegisterComponent(new SwAreaComponent(this, "left_vision", (uint)SwCollisionMask.PlayerTeam, leftArmState.AttackRect.Size, leftArmState.AttackRect.Center));
        var rightVisionArea = RegisterComponent(new SwAreaComponent(this, "right_vision", (uint)SwCollisionMask.PlayerTeam, rightArmState.AttackRect.Size, rightArmState.AttackRect.Center));
        leftArmState.VisionArea = leftVisionArea;
        rightArmState.VisionArea = rightVisionArea;
        var leftHurtbox = RegisterComponent(new SwHurtboxComponent(this, "left_hurtbox"));
        var rightHurtbox = RegisterComponent(new SwHurtboxComponent(this, "right_hurtbox"));
        leftHurtbox.Mask = (uint)SwCollisionMask.PlayerTeam;
        leftHurtbox.Size = new(32,32);
        leftHurtbox.DamageCommand = Props.Get("spine_damage");
        rightHurtbox.Mask = (uint)SwCollisionMask.PlayerTeam;
        rightHurtbox.Size = new(32,32);
        rightHurtbox.DamageCommand = Props.Get("spine_damage");
        leftArmState.Hurtbox = leftHurtbox;
        rightArmState.Hurtbox = rightHurtbox;
        // var leftHurtbox = RegisterComponent
        var legs = RegisterComponent(new SwAspectLegsComponent(this, "legs"));
        legs.Offset = ErVec2.Down * 24;
        LoadSprites("anim_data/sprites");
    }
    private void Wake(PriNode command)
    {
        StateMachine.SetState("wake");
    }
    protected override double Damage(SwDamage damage)
    {
        double oldHealth = Health;
        double thresh = MaxHealth;
        var dam = base.Damage(damage);
        if(oldHealth >= thresh && Health < thresh)
        {
            StateMachine.SetState("uproot");
        }
        return dam;
    }
}
