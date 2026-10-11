using Eris;
using ErisMath;
using Prion.Node;
using SpoonWitch.Game.Entity.Component;

namespace SpoonWitch.Game.Entity.MapEntity;

public class SwBollard: SwMapEntity
{
    private SwColliderBody Body = null!;
    private SwSpriteComponent Sprite = null!;
    public virtual SwCollisionMask Mask => IsEnabled ? SwCollisionMask.BlocksNav : SwCollisionMask.None;
    public bool IsEnabled = true;
    public SwBollard()
    {
        AddGlobalHandler("bollard_update", HandleBollardUpdate);
        // AddGlobalHandler("bollard_disable", OnDisable);
    }
    public override void Init()
    {
        base.Init();
        LoadSprites("sprites");
        Sprite = GetComponent<SwSpriteComponent>("sprite")!;
        IsEnabled = Game.Flags.TryGetValue(Iid, out int i) && i > 0;
        Sprite.Sprite.Visible = false;
        Sprite.Sprite.Centered = false;
        Body = new(SwApp.GetNextId(), Id)
        {
            Size = Size,
            Position = Rect.Position,
        };
        if(!Props.TryGet("fields/start_up", out bool startUp)) startUp = true;
        if(!Game.Flags.ContainsKey(Iid)) Game.Flags[Iid] = startUp ? 1 : 0;
        UpdateBollard();
    }
    private void HandleBollardUpdate(PriNode _)
    {
        bool isEnabled = Game.Flags.TryGetValue(Iid, out int i) && i > 0;
        if(IsEnabled == isEnabled) return;
        UpdateBollard();
        // IsEnabled = isEnabled;
        // Body.Mask = (uint)Mask;
        // Game.PhysicsWorld.MoveAndSlide(0.16, Body);
        // Sprite.Sprite.Play(isEnabled ? "extend" : "retract");
    }
    private void UpdateBollard()
    {
        bool isEnabled = Game.Flags.TryGetValue(Iid, out int i) && i > 0;
        IsEnabled = isEnabled;
        Body.Mask = (uint)Mask;
        Game.PhysicsWorld.MoveAndSlide(0.16, Body);
        Sprite.Sprite.Play(isEnabled ? "extend" : "retract");
    }
    // protected override void Update(double dt)
    // {
    //     base.Update(dt);
    //     Body.Mask = (uint)Mask;
    //     Game.PhysicsWorld.MoveAndSlide(dt, Body);
    // }
    protected override void Draw()
    {
        base.Draw();
        Sprite.Sprite.Visible = true;
        foreach (var item in RectTiles.GetInnerCoords())
        {
            var p = (ErVec2)(item * Game.Map.TileSize);
            Sprite.Sprite.Draw(p);
        }
        Sprite.Sprite.Visible = false;
    }
    // private void OnEnable(PriNode command)
    // {
    //     if(!command.TryGet("iid", out string iid) || iid != Iid) return;
    //     IsEnabled = true;
    //     Game.Flags[Iid] = 1;
    // }
    // private void OnDisable(PriNode command)
    // {
    //     IsEnabled = false;
    //     Game.Flags[Iid] = 0;
    // }
}
