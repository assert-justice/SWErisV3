using ErisMath;

namespace SpoonWitch.Game.Entity.Actor.Player.PlayerState;

public class SwPlayerAttacking: SwPlayerState
{
    public override string Name => "attacking";
    private enum AttackPhase
    {
        Startup,
        Hurt,
        Cancel,
    }
    private AttackPhase Phase;
    public override void BeginState(string lastState)
    {
        base.BeginState(lastState);
        Start();
    }
    private void Start()
    {
        Phase = AttackPhase.Startup;
        SpoonSprite.Visible = true;
        SpoonSprite.Angle = (Controls.LastFacingIdx - 1) * ErMath.HALF_PI;
        // Note: we stop the current spoon animation when we restart
        SpoonSprite.Stop();
        SpoonSprite.Play();
        BodySprite.Stop();
        HatSprite.Stop();
        SetBodyHandedAnim(0, 0, Controls.LastFacingIdx);
        Entity.Velocity = ErVec2.Zero;
        // Note: we are positioning the hurtbox but not enabling it yet
        SetHurtbox();
        Entity.UseStamina(Entity.SpoonStaminaCost);
        SpoonSprite.HFlip = !SpoonSprite.HFlip;
        Entity.SpoonClock.Start(Entity.SpoonHurtDelay);
        
    }
    public override void Update(double dt)
    {
        base.Update(dt);
        if(Phase == AttackPhase.Cancel)
        {
            if(CanAttack() && Controls.AttackJustDown)
            {
                Start();
            }
            else if(CanDodge() && Controls.DodgeJustDown)
            {
                StateMachine.SetState("dodging");
                return;
            }
        }
        if(Entity.SpoonClock.IsRunning) return;
        switch (Phase)
        {
            case AttackPhase.Startup:
                SpoonHurtbox.Enabled = true;
                Entity.SpoonClock.Start(Entity.SpoonHurtDuration);
                Phase = AttackPhase.Hurt;
                break;
            case AttackPhase.Hurt:
                SpoonHurtbox.Enabled = false;
                Entity.SpoonClock.Start(Entity.SpoonCancelWindow);
                Phase = AttackPhase.Cancel;
                break;
            case AttackPhase.Cancel:
                StateMachine.SetState("default");
                break;
        }
        // if(!SpoonSprite.IsPlaying) StateMachine.SetState("default");
        // SpoonHurtbox.Enabled = SpoonSprite.FrameIdx == 0;
    }
    public override void EndState(string nextState)
    {
        base.EndState(nextState);
        SpoonSprite.Visible = false;
        SpoonHurtbox.Enabled = false;
        Entity.ResumeStamina();
    }
    private void SetHurtbox()
    {
        var dir = ErVec2.FromAngle(Controls.LastFacingIdx * ErMath.HALF_PI);
        double dis = 32;
        SpoonHurtbox.Offset = dir * dis;
        // SpoonHurtbox.Enabled = true;
    }
}