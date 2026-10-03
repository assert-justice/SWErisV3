using SpoonWitch.Utils;

namespace SpoonWitch.Game.Entity.Actor.Player.PlayerState;

public class SwPlayerDodging : SwPlayerState
{
    public override string Name => "dodging";
    protected override double StaminaRegenClockMul => 0;
    private const int StartupPhase = 0;
    private const int InvulnPhase = 1;
    private const int CancelPhase = 2;
    private SwClock PhaseClock = null!;
    private int Phase = 0;
    public override void Ready()
    {
        base.Ready();
        PhaseClock = Entity.AddClock();
    }
    public override void BeginState(string lastState)
    {
        base.BeginState(lastState);
        BodySprite.Stop();
        SetBodyDodgeAnim(Controls.LastFacingIdx);
        // set and lock in velocity
        Entity.Velocity = Controls.Move.Normalized() * Entity.BaseSpeed * Entity.DodgeSpeedMul;
        DustParticles.Particles.Emitting = true;
        Entity.UseStamina(Entity.DodgeStaminaCost);
        Phase = 0;
    }
    public override void Update(double dt)
    {
        base.Update(dt);
        // check stuff on last phase
        if(Phase == CancelPhase){}
        if(PhaseClock.IsRunning) return;
        switch (Phase)
        {
            case StartupPhase:
                PhaseClock.Start(Entity.DodgeInvulnDelay);
            break;
            case InvulnPhase:
                PhaseClock.Start(Entity.DodgeInvulnDuration);
                Entity.SetInvulnerable(Entity.DodgeInvulnDuration);
            break;
            case CancelPhase:
                PhaseClock.Start(Entity.DodgeCancelWindow);
            break;
            default: // End dodge
                StateMachine.SetState("default");
                break;
        }
        Phase++;
    }
    public override void EndState(string nextState)
    {
        base.EndState(nextState);
        Entity.DodgeCooldownClock.SetDuration(Entity.DodgeCooldown);
    }
}
