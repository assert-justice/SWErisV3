using Eris;
using Eris.Renderer;
using ErisMath;

namespace SpoonWitch.Ik;

public class SwIkLimb
{
    private readonly List<ErVec2> Joints = [];
    private readonly List<ErVec2> JointsReversed = [];
    private readonly List<ErVec2> Offsets = [];
    public ErVec2 Origin;
    public ErVec2 Target;
    public double Speed = 100;
    public int NumSteps = 1;
    public ErVec2 Tip => Joints.Count == 0 ? ErVec2.Zero : Joints[^1];
    public bool IsAtTarget => !(Tip - Target).IsNonzero();
    public SwIkLimb(){}
    public SwIkLimb(IList<double> distances, ErVec2? origin = null, ErVec2? target = null)
    {
        Origin = origin ?? ErVec2.Zero;
        Target = target ?? new ErVec2(distances.Sum(), 0);
        ErVec2 dir = (Target - Origin).Normalized();
        Joints = new(distances.Count + 1);
        ErVec2 joint = ErVec2.Zero;
        Joints.Add(joint);
        foreach (var d in distances)
        {
            joint += dir * d;
            Joints.Add(joint);
        }
    }
    public void Update(double deltaTime)
    {
        if(Joints.Count < 2) return;
        double dt = deltaTime / NumSteps;
        for (int idx = 0; idx < NumSteps; idx++)
        {
            Step(dt);
        }
    }
    public virtual void Draw(double frameTime){}
    public void DebugDraw()
    {
        if(Joints.Count < 2) return;
        // foreach (var item in Joints)
        // {
        //     ErEngine.Renderer.DrawCircle(item + Origin, 4, 16, ErColor.Blue);
            // ErEngine.Renderer.DrawArc(item + Origin, 4, 0, ErMath.HALF_PI, 2, ErColor.Blue);
            // ErEngine.Renderer.DrawRect(ErRect2.Centered(item+Origin, new(4,4)), ErColor.Blue);
        // }
        for (int idx = 0; idx < Joints.Count - 1; idx++)
        {
            ErVec2 a = Joints[idx] + Origin;
            ErVec2 b = Joints[idx+1] + Origin;
            var diff = b - a;
            ErVec2 tangent = new(-diff.Y,diff.X);
            ErVec2 c = b + tangent;
            ErVec2 d = c - diff;
            ErVec2[] points = [a,b,c,d];
            ErEngine.Renderer.DrawQuads(points, ErColor.Blue);
            // ErEngine.Renderer.DrawLine(a, b, ErColor.Blue);
            // ErEngine.Renderer.DrawLine(b, c, ErColor.Blue);
            // ErEngine.Renderer.DrawLine(c, d, ErColor.Blue);
            // ErEngine.Renderer.DrawLine(d, a, ErColor.Blue);
            // ErEngine.Renderer.DrawLine(Joints[idx] + Origin, Joints[idx+1] + Origin, ErColor.Blue);
            // ErEngine.Renderer.DrawLine(Joints[idx] + Origin, Joints[idx] + tangent + Origin, ErColor.Blue);
        }
    }
    private void Step(double deltaTime)
    {
        Offsets.Clear();
        var root = Joints[0];
        var tip = Joints[^1];
        var diff = Target - tip;
        double speed = Speed * deltaTime;
        if(diff.GetLength() < speed)
        {
            tip = Target;
        }
        else
        {
            tip += diff.Normalized() * speed;
        }
        Pass(Joints, in JointsReversed, tip);
        Pass(JointsReversed, in Joints, root);
    }
    private void Pass(List<ErVec2> joints, in List<ErVec2> reversedJoints, ErVec2 target)
    {
        reversedJoints.Clear();
        Offsets.Clear();
        // calculate the offsets
        ErVec2 lastJoint = joints[0];
        for (int idx = 1; idx < joints.Count; idx++)
        {
            var offset = joints[idx] - lastJoint;
            Offsets.Add(offset);
            lastJoint = joints[idx];
        }
        // loop backwards over the joints
        for (int idx = joints.Count - 2; idx > -1; idx--)
        {
            reversedJoints.Add(target);
            ErVec2 basePos = joints[idx];
            ErVec2 dir = (basePos - target).Normalized();
            double len = Offsets[idx].GetLength();
            target += dir * len;
        }
        reversedJoints.Add(target);
    }
}
