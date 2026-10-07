using Eris.Renderer;
using ErisMath;
using SpoonWitch.Game.Entity.Component.Ik.Tentacle;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Entity.Component.Ik.AspectLegs;

public class SwAspectLeg
{
    private readonly List<SwTentacleSegment> Segments = [];
    private readonly List<SwTentacleSegment> OutlineSegments = [];
    public required SwAspectLegsComponent Parent;
    public ErVec2 Offset;
    public ErVec2 GlobalPos => Parent.GlobalPos + Offset;
    public ErVec2 TipDir=> Segments[^1].Direction;
    public ErVec2 TipPos => Segments[^1].GlobalPos;
    public ErVec2 Target;
    public double TipSpeed = 300;
    public double Length{get; private set;}
    private double Bias = 0;
    public void Update(double dt)
    {
        // double bias = Random.Shared.NextDouble() * 1;
        var diff = Target - TipPos - Parent.Velocity.Normalized() * Length * Bias;
        var len = diff.GetLength();
        double radius = Length * 0.3;
        if(len > Length)
        {
            var circlePoint = SwRandom.GetRandomPointOnCircle(ErVec2.Zero,radius);
            var stride = Parent.Velocity.Normalized() * Length;
            Target = GlobalPos + circlePoint + stride;
            Bias = Random.Shared.NextDouble();
        }
        Step(Target,GlobalPos);
    }
    public void Draw()
    {
        for (int idx = 0; idx < Segments.Count; idx++)
        {
            OutlineSegments[idx].Draw();
            Segments[idx].Draw();
        }
    }
    public void Activate()
    {
        int numSegments = 5;
        double segLen = 6;
        ErColor color = new(255, 249, 117);
        ErColor outlineColor = new(128, 120, 60);
        ErVec2 bottom = new ErVec2(0, numSegments * segLen) + GlobalPos;
        for (int segIdx = 0; segIdx < numSegments; segIdx++)
        {
            double yPos = segLen * segIdx;
            ErVec2 pos = new(GlobalPos.X, GlobalPos.Y + yPos);
            SwTentacleSegment segment = new()
            {
                GlobalPos = pos,
                Length = segLen,
                Color = color,
                Radius = 1,
            };
            SwTentacleSegment outlineSegment = new()
            {
                GlobalPos = pos,
                Length = segLen,
                Color = outlineColor,
                Radius = 2,
            };
            if(segIdx > 0)
            {
                Segments[^1].NextSegment = segment;
                segment.LastSegment = Segments[^1];
                OutlineSegments[^1].NextSegment = outlineSegment;
                outlineSegment.LastSegment = Segments[^1];
            }
            Segments.Add(segment);
            OutlineSegments.Add(outlineSegment);
            Length+=segment.Length;
            Step(bottom, GlobalPos);
        }
        // Scrunch();
    }
    public void Randomize()
    {
        Target = SwRandom.GetRandomPointOnCircle(GlobalPos,Length);
        SnapToTarget();
    }
    public void SnapToTarget(ErVec2 target)
    {
        Target = target;
        SnapToTarget();
    }
    public void SnapToTarget()
    {
        Step(Target, GlobalPos);
    }
    // public void Scrunch()
    // {
    //     for (int idx = 0; idx < Segments.Count; idx++)
    //     {
    //         var segment = Segments[idx];
    //         if(idx%2 == 0) segment.GlobalPos = GlobalPos;
    //         else segment.GlobalPos = GlobalPos + ErVec2.Down * segment.Length;
    //     }
    //     Target = TipPos;
    // }
    public void Step(ErVec2 target, ErVec2 root)
    {
        Parent.Step(Segments,target,root);
        for (int idx = 0; idx < Segments.Count; idx++)
        {
            OutlineSegments[idx].GlobalPos = Segments[idx].GlobalPos;
            OutlineSegments[idx].Direction = Segments[idx].Direction;
        }
    }
}
