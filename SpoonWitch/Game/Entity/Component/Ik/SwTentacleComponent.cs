using Eris;
using Eris.Renderer;
using ErisMath;
using SpoonWitch.Ik;

namespace SpoonWitch.Game.Entity.Component.Ik;

public class SwTentacleComponent: SwComponent
{
    private class SwSegment
    {
        public required SwTentacleComponent Parent;
        public SwSegment? LastSegment;
        public SwSegment? NextSegment;
        public ErVec2 Position;
        public ErVec2 Direction = ErVec2.Right;
        public ErVec2 GlobalPos => Position + Parent.Parent.Position;
        public ErVec2 Up => new ErVec2(Direction.Y,-Direction.X) * Radius;
        public ErVec2 Down => new ErVec2(-Direction.Y,Direction.X) * Radius;
        public ErVec2 Above => GlobalPos + Up;
        public ErVec2 Below => Down + GlobalPos;
        public double Radius = 3;
        public List<(ErVec2 pos, ErVec2 bud)> Spines = [];
        public static readonly ErColor Color = ErColor.Blue;// new(152,184,75);
        public void Update()
        {
            if(NextSegment is not null && LastSegment is not null)Direction = (NextSegment.Position - LastSegment.Position).Normalized();
            else if(NextSegment is not null) Direction = (NextSegment.Position - Position).Normalized();
            else if(LastSegment is not null) Direction = (Position - LastSegment.Position).Normalized();
        }
        public void Draw()
        {
            if(LastSegment is not null) DrawLink(LastSegment);
            if(NextSegment is not null) DrawLink(NextSegment);
            DrawTip();
        }
        public void DrawTip()
        {
            ErEngine.Renderer.DrawCircle(GlobalPos,Radius,12,Color);
        }
        private void DrawLink(SwSegment segment)
        {
            DrawQuad(GlobalPos,Above,segment.Above,segment.GlobalPos);
            DrawQuad(GlobalPos,Below,segment.Below,segment.GlobalPos);
        }
        private void DrawQuad(ErVec2 a, ErVec2 b, ErVec2 c, ErVec2 d)
        {
            ErEngine.Renderer.DrawQuad(a,b,c,d,Color);
        }
    }
    // private class SwSpine
    // {
    //     public required SwSegment Segment;
    //     public ErVec2 Position;
    //     public ErVec2 BudPosition;
    //     public static readonly ErColor BudColor = new(151,58,77);
    //     public static readonly ErVec2 BudSize = new(2,2);
    //     public void Draw()
    //     {
    //         double angle = Segment.Direction.GetAngle(); // here we go again
    //         ErVec2 pos = Position.Rotate(angle);
    //         ErVec2 bud = BudPosition.Rotate(angle,pos);
    //         ErEngine.Renderer.DrawLine(pos+Segment.GlobalPos, bud+Segment.GlobalPos, BudColor);
    //         ErEngine.Renderer.DrawRect(ErRect2.Centered(bud+Segment.GlobalPos,BudSize),BudColor);
    //     }
    // }
    private readonly List<SwSegment> Segments = [];
    private readonly List<ErVec2> Joints = [];
    // private readonly List<SwSpine> Spines = [];
    private ErVec2? Target;
    public SwTentacleComponent(SwEntity parent, string name) : base(parent, name)
    {
        ErVec2 pos = ErVec2.Zero;
        ErVec2 diff = ErVec2.Right * 4;
        for (int idx = 0; idx < 32; idx++)
        {
            SwSegment segment = new()
            {
                Parent = this,
                Position = idx % 2 == 0 ? pos : diff,
            };
            if(idx > 0)
            {
                segment.LastSegment = Segments[idx-1];
                Segments[idx-1].NextSegment = segment;
            }
            Segments.Add(segment);
            // pos += diff;
        }
        // var lastSegment = Segments[^1];
        // lastSegment.Spines.Add((ErVec2.Zero,ErVec2.One*4));
        // lastSegment.Spines.Add((ErVec2.Zero,new ErVec2(4,-4)));
        // ErVec2 p = ErVec2.Zero;
        // for (int idx = 0; idx < 2; idx++)
        // {
        //     lastSegment.Spines.Add((p + ErVec2.Up,p + ErVec2.Up*2));
        //     lastSegment.Spines.Add((p - ErVec2.Up,p - ErVec2.Up*2));
        //     lastSegment.Spines.Add((p,p));
        //     p+=ErVec2.Left*2;
        // }
    }
    public override void Update(double dt)
    {
        base.Update(dt);
        Step();
    }
    public void SetTarget(ErVec2 target)
    {
        Target = target;
    }
    private void Step()
    {
        if(Target is null) return;
        Joints.Clear();
        for (int idx = 0; idx < Segments.Count; idx++)
        {
            Joints.Add(Segments[idx].Position);
        }
        SwFabrik.Step(Joints, Target.Value, ErVec2.Zero);
        for (int idx = 0; idx < Segments.Count; idx++)
        {
            Segments[idx].Position = Joints[idx];
        }
        for (int idx = 0; idx < Segments.Count; idx++)
        {
            Segments[idx].Update();
        }
    }
    public override void Draw()
    {
        base.Draw();
        for (int idx = 0; idx < Segments.Count; idx++)
        {
            Segments[idx].Draw();
        }
    }
}
