using Eris;
using Eris.Renderer;
using ErisMath;

namespace SpoonWitch.Ik;

public class SwIkLimb
{
    protected readonly List<ErVec2> Segments = [];
    public ErVec2 Origin;
    private ErVec2 _Target;
    public ErVec2 Target
    {
        get => _Target;
        set
        {
            IsAtTarget = false;
            _Target = value;
        }
    }
    public double Speed = 100;
    public int NumSteps = 1;
    public bool IsAtTarget{get; private set;} = true;
    public SwIkLimb(){}
    public SwIkLimb(IList<double> distances, ErVec2? origin = null, ErVec2? target = null)
    {
        Origin = origin ?? ErVec2.Zero;
        _Target = target ?? new ErVec2(distances.Sum(), 0);
        ErVec2 dir = (_Target - Origin).Normalized();
        Segments = new(distances.Count);
        foreach (var d in distances)
        {
            Segments.Add(dir * d);
        }
    }
    public void Update(double deltaTime)
    {
        if(Segments.Count < 1) return;
        double dt = deltaTime / NumSteps;
        for (int idx = 0; idx < NumSteps; idx++)
        {
            Step(dt);
        }
    }
    public virtual void Draw(double frameTime){}
    public void DebugDraw()
    {
        if(Segments.Count < 1) return;
        var pos = Origin;
        ErVec2 nextPos;
        foreach (var segment in Segments)
        {
            nextPos = pos + segment;
            ErEngine.Renderer.DrawLine(pos, nextPos, ErColor.Blue);
            pos = nextPos;
        }
    }
    private void Step(double deltaTime)
    {
        var tip = ErVec2.Zero;
        foreach (var seg in Segments)
        {
            tip += seg;
        }
        var diff = _Target - tip;
        double speed = Speed * deltaTime;
        if(diff.GetLength() < speed)
        {
            tip = _Target;
            IsAtTarget = true;
        }
        else
        {
            tip += diff.Normalized() * speed;
        }
        var pass1 = Pass(Segments, tip, ErVec2.Zero);
        var pass2 = Pass(pass1, ErVec2.Zero, tip);
        for (int idx = 0; idx < pass2.Count; idx++)
        {
            Segments[idx] = pass2[idx];
        }
    }
    private List<ErVec2> Pass(List<ErVec2> segments, ErVec2 target, ErVec2 root)
    {
        List<ErVec2> tips = [];
        List<ErVec2> backwards = [];
        ErVec2 prevTip = root;
        ErVec2 currentTarget = target;
        foreach (var item in segments)
        {
            var tip = prevTip + item;
            prevTip = tip;
            tips.Add(tip);
        }
        ErVec2 d;
        double l;
        ErVec2 v;
        for(int idx = tips.Count - 2; idx > -1; idx--)
        {
            d = (tips[idx] - currentTarget).Normalized();
            l = segments[idx+1].GetLength();
            v = d*l;
            backwards.Add(v);
            currentTarget += v;
        }
        d = (root - currentTarget).Normalized();
        l = segments[0].GetLength();
        v = d*l;
        backwards.Add(v);
        return backwards;
    }
}
