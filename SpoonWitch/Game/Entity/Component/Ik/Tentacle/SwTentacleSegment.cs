using Eris;
using Eris.Renderer;
using ErisMath;

namespace SpoonWitch.Game.Entity.Component.Ik.Tentacle;
public class SwTentacleSegment
{
    public required SwTentacleComponent Parent;
    public SwTentacleSegment? LastSegment;
    public SwTentacleSegment? NextSegment;
    public ErVec2 Direction = ErVec2.Right;
    public ErVec2 GlobalPos;
    public ErVec2 Up => new(Direction.Y,-Direction.X);
    public ErVec2 Down => new(-Direction.Y,Direction.X);
    public ErVec2 Above => GlobalPos + Up * Radius;
    public ErVec2 Below => GlobalPos + Down * Radius;
    public double Radius = 3;
    public double Length;
    public List<SwTentacleSpine> Spines = [];
    public ErColor Color = ErColor.Blue;// new(152,184,75);
    public SwTentacleSpine AddNewSpine()
    {
        var spine = SwTentacleSpine.SegmentInitSpine(this);
        Spines.Add(spine);
        return spine;
    }
    public void Update()
    {
        if(NextSegment is not null && LastSegment is not null)Direction = (NextSegment.GlobalPos - LastSegment.GlobalPos).Normalized();
        else if(NextSegment is not null) Direction = (NextSegment.GlobalPos - GlobalPos).Normalized();
        else if(LastSegment is not null) Direction = (GlobalPos - LastSegment.GlobalPos).Normalized();
    }
    public void Draw()
    {
        if(LastSegment is not null) DrawLink(LastSegment);
        if(NextSegment is not null)
        {
            DrawLink(NextSegment);
            ErEngine.Renderer.DrawCircle(GlobalPos,Radius,12,Color);
        }
        else DrawTip();
    }
    public void DrawTip()
    {
        ErEngine.Renderer.DrawTriangle(Above,Below,GlobalPos+Direction*Radius,Color);
    }
    private void DrawLink(SwTentacleSegment segment)
    {
        ErEngine.Renderer.DrawQuad(
            Above,
            segment.GlobalPos + Up * segment.Radius,
            segment.GlobalPos + Down * segment.Radius,
            Below,
            Color);
    }
}
