using Eris;
using Eris.Renderer;
using ErisMath;

namespace SpoonWitch.Game;

public class SwCamera
{
    private static readonly ErVec2 Offset = new(0,SwApp.HUD_HEIGHT);
    private readonly ErTexture Texture = ErTexture.GetRenderTexture(SwApp.INTERNAL_WIDTH,SwApp.INTERNAL_HEIGHT-SwApp.HUD_HEIGHT);
    private ErRect2 Bounds;
    private ErVec2 TargetPos;
    private ErVec2 CurrentPos;
    private ErVec2 NextPos;
    public double Speed = 1200;
    public bool UseBounds = false;
    public ErVec2 Position => CurrentPos;
    public ErVec2 Size => Texture.Size;
    public ErRect2 Rect => ErRect2.Centered(Position, Size);
    public bool IsInBounds()
    {
        if(!UseBounds) return true;
        return Bounds.Contains(CurrentPos);
    }
    public void SetBounds(ErRect2 bounds)
    {
        // todo: check if bounds are valid
        var pos = bounds.Position + Size / 2;
        var size = bounds.Size - Size;
        Bounds = new(pos,size);
    }
    public void SetTargetPosition(ErVec2 targetPosition)
    {
        if(!UseBounds) TargetPos = targetPosition;
        else TargetPos = Bounds.Clamp(targetPosition);
    }
    public void SnapToTarget()
    {
        CurrentPos = TargetPos;
        NextPos = TargetPos;
    }
    public void SnapToTarget(ErVec2 targetPos)
    {
        SetTargetPosition(targetPos);
        SnapToTarget();
    }
    public bool IsPointVisible(ErVec2 point)
    {
        return ErRect2.Centered(Position, Size).Contains(point);
    }
    public bool IsRectVisible(ErRect2 rect)
    {
        return ErRect2.Centered(Position, Size).Contains(rect);
    }
    public void Update(double dt)
    {
        CurrentPos = NextPos;
        if (IsInBounds())
        {
            NextPos = TargetPos;
            return;
        }
        var diff = TargetPos - CurrentPos;
        // Note, if diff has length 0 normalizing it doesn't work, so we check the length first
        double speed = Speed * dt;
        if(diff.GetLengthSquared() < speed * speed){NextPos = TargetPos;}
        else NextPos = CurrentPos + diff.Normalized() * speed;
    }
    public void BeginDraw()
    {
        //
        ErEngine.Renderer.PushViewport(ErVec2.Zero, Texture);
        // Todo: take out these calls to set clear color
        // ErEngine.Renderer.SetClearColor(default);
        ErEngine.Renderer.Clear();
        // ErEngine.Renderer.SetClearColor(default);
    }
    public void EndDraw()
    {
        ErEngine.Renderer.PopViewport();
        Texture.Draw(Offset);
    }
}

// public class SwCamera
// {
//     private readonly ErTexture Texture;
//     private readonly ErVec2 Half;
//     public double Speed = 1200;
//     public Action DrawFn = ()=>{};
//     private static readonly ErVec2 Offset = new(0,SwApp.HUD_HEIGHT);
//     private static readonly ErColor CamColor = default;// new(100, 149, 237);
//     private static readonly ErColor ClearColor = default;
//     private ErRect2 Bounds;
//     public bool UseBounds = false;
//     public ErVec2 TargetPos{get; private set;}
//     private ErVec2 CurrentPos;
//     public ErVec2 Position => CurrentPos;
//     private ErVec2 NextPos;
//     public ErVec2 Size => Texture.Size;
//     public ErVec2 DrawPos;
//     public SwCamera()
//     {
//         Texture = ErTexture.GetRenderTexture(SwApp.INTERNAL_WIDTH,SwApp.INTERNAL_HEIGHT-SwApp.HUD_HEIGHT);
//         Half = Size * 0.5f * (1-ErMath.EPSILON);
//     }
//     public void SetBounds(ErRect2 bounds)
//     {
//         // todo: check if bounds are valid
//         var pos = bounds.Position + Half;
//         var size = bounds.Size - Size;
//         Bounds = new(pos,size);
//     }
//     public void SetTargetPosition(ErVec2 targetPosition)
//     {
//         if(!UseBounds) TargetPos = targetPosition;
//         else TargetPos = Bounds.Clamp(targetPosition);
//     }
//     public void SnapToPosition(ErVec2 position)
//     {
//         SetTargetPosition(position);
//         CurrentPos = NextPos;
//         NextPos = TargetPos;
//     }
//     public bool IsPointVisible(ErVec2 point)
//     {
//         return ErRect2.Centered(Position, Size).Contains(point);
//     }
//     public bool IsInBounds()
//     {
//         if(!UseBounds) return true;
//         return Bounds.Contains(CurrentPos);
//     }
//     public void Update()
//     {
//         CurrentPos = NextPos;
//         if (IsInBounds())
//         {
//             NextPos = TargetPos;
//             return;
//         }
//         var diff = TargetPos - CurrentPos;
//         // Note, if diff has length 0 normalizing it doesn't work, so we check the length first
//         double speed = Speed * SwGame.DeltaTime;
//         if(diff.GetLengthSquared() < speed){NextPos = TargetPos;}
//         else NextPos = CurrentPos + diff.Normalized() * speed;
//     }
//     public void Draw()
//     {
//         DrawPos = ErMath.Lerp(CurrentPos,NextPos,SwGame.FrameWeight)-Half;
//         ErEngine.Renderer.PushViewport(ErVec2.Zero, Texture);
//         ErEngine.Renderer.SetClearColor(CamColor);
//         ErEngine.Renderer.Clear();
//         ErEngine.Renderer.SetClearColor(ClearColor);
//         DrawFn();
//         ErEngine.Renderer.PopViewport();
//         Texture.Draw(Offset);
//     }
// }