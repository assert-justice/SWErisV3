using Eris;
using ErisPhysics2D.Collider;

namespace SpoonWitch.Game.Map.Collision;

public class SwColliderArea: ErColliderArea
{
    public readonly int ParentId;
    public Action<SwColliderArea,ErColliderBody>? OnBodyEnterFn;
    public Action<SwColliderArea,ErColliderBody>? OnBodyExitFn;

    public SwColliderArea(int id, int parentId) : base(id)
    {
        ParentId = parentId;
    }

    public override void OnBodyEnter(ErColliderBody body)
    {
        base.OnBodyEnter(body);
        if(OnBodyEnterFn is not null) OnBodyEnterFn(this, body);
    }
    public override void OnBodyExit(ErColliderBody body)
    {
        base.OnBodyExit(body);
        if(OnBodyExitFn is not null) OnBodyExitFn(this, body);
    }
}
