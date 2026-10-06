using Eris;
using Eris.Renderer;
using ErisMath;

namespace SpoonWitch.Game.Entity.Component.Ik.Tentacle;

public class SwTentacleSpine
{
    public readonly SwTentacleSegment Segment;
    public ErVec2 GlobalPos;
    private ErVec2 _Direction;
    public ErVec2 Direction
    {
        get => _Direction;
        set
        {
            if (value.IsNormalized()) _Direction = value;
            else _Direction = value.Normalized();
        }
    }
    public double Length;
    public double Angle => _Direction.GetAngle();
    public ErVec2 Vector
    {
        get => GlobalPos + _Direction * Length;
        set
        {
            (_Direction,Length) = value.GetDirLen();
        }
    }
    public static readonly ErColor Color = new(151,58,77);
    public static readonly ErVec2 TipSize = new(2,2);
    private SwTentacleSpine(SwTentacleSegment segment)
    {
        Segment = segment;
    }
    public void Draw()
    {
        double angle = Segment.Direction.GetAngle();
        ErVec2 pos = GlobalPos.Rotate(angle);
        ErVec2 bud = (Vector - GlobalPos).Rotate(angle) + pos;
        ErEngine.Renderer.DrawLine(pos+Segment.GlobalPos, bud+Segment.GlobalPos, Color);
        ErEngine.Renderer.DrawRect(ErRect2.Centered(bud+Segment.GlobalPos,TipSize),Color);
    }
    public static SwTentacleSpine SegmentInitSpine(SwTentacleSegment segment)
    {
        return new(segment);
    }
}
