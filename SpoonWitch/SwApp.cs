using System.Reflection;
using System.Text.Json.Nodes;
using Eris;
using Eris.App;
using Eris.Renderer;
using ErisMath;
using Prion.Db;
using Prion.Node;
using Prion.Parser;
using SpoonWitch.Command;
using SpoonWitch.Data;
using SpoonWitch.Game;
using SpoonWitch.Game.Map.Foliage;
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
    public static readonly SwCommandStore CommandStore = new();
    // public static readonly PriDb Settings = new();
    // public static readonly PriDb SaveData = new();
    // public static readonly PriDb Manifest = new();
    // private ErAudioSource Source = null!;
    // public static double GameSpeed => IsPaused ? GameSpeedMul : 0;
    // public static double GameSpeedMul => 1;
    public static bool IsPaused{get; private set;} = false;
    // public const string GAME_DATA_PATH = "game_data";
    public static bool Debug => false;// Settings.TryGet("debug/debug", out bool debug) && debug;
    private readonly SwCommandHandler CommandHandler = new(CommandStore);
    public static int Main()
    {
        SwApp app = new();
        ErEngine.Renderer.SetWindow("Spoon Witch", new(1920, 1080));
        ErEngine.Run(app);
        return 0;
    }
    public void Init()
    {
        CommandHandler.AddHandlerAction("quit", ErEngine.Quit);
        CommandHandler.AddHandlerAction("launch", Launch);
        CommandHandler.AddHandlerAction("pause", Pause);
        CommandHandler.AddHandlerAction("unpause", UnPause);
        RenderTexture = ErTexture.GetRenderTexture(INTERNAL_WIDTH,INTERNAL_HEIGHT);
        if (!SwData.TryInit())
        {
            ErEngine.LogError("game initialization failed");
            return;
        }
        // if(!Temp()) ErEngine.LogWarning("failed to load map");
        // else ErEngine.Log("map loaded!");
        // ErEngine.Quit();
        TryInitMenu();
    }
    private bool TryInitMenu()
    {
        // SwData.TryLoadPrion()
        // if(!TryLoadPrion("game_data/menus/menus.json", out var node)) return false;
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
        if(!SwTileData.TryFromData(out var tileData, SwData.Manifest.Get("map/tile_data"))) return ErEngine.LogWarning("failed to load tile data");
        if(!SwFoliageData.TryFromData(out var foliageData, SwData.Manifest.Get("map/foliage_data"))) return ErEngine.LogWarning("failed to load foliage data");
        if(!SwMapData.TryFromLdtkData(out mapData, tileData, foliageData, SwData.Manifest.Get("map/map_data"))) return ErEngine.LogWarning("failed to load map data");
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
        CommandStore.Flush();
        CommandHandler.Dispatch();
        if(!IsPaused) Game?.Update(ErEngine.DeltaTime);
        // Game?.Update(IsPaused ? 0 : ErEngine.DeltaTime);
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
        //
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
                CommandStore.AddCommandVerb("pause");
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
