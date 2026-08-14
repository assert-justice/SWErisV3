using Eris;
using ErisPhysics2D.Collider;

namespace SpoonWitch.Game.Map.Collision;

public class SwColliderArea: ErColliderArea
{
    public Action<SwColliderArea,int,ErColliderBody>? OnBodyEnterFn;
    public Action<SwColliderArea,int,ErColliderBody>? OnBodyExitFn;
    // public int SingleUseId = -1;
    public override void OnBodyEnter(int bodyId, ErColliderBody body)
    {
        base.OnBodyEnter(bodyId, body);
        if(OnBodyEnterFn is not null) OnBodyEnterFn(this, bodyId, body);
        // ErEngine.Log("body entered ", bodyId);
    }
    public override void OnBodyExit(int bodyId, ErColliderBody body)
    {
        base.OnBodyExit(bodyId, body);
        if(OnBodyEnterFn is not null) OnBodyEnterFn(this, bodyId, body);
    }
    public override void Copy<T>(ref T value)
    {
        if(value is not SwColliderArea area) throw new($"bad collider type {value.GetType()}");
        base.Copy(ref value);
        area.OnBodyEnterFn = OnBodyEnterFn;
        area.OnBodyExitFn = OnBodyExitFn;
    }
    // public override void Update(IEnumerable<(int bodyId, ErColliderBody body)> bodies, Dictionary<int, ErColliderBody> bodyLookup)
    // {
    //     base.Update(bodies, bodyLookup);
    //     if(SingleUseId >= 0) SwGame.GetMap().PhysicsWorld.RemoveArea(SingleUseId);
    // }
}