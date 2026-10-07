using Eris.Renderer;
using ErisMath;
using SpoonWitch.Game.Entity.Component.Ik.Tentacle;
using SpoonWitch.Ik;

namespace SpoonWitch.Game.Entity.Component.Ik.AspectLegs;

public class SwAspectLegsComponent : SwComponent
{
    private readonly List<SwAspectLeg> Legs = [];
    private readonly List<ErVec2> Joints = [];
    public ErVec2 Offset;
    public bool IsVisible;
    public bool IsActive;
    public ErVec2 Velocity;
    public ErVec2 GlobalPos => Parent.Position + Offset;
    public double LegSpacing = 5;
    public SwAspectLegsComponent(SwEntity parent, string name) : base(parent, name)
    {
    }
    public override void Update(double dt)
    {
        base.Update(dt);
        foreach (var leg in Legs)
        {
            leg.Update(dt);
        }
    }
    public override void Draw()
    {
        base.Draw();
        if(!IsVisible) return;
        foreach (var leg in Legs)
        {
            leg.Draw();
        }
    }
    public void Activate()
    {
        IsVisible = true;
        IsActive = true;
        int numLegs = 6;
        for (int legIdx = 0; legIdx < numLegs; legIdx++)
        {
            double xPos = LegSpacing * legIdx - LegSpacing * numLegs * 0.5;
            SwAspectLeg leg = new()
            {
                Parent = this,
                Offset = ErVec2.Right * xPos
            };
            Legs.Add(leg);
            leg.Activate();
        }
    }
    public void Step(List<SwTentacleSegment> segments, ErVec2 target, ErVec2 root)
    {
        Joints.Clear();
        for (int idx = 0; idx < segments.Count; idx++)
        {
            Joints.Add(segments[idx].GlobalPos);
        }
        SwFabrik.Step(Joints, target, root);
        for (int idx = 0; idx < segments.Count; idx++)
        {
            segments[idx].GlobalPos = Joints[idx];
        }
        for (int idx = 0; idx < segments.Count; idx++)
        {
            segments[idx].Update();
        }
    }
    public void Randomize()
    {
        foreach (var leg in Legs)
        {
            leg.Randomize();
        }
    }
}
