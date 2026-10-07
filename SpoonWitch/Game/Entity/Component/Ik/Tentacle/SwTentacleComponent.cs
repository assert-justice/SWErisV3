using Eris;
using ErisMath;
using SpoonWitch.Ik;

namespace SpoonWitch.Game.Entity.Component.Ik.Tentacle;

public class SwTentacleComponent: SwComponent
{
    private readonly List<SwTentacleSegment> Segments = [];
    private readonly List<SwTentacleSegment> OutlineSegments = [];
    private readonly List<SwTentacleSpine> Spines = [];
    private readonly List<ErVec2> Joints = [];
    public ErVec2 Target{get; private set;}
    public ErVec2 TipPos => Segments[^1].GlobalPos;
    public bool IsAtTarget => (TipPos-Target).IsApproxZero();
    public ErVec2 TipDir=> Segments[^1].Direction;
    public double TipSpeed;
    public double TipTurnRadius = 10;
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
                // Parent = this,
                GlobalPos = pos,
                Length = length,
                Color = new(152,184,75),
                Radius = 2,
            };
            SwTentacleSegment outlineSegment = new()
            {
                // Parent = this,
                GlobalPos = pos,
                Length = length,
                Color = new(59,99,28),
                Radius = 3,
            };
            if(idx > 0)
            {
                segment.LastSegment = Segments[idx-1];
                Segments[idx-1].NextSegment = segment;
                outlineSegment.LastSegment = OutlineSegments[idx-1];
                OutlineSegments[idx-1].NextSegment = outlineSegment;
            }
            Segments.Add(segment);
            OutlineSegments.Add(outlineSegment);
            pos += diff;
        }
        Target = TipPos;
        var spineSeg = Segments[^1];
        var spine = AddSpine(spineSeg);
        spine.Vector = ErVec2.One * 4;
        spine = AddSpine(spineSeg);
        spine.Vector = new ErVec2(4,-4);
        ErVec2 p = ErVec2.Zero;
        for (int idx = 0; idx < 2; idx++)
        {
            spine = AddSpine(spineSeg);
            spine.GlobalPos = p + ErVec2.Up;
            spine.Vector = ErVec2.Up * 4;
            spine = AddSpine(spineSeg);
            spine.GlobalPos = p + ErVec2.Down;
            spine.Vector = ErVec2.Down * 4;
            p+=ErVec2.Left*3;
        }
        p = ErVec2.Zero;
        for (int i = 0; i < 5; i++)
        {
            spineSeg = Segments[^(1+i)];
            spine = AddSpine(spineSeg);
            spine.GlobalPos = p + ErVec2.Up;
            spine.Vector = ErVec2.Up * 4;
            spine = AddSpine(spineSeg);
            spine.GlobalPos = p + ErVec2.Down;
            spine.Vector = ErVec2.Down * 4;
        }
    }
    private (ErVec2 direction, double length) GetTipDirLen(ErVec2 diff)
    {
        // Todo: take another pass at constraints
        var (dir,len) = diff.GetDirLen();
        // double maxAngleDelta = len * ErMath.PI / TipTurnRadius;
        // // double dot = TipDir.Dot(dir);
        // // double angleDelta = Math.Acos(dot);
        // double angleDelta = TipDir.GetAngleTo(dir);
        // if(Math.Abs(angleDelta) > maxAngleDelta)
        // {
        //     angleDelta = maxAngleDelta * Math.Sign(angleDelta);
        //     dir = TipDir.Rotate(angleDelta);
        // }
        return (dir,len);
    }
    public override void Update(double dt)
    {
        base.Update(dt);
        if(!IsActive) return;
        ErVec2 diff = Target - TipPos;
        var(dir,len) = GetTipDirLen(diff);
        double speed = TipSpeed * dt;
        if(len < speed)
        {
            // ignores turn radius, might revisit
            Step(Target, GlobalPos);
            return;
        }
        len = speed;
        ErVec2 target = TipPos + dir * len;
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
            OutlineSegments[idx].GlobalPos = Joints[idx];
        }
        for (int idx = 0; idx < Segments.Count; idx++)
        {
            Segments[idx].Update();
            OutlineSegments[idx].Update();
        }
    }
    public override void Draw()
    {
        base.Draw();
        if(!IsVisible) return;
        for (int idx = 0; idx < Segments.Count; idx++)
        {
            OutlineSegments[idx].Draw();
            Segments[idx].Draw();
        }
        foreach (var spine in Spines)
        {
            spine.Draw();
        }
    }
}
