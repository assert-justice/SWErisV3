using ErisMath;
using SpoonWitch.Data;
using SpoonWitch.Game.Entity.Projectile;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Entity.Actor.Player.PlayerState;
public class SwPlayerCharging: SwPlayerState
{
    public override string Name => "charging";
    private const int NumPhases = 3;
    private int Phase = 0;
    public override void BeginState(string lastState)
    {
        base.BeginState(lastState);
        SlingSprite.Visible = true;
        SlingSprite.Play("charging");
        ReticleSprite.Play(ReticleAnimations[1]);
        Entity.SlingChargeClock.Start(Entity.SlingChargeTime / NumPhases);
        Phase = 0;
    }
    public override void Update(double dt)
    {
        base.Update(dt);
        int animIdx = Entity.Velocity.IsNonzero() ? 1 : 0;

        SetBodyHandedAnim(animIdx, 1, Controls.LastFacingIdx);
        Entity.Velocity = Controls.Move * Entity.BaseSpeed * Entity.SlowedSpeedMul;
        if (!Controls.IsCharging)
        {
            SlingSprite.Visible = false;
            SlingSprite.Stop();
            ReticleSprite.Play("still");
            StateMachine.SetState("default");
            return;
        }
        if (Entity.SlingChargeClock.IsRunning) return;
        if(Phase == NumPhases) StateMachine.SetState("charged");
        else
        {
            Phase++;
            ReticleSprite.Play(ReticleAnimations[Phase + 1]);
            Entity.SlingChargeClock.Restart();
        }
    }
}
public class SwPlayerCharged: SwPlayerState
{
    public override string Name => "charged";
    public override void BeginState(string lastState)
    {
        base.BeginState(lastState);
        SlingSprite.Play("charged");
    }
    private bool CanFire()
    {
        if(!Controls.Aim.IsNonzero()) return false;
        if(!Controls.FireJustPressed) return false;
        return true;
    }
    private void Fire()
    {
        Entity.Ammo--;
        var sling = Entity.Props.Get("sling");
        if(!Entity.Props.TryGet("sling/projectile", out string slingProto)) return;
        var props = SwData.Prototypes.Get($"projectiles/{slingProto}");
        SwPrion.TrySetVec2(props, "velocity", Controls.Aim.Normalized() * Entity.SlingBulletSpeed);
        SwPrion.TrySetVec2(props, Entity.Position);
        props.TrySet("damage", sling.Get("sling_damage"));
        Entity.Game.AddEntity<SwProjectile>(props);
    }
    public override void Update(double dt)
    {
        base.Update(dt);
        int animIdx = Entity.Velocity.IsNonzero() ? 1 : 0;
        SetBodyHandedAnim(animIdx, 1, Controls.LastFacingIdx);
        Entity.Velocity = Controls.Move * Entity.BaseSpeed * Entity.SlowedSpeedMul;
        if (!Controls.IsCharging) StateMachine.SetState("default");
        else if (CanFire())
        {
            Fire();
            StateMachine.SetState("default");
        }
    }
    public override void EndState(string nextState)
    {
        base.EndState(nextState);
        SlingSprite.Visible = false;
        SlingSprite.Stop();
        ReticleSprite.Play(ReticleAnimations[0]);
    }
}
