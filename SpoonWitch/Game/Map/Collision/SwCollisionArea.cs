using Eris;
using ErisPhysics2D.Collider;

namespace SpoonWitch.Game.Map.Collision;

public class SwColliderArea: ErColliderArea
{
    public Action<int,ErColliderBody>? OnBodyEnterFn;
    public Action<int,ErColliderBody>? OnBodyExitFn;
    // public int SingleUseId = -1;
    public override void OnBodyEnter(int bodyId, ErColliderBody body)
    {
        base.OnBodyEnter(bodyId, body);
        if(OnBodyEnterFn is not null) OnBodyEnterFn(bodyId, body);
        // ErEngine.Log("body entered ", bodyId);
    }
    public override void OnBodyExit(int bodyId, ErColliderBody body)
    {
        base.OnBodyExit(bodyId, body);
        if(OnBodyEnterFn is not null) OnBodyEnterFn(bodyId, body);
    }
    // public override void Update(IEnumerable<(int bodyId, ErColliderBody body)> bodies, Dictionary<int, ErColliderBody> bodyLookup)
    // {
    //     base.Update(bodies, bodyLookup);
    //     if(SingleUseId >= 0) SwGame.GetMap().PhysicsWorld.RemoveArea(SingleUseId);
    // }
}