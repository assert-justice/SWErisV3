using Eris;
using ErisMath;
using ErisPhysics2D.Collider;
using SpoonWitch.Game.Map.Collision;

namespace SpoonWitch.Game.Entity.Component;

public class SwAreaComponent: SwComponent
{
    private readonly SwColliderArea Area;
    private bool WasEnabled = false;
    public bool Enabled;
    public uint Mask;
    public ErVec2 Offset;
    public ErVec2 Size;

    public SwAreaComponent(SwEntity parent, string name, uint mask, ErVec2 size, ErVec2? offset = null, bool enabled = false,
        Action<SwEntity>? onBodyEnter = null, Action<SwEntity>? onBodyExit = null) : base(parent, name)
    {
        Enabled = enabled;
        Mask = mask;
        Offset = offset ?? ErVec2.Zero;
        Size = size;
        OnBodyEnter = onBodyEnter;
        OnBodyExit = onBodyExit;
        Area = new(SwApp.GetNextId(), parent.Id);
    }

    public Action<SwEntity>? OnBodyEnter{private get; set;}
    public Action<SwEntity>? OnBodyExit{private get; set;}
    public override void Ready()
    {
        base.Ready();
        Area.OnBodyEnterFn = OnEnter;
        Area.OnBodyExitFn = OnExit;
    }
    public override void Update(double dt)
    {
        base.Update(dt);
        if(Enabled != WasEnabled)
        {
            WasEnabled = Enabled;
            if (!Enabled) Parent.Game.PhysicsWorld.RemoveArea(Area.Id);
        }
        if(!Enabled) return;
        Area.Position = Parent.Position + Offset - Size*0.5;
        Area.Size = Size;
        Area.Mask = Mask;
        Parent.Game.PhysicsWorld.AddArea(Area);
    }
    private void OnEnter(SwColliderArea area, ErColliderBody body)
    {
        if(OnBodyEnter is null) return;
        if(body is not SwColliderBody b) return;
        if(!Parent.Game.EntityLookup.TryGet<SwEntity>(b.ParentId.ToString(), out var entity)) return;
        OnBodyEnter(entity);
    }
    private void OnExit(SwColliderArea area, ErColliderBody body)
    {
        if(OnBodyExit is null) return;
        if(body is not SwColliderBody b) return;
        if(!Parent.Game.EntityLookup.TryGet<SwEntity>(b.ParentId.ToString(), out var entity)) return;
        OnBodyExit(entity);
    }
    public override void Cleanup()
    {
        base.Cleanup();
        Parent.Game.PhysicsWorld.RemoveArea(Area.Id);
    }
}
