using Eris;
using ErisMath;
using Prion.Node;
using SpoonWitch.Game.Entity.Actor.Enemy.Aspect.State;
using SpoonWitch.Game.Entity.Component;
using SpoonWitch.Game.Entity.Component.Ik;
using SpoonWitch.Game.Entity.Component.State;
using SpoonWitch.Ik;

namespace SpoonWitch.Game.Entity.Actor.Enemy.Aspect;

public class SwAspect: SwEnemy
{
    private SwStateMachine<SwAspect> StateMachine = null!;
    public SwAspect()
    {
        AddGlobalHandler("boss_wake", Wake);
    }
    public override void SetProps(PriNode props)
    {
        base.SetProps(props);
        LoadSprites("anim_data/sprites");
    }
    public override void Init()
    {
        base.Init();
        StateMachine = RegisterComponent(SwAspectState.GetStateMachine(this, "state_machine"));
        StateMachine.SetDefaultState("stationary");
        // double dist = 4;
        // double r = 16;
        // double dr = 2;
        // List<(ErVec2 pos, double radius)> joints = [];
        // ErVec2 pos = ErVec2.Zero;
        // for (int idx = 0; idx < 16; idx++)
        // {
        //     // joints.Add((pos, 4));
        //     joints.Add((pos, idx % 2 == 0 ? 4: 8));
        //     pos += ErVec2.Right * 8;
        // }
        // while(r > 2)
        // {
        //     joints.Add((pos, r));
        //     pos += ErVec2.Right * r;
        //     r -= dr;
        // }
        RegisterComponent(new SwTentacleComponent(this, "right_arm"));
        // double[] distances = [32];
        // double[] distances = [32, 32, 32, 32];
        // double[] distances = new double[32];
        // for (int idx = 0; idx < distances.Length; idx++)
        // {
        //     distances[idx] = 4;
        // }
        // SwIkLimb limb = new(distances);
        // // limb.Target = ErVec2.Down * 32;
        // // for (int idx = 0; idx < 1; idx++)
        // // {
        // //     limb.Update(1.0/60);
        // // }
        // RegisterComponent(new SwIkLimbComponent(this, "right_arm", limb));
    }
    private void Wake(PriNode command)
    {
        StateMachine.SetState("wake");
    }
}
