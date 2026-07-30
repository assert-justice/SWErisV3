using Eris;
using Eris.Renderer;
using ErisMath;

namespace SpoonWitch.Game;

public class SwCamera
{
    private readonly ErTexture Texture;
    private readonly ErVec2 Half;
    public double Speed = 1200;
    public Action DrawFn = ()=>{};
    private static readonly ErVec2 Offset = new(0,SwApp.HUD_HEIGHT);
    private static readonly ErColor CamColor = new(100, 149, 237);
    private static readonly ErColor ClearColor = default;
    private ErRect2 Bounds;
    public bool UseBounds = false;
    private ErVec2 TargetPos;
    private ErVec2 CurrentPos;
    private ErVec2 NextPos;
    public ErVec2 Size => Texture.Size;
    public SwCamera()
    {
        Texture = ErTexture.GetRenderTexture(SwApp.INTERNAL_WIDTH,SwApp.INTERNAL_HEIGHT-SwApp.HUD_HEIGHT);
        Half = Size * 0.5f * (1-ErMath.EPSILON);
    }
    public void SetBounds(ErRect2 bounds)
    {
        // todo: check if bounds are valid
        var pos = bounds.Position + Half;
        var size = bounds.Size - Size;
        Bounds = new(pos,size);
    }
    public void SetTargetPosition(ErVec2 targetPosition)
    {
        if(!UseBounds) TargetPos = targetPosition;
        else TargetPos = Bounds.Clamp(targetPosition);
    }
    public void SnapToPosition(ErVec2 position)
    {
        SetTargetPosition(position);
        CurrentPos = NextPos;
        NextPos = TargetPos;
    }
    public bool IsInBounds()
    {
        if(!UseBounds) return true;
        return Bounds.Contains(CurrentPos);
    }
    public void Update()
    {
        CurrentPos = NextPos;
        if (IsInBounds())
        {
            NextPos = TargetPos;
            return;
        }
        var diff = TargetPos - CurrentPos;
        // Note, if diff has length 0 normalizing it doesn't work, so we check the length first
        double speed = Speed * SwGame.DeltaTimeRaw;
        if(diff.GetLengthSquared() < speed){NextPos = TargetPos;}
        else NextPos = CurrentPos + diff.Normalized() * speed;
    }
    public void Draw()
    {
        var pos = ErMath.Lerp(CurrentPos,NextPos,SwGame.FrameProgress);
        ErEngine.Renderer.PushViewport(pos-Half, Texture);
        ErEngine.Renderer.SetClearColor(CamColor);
        ErEngine.Renderer.Clear();
        DrawFn();
        ErEngine.Renderer.PopViewport();
        Texture.Draw(Offset);
        ErEngine.Renderer.SetClearColor(ClearColor);
    }
}