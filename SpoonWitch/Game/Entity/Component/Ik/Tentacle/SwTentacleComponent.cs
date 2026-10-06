using Eris;
using Eris.Renderer;
using ErisMath;
using SpoonWitch.Ik;

namespace SpoonWitch.Game.Entity.Component.Ik.Tentacle;

public class SwTentacleComponent: SwComponent
{
    // private class SwSegment
    // {
    //     public required SwTentacleComponent Parent;
    //     public SwSegment? LastSegment;
    //     public SwSegment? NextSegment;
    //     public ErVec2 Position;
    //     public ErVec2 Direction = ErVec2.Right;
    //     public ErVec2 GlobalPos => Position + Parent.GlobalPos;
    //     public ErVec2 Up => new ErVec2(Direction.Y,-Direction.X) * Radius;
    //     public ErVec2 Down => new ErVec2(-Direction.Y,Direction.X) * Radius;
    //     public ErVec2 Above => GlobalPos + Up;
    //     public ErVec2 Below => Down + GlobalPos;
    //     public double Radius = 3;
    //     public List<SwSpine> Spines = [];
    //     public static readonly ErColor Color = ErColor.Blue;// new(152,184,75);
    //     public SwSpine AddSpine()
    //     {
    //         SwSpine spine = new(this);
    //         Spines.Add(spine);
    //         return spine;
    //     }
    //     public void Update()
    //     {
    //         if(NextSegment is not null && LastSegment is not null)Direction = (NextSegment.Position - LastSegment.Position).Normalized();
    //         else if(NextSegment is not null) Direction = (NextSegment.Position - Position).Normalized();
    //         else if(LastSegment is not null) Direction = (Position - LastSegment.Position).Normalized();
    //     }
    //     public void Draw()
    //     {
    //         if(LastSegment is not null) DrawLink(LastSegment);
    //         if(NextSegment is not null) DrawLink(NextSegment);
    //         DrawTip();
    //     }
    //     public void DrawTip()
    //     {
    //         ErEngine.Renderer.DrawCircle(GlobalPos,Radius,12,Color);
    //     }
    //     private void DrawLink(SwSegment segment)
    //     {
    //         DrawQuad(GlobalPos,Above,segment.Above,segment.GlobalPos);
    //         DrawQuad(GlobalPos,Below,segment.Below,segment.GlobalPos);
    //     }
    //     private void DrawQuad(ErVec2 a, ErVec2 b, ErVec2 c, ErVec2 d)
    //     {
    //         ErEngine.Renderer.DrawQuad(a,b,c,d,Color);
    //     }
    // }
    // private class SwSpine
    // {
    //     public readonly SwSegment Segment;
    //     public ErVec2 Position;
    //     public ErVec2 BudPosition;
    //     public static readonly ErColor BudColor = new(151,58,77);
    //     public static readonly ErVec2 BudSize = new(2,2);
    //     public SwSpine(SwSegment segment)
    //     {
    //         Segment = segment;
    //     }
    //     public void Draw()
    //     {
    //         double angle = Segment.Direction.GetAngle(); // here we go again
    //         ErVec2 pos = Position.Rotate(angle);
    //         ErVec2 bud = (BudPosition - Position).Rotate(angle) + pos;
    //         ErEngine.Renderer.DrawLine(pos+Segment.GlobalPos, bud+Segment.GlobalPos, BudColor);
    //         ErEngine.Renderer.DrawRect(ErRect2.Centered(bud+Segment.GlobalPos,BudSize),BudColor);
    //     }
    // }
    private readonly List<SwTentacleSegment> Segments = [];
    private readonly List<SwTentacleSpine> Spines = [];
    private readonly List<ErVec2> Joints = [];
    public ErVec2 Target{get; private set;}
    public ErVec2 TipPos => Segments[^1].GlobalPos;
    public bool IsAtTarget => (TipPos-Target).IsApproxZero();
    public ErVec2 TipDir=> Segments[^1].Direction;
    public double TipSpeed;
    public double TipTurnRadius;
    public ErVec2 Offset{get; private set;}
    public bool IsVisible = false;
    public bool IsActive = false;
    public ErVec2 GlobalPos => Parent.Position + Offset;
    public SwTentacleComponent(SwEntity parent, string name) : base(parent, name){}
    public override void Ready()
    {
        base.Ready();
        ErVec2 pos = ErVec2.Zero;
        double length = 4;
        ErVec2 diff = ErVec2.Right * length;
        for (int idx = 0; idx < 32; idx++)
        {
            SwTentacleSegment segment = new()
            {
                Parent = this,
                GlobalPos = pos,
                Length = length,
            };
            if(idx > 0)
            {
                segment.LastSegment = Segments[idx-1];
                Segments[idx-1].NextSegment = segment;
            }
            Segments.Add(segment);
            pos += diff;
        }
        Target = TipPos;
        var lastSegment = Segments[^1];
        var spine = AddSpine(lastSegment);
        spine.Vector = ErVec2.One * 4;
        spine = AddSpine(lastSegment);
        spine.Vector = new ErVec2(4,-4);
        ErVec2 p = ErVec2.Zero;
        for (int idx = 0; idx < 2; idx++)
        {
            spine = AddSpine(lastSegment);
            spine.GlobalPos = p + ErVec2.Up;
            spine.Vector = ErVec2.Up * 4;
            spine = AddSpine(lastSegment);
            spine.GlobalPos = p + ErVec2.Down;
            spine.Vector = ErVec2.Down * 4;
            p+=ErVec2.Left*3;
        }
    }
    public override void Update(double dt)
    {
        base.Update(dt);
        if(!IsActive) return;
        ErVec2 diff = Target - TipPos;
        double len = diff.GetLength();
        double speed = TipSpeed * dt;
        ErVec2 target = len < speed ? Target : TipPos + diff.Normalized() * speed;
        Step(target,GlobalPos);
    }
    public void SetTarget(ErVec2 target)
    {
        Target = target;
    }
    public void SnapToTarget()
    {
        Step(Target, GlobalPos);
    }
    public void SnapToTarget(ErVec2 target)
    {
        SetTarget(target);
        SnapToTarget();
    }
    public void Scrunch()
    {
        for (int idx = 0; idx < Segments.Count; idx++)
        {
            var segment = Segments[idx];
            if(idx%2 == 0) segment.GlobalPos = GlobalPos;
            else segment.GlobalPos = GlobalPos + ErVec2.Right * segment.Length;
        }
        Target = TipPos;
    }
    public void Activate()
    {
        Scrunch();
        IsActive = true;
        IsVisible = true;
    }
    private SwTentacleSpine AddSpine(SwTentacleSegment segment)
    {
        SwTentacleSpine spine = segment.AddNewSpine();
        Spines.Add(spine);
        return spine;
    }
    private void Step(ErVec2 target, ErVec2 root)
    {
        Joints.Clear();
        for (int idx = 0; idx < Segments.Count; idx++)
        {
            Joints.Add(Segments[idx].GlobalPos);
        }
        SwFabrik.Step(Joints, target, root);
        for (int idx = 0; idx < Segments.Count; idx++)
        {
            Segments[idx].GlobalPos = Joints[idx];
        }
        for (int idx = 0; idx < Segments.Count; idx++)
        {
            Segments[idx].Update();
        }
    }
    public override void Draw()
    {
        base.Draw();
        if(!IsVisible) return;
        for (int idx = 0; idx < Segments.Count; idx++)
        {
            Segments[idx].Draw();
        }
        foreach (var spine in Spines)
        {
            spine.Draw();
        }
    }
}
