using Eris;
using ErisMath;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Entity.Actor.Enemy.Aspect.State.ArmState;

public class SwArmAwake: SwAspectArmState
{
    public override string Name => "awake";
    public override void BeginState(string lastState)
    {
        base.BeginState(lastState);
        StateM.VisionArea.Enabled = true;
        Arm.TipSpeed = Entity.BaseArmSpeed * 0.25;
        SetTarget();
    }
    private void SetTarget()
    {
        ErVec2 target = Entity.Position + SwRandom.GetRandomPointInRect(StateM.WanderRect);
        // ErVec2 target = Entity.Position + SwRandom.GetRandomPointOnCircle(new ErVec2(-64,-32),48);
        Arm.SetTarget(target);
        Clock.Start(Random.Shared.NextDouble() * 0.8 + 0.2);
    }
    public override void Update(double dt)
    {
        base.Update(dt);
        if(StateM.VisionArea.Area.OverlappingCount > 0) StateM.SetState("attacking");
        else if(!Clock.IsRunning) SetTarget();
    }
    // public override void Draw()
    // {
    //     base.Draw();
    //     ErEngine.Renderer.DrawRect(StateM.WanderRect.Translate(Entity.Position), Eris.Renderer.ErColor.Blue, false);
    //     ErEngine.Renderer.DrawRect(StateM.AttackRect.Translate(Entity.Position), Eris.Renderer.ErColor.Green, false);
    // }
}
