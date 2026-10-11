using ErisMath;
using SDL3;

namespace Eris.Renderer;

public class ErRenderer
{
    internal ErTextureManager TextureManager{get; private set;} = null!;
    public readonly ErFontManager FontManager = new();
    private nint Window;
    private ErColor ClearColor = ErColor.Black;
    private SDL.Vertex[] Vertices = [];
    private readonly List<ErVec2> Points = [];
    public string WindowName{get; private set;} = "Eris Engine";
    public ErVec2I WindowSize{get; private set;} = new(800, 600);
    public SDL.WindowFlags WindowFlags{get; private set;}
    // public bool ShowCursor
    // {
    //     set
    //     {
    //         nint cursor = SDL.GetCursor();
    //         SDL.HideCursor()
    //     }
    // }
    public bool IsFullscreen{get; private set;}
    public nint Handle{get; private set;}
    public ErRect2 ViewportTransform{get; private set;}
    private readonly Stack<(ErRect2,nint)> ViewportStack = [];
    // private readonly Queue<Action> DebugDrawQueue = [];
    public void PushViewport(ErVec2 position, ErTexture target)
    {
        ViewportStack.Push((new(position,target.Size),target.Handle));
        UseViewport();
    }
    public void PopViewport()
    {
        if(!ViewportStack.TryPop(out _)) ErEngine.LogWarning("attempted to pop from empty viewport stack");
        else UseViewport();
    }
    private void UseViewport()
    {
        if(ViewportStack.TryPeek(out var result))
        {
            ViewportTransform = result.Item1;
            SDL.SetRenderTarget(Handle, result.Item2);
        }
        else
        {
            ViewportTransform = new(ErVec2.Zero, (ErVec2)WindowSize);
            SDL.SetRenderTarget(Handle, 0);
        }
    }
    private void ResetViewport()
    {
        ViewportTransform = new(ErVec2.Zero, ErVec2.One);
        SDL.SetRenderTarget(Handle, 0);
        ViewportStack.Clear();
    }
    private readonly Stack<Action> CleanupStack = new();
    public void Init()
    {
        if (!SDL.Init(SDL.InitFlags.Video | SDL.InitFlags.Gamepad))
        {
            ErEngine.LogError($"SDL could not initialize: {SDL.GetError()}");
            return;
        }
        CleanupStack.Push(SDL.Quit);
        if (!SDL.CreateWindowAndRenderer(WindowName, WindowSize.X, WindowSize.Y, WindowFlags, out Window, out var renderer))
        {
            ErEngine.LogError($"Error creating window and rendering: {SDL.GetError()}");
            return;
        }
        CleanupStack.Push(()=>SDL.DestroyWindow(Window));
        CleanupStack.Push(()=>SDL.DestroyRenderer(Handle));
        SDL.HideCursor();
        if (!TTF.Init())
        {
            ErEngine.LogError($"Error initializing font renderer: {SDL.GetError()}");
            return;
        }
        CleanupStack.Push(TTF.Quit);
        CleanupStack.Push(FontManager.Cleanup);
        Handle = renderer;
        SDL.SetRenderVSync(Handle, 1);
        SDL.SetDefaultTextureScaleMode(Handle, SDL.ScaleMode.Nearest);
        SDL.SetRenderDrawBlendMode(Handle, SDL.BlendMode.Blend);
        ResetViewport();
        TextureManager = new(Handle);
        CleanupStack.Push(TextureManager.Cleanup);
    }
    public void Cleanup()
    {
        while(CleanupStack.TryPop(out Action? result)) result();
    }
    public void SetWindow(string? name = null, ErVec2I? size = null, SDL.WindowFlags windowFlags = 0)
    {
        if(Window == 0)
        {
            if(name is not null) WindowName = name;
            if(size is not null) WindowSize = size.Value;
            WindowFlags = windowFlags;
            return;
        }
        ErEngine.LogError("Setting the size of the window is not yet implemented");
    }
    public void BeginRender()
    {
        Clear();
    }
    public void EndRender()
    {
        // FlushDebug();
        SDL.RenderPresent(Handle);
    }
    public void Clear()
    {
        SDL.SetRenderDrawColor(Handle, ClearColor.R, ClearColor.G, ClearColor.B, ClearColor.A);
        SDL.RenderClear(Handle);
    }
    public void SetClearColor(ErColor color)
    {
        ClearColor = color;
    }
    public void DrawRect(ErRect2 rect, ErColor color, bool filled = true)
    {
        rect = rect.Translate(-ViewportTransform.Position);
        SDL.SetRenderDrawColor(Handle, color.R, color.G, color.B, color.A);
        if (filled)SDL.RenderFillRect(Handle, rect.ToSdlRect());
        else SDL.RenderRect(Handle, rect.ToSdlRect());
    }
    public void DrawLine(ErVec2 start, ErVec2 end, ErColor color)
    {
        start -= ViewportTransform.Position;
        end -= ViewportTransform.Position;
        SDL.SetRenderDrawColor(Handle, color.R, color.G, color.B, color.A);
        SDL.RenderLine(Handle, (float)start.X, (float)start.Y, (float)end.X, (float)end.Y);
    }
    public void DrawTriangle(ErVec2 a, ErVec2 b, ErVec2 c, ErColor color)
    {
        Points.Clear();
        Points.Add(a-ViewportTransform.Position);
        Points.Add(b-ViewportTransform.Position);
        Points.Add(c-ViewportTransform.Position);
        UpdatePoints(color);
        SDL.RenderGeometry(Handle, 0, Vertices, Points.Count, 0, 0);
    }
    public void DrawTriangles(IList<ErVec2> points, ErColor color)
    {
        Points.Clear();
        for (int idx = 0; idx < points.Count - 3; idx += 3)
        {
            Points.Add(points[idx]-ViewportTransform.Position);
            Points.Add(points[idx+1]-ViewportTransform.Position);
            Points.Add(points[idx+2]-ViewportTransform.Position);
        }
        UpdatePoints(color);
        SDL.RenderGeometry(Handle, 0, Vertices, Points.Count, 0, 0);
    }
    public void DrawQuads(IList<ErVec2> points, ErColor color)
    {
        Points.Clear();
        for (int idx = 0; idx <= points.Count - 4; idx += 4)
        {
            var a = points[idx]-ViewportTransform.Position;
            var b = points[idx+1]-ViewportTransform.Position;
            var c = points[idx+2]-ViewportTransform.Position;
            var d = points[idx+3]-ViewportTransform.Position;
            Points.Add(a);
            Points.Add(b);
            Points.Add(c);
            Points.Add(c);
            Points.Add(d);
            Points.Add(a);
        }
        UpdatePoints(color);
        SDL.RenderGeometry(Handle, 0, Vertices, Points.Count, 0, 0);
    }
    public void DrawQuad(ErVec2 a, ErVec2 b, ErVec2 c, ErVec2 d, ErColor color)
    {
        a -= ViewportTransform.Position;
        b -= ViewportTransform.Position;
        c -= ViewportTransform.Position;
        d -= ViewportTransform.Position;
        Points.Clear();
        Points.Add(a);
        Points.Add(b);
        Points.Add(c);
        Points.Add(c);
        Points.Add(d);
        Points.Add(a);
        UpdatePoints(color);
        SDL.RenderGeometry(Handle, 0, Vertices, Points.Count, 0, 0);
    }
    public void DrawQuadLines(ErVec2 a, ErVec2 b, ErVec2 c, ErVec2 d, ErColor color)
    {
        DrawLine(a,b,color);
        DrawLine(b,c,color);
        DrawLine(c,d,color);
        DrawLine(d,a,color);
    }
    public void DrawCircle(ErVec2 center, double radius, int numSides, ErColor color, double angle = 0)
    {
        Points.Clear();
        center -= ViewportTransform.Position;
        double da = ErMath.TAU / numSides;
        ErVec2 lastPoint = ErVec2.FromAngle(angle) * radius + center;
        for (int idx = 0; idx < numSides; idx++)
        {
            Points.Add(lastPoint);
            Points.Add(center);
            angle += da;
            lastPoint = ErVec2.FromAngle(angle) * radius + center;
            Points.Add(lastPoint);
        }
        UpdatePoints(color);
        SDL.RenderGeometry(Handle, 0, Vertices, Points.Count, 0, 0);
    }
    public void DrawCircleLines(ErVec2 center, double radius, int numSides, ErColor color, double angle = 0)
    {
        double da = ErMath.TAU / numSides;
        ErVec2 lastPoint = ErVec2.FromAngle(angle) * radius + center;
        for (int idx = 0; idx < numSides; idx++)
        {
            angle += da;
            ErVec2 point = ErVec2.FromAngle(angle) * radius + center;
            DrawLine(lastPoint, point, color);
            lastPoint = point;
        }
    }
    public void DrawPoint(ErVec2 point, ErColor color)
    {
        SDL.SetRenderDrawColor(Handle, color.R, color.G, color.B, color.A);
        SDL.RenderPoint(Handle, (float)point.X, (float)point.Y);
    }
    public void DrawArc(ErVec2 center, double radius, double startAngle, double endAngle, int numSides, ErColor color)
    {
        if(numSides <= 0) return;
        Points.Clear();
        center -= ViewportTransform.Position;
        double da = (endAngle - startAngle) / numSides;
        double angle = startAngle;
        ErVec2 lastPoint = ErVec2.FromAngle(angle) * radius + center;
        for (int idx = 0; idx < numSides; idx++)
        {
            Points.Add(lastPoint);
            Points.Add(center);
            angle += da;
            lastPoint = ErVec2.FromAngle(angle) * radius + center;
            Points.Add(lastPoint);
        }
        UpdatePoints(color);
        SDL.RenderGeometry(Handle, 0, Vertices, Points.Count, 0, 0);
    }
    public void DrawArcLines(ErVec2 center, double radius, double startAngle, double endAngle, int numSides, ErColor color)
    {
        if(numSides <= 0) return;
        Points.Clear();
        double da = (endAngle - startAngle) / numSides;
        double angle = startAngle;
        ErVec2 lastPoint = ErVec2.FromAngle(angle) * radius + center;
        for (int idx = 0; idx < numSides; idx++)
        {
            angle += da;
            ErVec2 point = ErVec2.FromAngle(angle) * radius + center;
            DrawLine(lastPoint, point, color);
            lastPoint = point;
        }
        UpdatePoints(color);
        SDL.RenderGeometry(Handle, 0, Vertices, Points.Count, 0, 0);
    }
    public void DrawFanLines(ErVec2 center, double startAngle, double endAngle, double startRadius, double endRadius, int numSides, ErColor color)
    {
        if(numSides <= 0) return;
        Points.Clear();
        double da = (endAngle - startAngle) / numSides;
        double dr = (endRadius - startRadius) / numSides;
        double angle = startAngle;
        double radius = startRadius;
        ErVec2 lastPoint = ErVec2.FromAngle(angle) * radius + center;
        for (int idx = 0; idx < numSides; idx++)
        {
            angle += da;
            radius += dr;
            ErVec2 point = ErVec2.FromAngle(angle) * radius + center;
            DrawLine(lastPoint, point, color);
            lastPoint = point;
        }
        UpdatePoints(color);
        SDL.RenderGeometry(Handle, 0, Vertices, Points.Count, 0, 0);
    }
    private void UpdatePoints(ErColor color)
    {
        var sColor = color.ToSdlFColor();
        if(Points.Count > Vertices.Length) Vertices = new SDL.Vertex[Points.Count];
        for (int idx = 0; idx < Points.Count; idx++)
        {
            Vertices[idx] = new()
            {
                Position = Points[idx].ToSdlPoint(),
                Color = sColor,
            };
        }
    }
}
