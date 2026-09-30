using Eris;
using Eris.App;
using Eris.Renderer;
using ErisMath;
using SpoonWitch.Command;
using SpoonWitch.Data;
using SpoonWitch.Game;
using SpoonWitch.Game.Map.MapData;
using SpoonWitch.UI.Node;

namespace SpoonWitch;

public class SwApp : IErApp
{
    public const int INTERNAL_WIDTH = 640;
    public const int INTERNAL_HEIGHT = 360;
    public const int HUD_HEIGHT = 40;
    public static readonly ErVec2 ScreenSize = new(INTERNAL_WIDTH, INTERNAL_HEIGHT);
    public static readonly ErVec2 CameraSize = new(INTERNAL_WIDTH, INTERNAL_HEIGHT - HUD_HEIGHT);
    private SwGame? Game;
    private SwMenuHolder MenuHolder = null!;
    private static int NextId;
    private ErTexture RenderTexture = null!;
    public static readonly SwCommandQueue CommandQueue = new();
    // public static double GameSpeed => IsPaused ? GameSpeedMul : 0;
    // public static double GameSpeedMul => 1;
    public static bool IsPaused{get; private set;} = false;
    public static bool Debug => false;// Settings.TryGet("debug/debug", out bool debug) && debug;
    public static int Main()
    {
        SwApp app = new();
        ErEngine.Renderer.SetWindow("Spoon Witch", new(1920, 1080));
        ErEngine.Run(app);
        return 0;
    }
    public void Init()
    {
        CommandQueue.AddHandler("quit", (_)=>ErEngine.Quit());
        CommandQueue.AddHandler("launch", (_)=>Launch());
        CommandQueue.AddHandler("pause", (_)=>Pause());
        CommandQueue.AddHandler("unpause", (_)=>UnPause());
        RenderTexture = ErTexture.GetRenderTexture(INTERNAL_WIDTH,INTERNAL_HEIGHT);
        if (!SwData.TryInit())
        {
            ErEngine.LogError("game initialization failed");
            return;
        }
        TryInitMenu();
    }
    private bool TryInitMenu()
    {
        if(!SwUiNode.TryFromPrion(SwData.Manifest.Get("menu_config"), out SwMenuHolder menuHolder)) return false;
        MenuHolder = menuHolder;
        return true;
    }
    private void Launch()
    {
        UnPause();
        Game?.Cleanup();
        if(!TryLoadMap(out var mapData))
        {
            ErEngine.Quit();
            return;
        }
        Game = new(mapData, 1);
    }
    private static bool TryLoadMap(out SwMapData mapData)
    {
        mapData = default!;
        if(!SwMapData.TryConvertLdtkData(out var mapDataPri, SwData.Manifest.Get("map"))) return ErEngine.LogWarning("failed to convert map data");
        if(!SwMapData.TryFromData(out mapData, mapDataPri)) return ErEngine.LogWarning("failed to load map data");
        return true;
    }
    private void Pause()
    {
        IsPaused = true;
        MenuHolder.Visible = true;
        MenuHolder.SetMenu("pause");
    }
    private void UnPause()
    {
        IsPaused = false;
        MenuHolder.Visible = false;
    }
    public void Update()
    {
        CommandQueue.Process();
        if(!IsPaused) Game?.Update(ErEngine.DeltaTime);
        MenuInput();
        MenuHolder.Update();
    }
    public void Draw()
    {
        ErEngine.Renderer.PushViewport(ErVec2.Zero, RenderTexture);
        ErEngine.Renderer.SetClearColor(ErColor.Black);
        ErEngine.Renderer.Clear();
        if(!IsPaused) Game?.Draw();
        if(MenuHolder is not null && MenuHolder.Visible) MenuHolder.Draw();
        ErEngine.Renderer.PopViewport();
        RenderTexture.DrawFullscreen();
    }
    public void Cleanup()
    {
        CommandQueue.Clear();
    }
    private bool Up;
    private bool Down;
    private bool Left;
    private bool Right;
    private bool Confirm;
    private bool Cancel;
    private void MenuInput()
    {
        if(!MenuHolder.Visible)
        {
            if (ErEngine.Input.GetKeyDown(SDL3.SDL.Scancode.Escape))
            {
                MenuHolder.Visible = true;
                CommandQueue.AddCommandVerb("pause");
            }
            return;
        }
        bool pressed = ErEngine.Input.GetKeyDown(SDL3.SDL.Scancode.Up);
        if(pressed && !Up) MenuHolder.Up();
        Up = pressed;
        pressed = ErEngine.Input.GetKeyDown(SDL3.SDL.Scancode.Down);
        if(pressed && !Down) MenuHolder.Down();
        Down = pressed;
        pressed = ErEngine.Input.GetKeyDown(SDL3.SDL.Scancode.Left);
        if(pressed && !Left) MenuHolder.Left();
        Left = pressed;
        pressed = ErEngine.Input.GetKeyDown(SDL3.SDL.Scancode.Right);
        if(pressed && !Right) MenuHolder.Right();
        Right = pressed;
        pressed = ErEngine.Input.GetKeyDown(SDL3.SDL.Scancode.Space);
        if(pressed && !Confirm) MenuHolder.Confirm();
        Confirm = pressed;
        pressed = ErEngine.Input.GetKeyDown(SDL3.SDL.Scancode.Escape);
        if(pressed && !Cancel) MenuHolder.Cancel();
        Cancel = pressed;
    }
    public static int GetNextId()
    {
        int id = NextId;
        // Todo: check for overflow
        NextId++;
        return id;
    }
    public static int PeekNextId()
    {
        return NextId;
    }
}
