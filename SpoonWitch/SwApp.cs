using Eris;
using Eris.App;
using Eris.Renderer;
using ErisMath;
using SpoonWitch.Command;
using SpoonWitch.Data;
using SpoonWitch.Game;
using SpoonWitch.Game.Entity.Actor.Player;
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
    private bool GameSavingEnabled = false;
    private bool SettingSavingEnabled = true;
    private SwMenuHolder MenuHolder = null!;
    private readonly SwPlayerInput MenuInput = new();
    private static int NextId;
    private ErTexture RenderTexture = null!;
    public static readonly SwCommandQueue CommandQueue = new();
    // public static double GameSpeed => IsPaused ? GameSpeedMul : 0;
    // public static double GameSpeedMul => 1;
    public static bool IsPaused{get; private set;} = true;
    public static bool Debug => false;// Settings.TryGet("debug/debug", out bool debug) && debug;
    public static int Main()
    {
        SwApp app = new();
        if (!SwData.TryLoadManifest())
        {
            ErEngine.LogError("game initialization failed");
            return 1;
        }
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
        if (!TryInit())
        {
            ErEngine.LogError("initialization failed");
            return;
        }
        MenuInput.SetProfileAll(SwData.Settings.Get("input_binds"));
    }
    private bool TryInit()
    {
        if(!SwData.TryLoadPallets()) return ErEngine.LogWarning("failed to load pallets");
        if(!SwData.TryLoadUiConfig()) return ErEngine.LogWarning("failed to load ui config");
        if(!SwData.TryLoadPrototypes()) return ErEngine.LogWarning("failed to load prototypes");
        if(!SwUiNode.TryFromPrion(SwData.UiConfig.Get("menu_config"), out SwMenuHolder menuHolder))
        {
            return ErEngine.LogWarning("failed to load menu config");
        }
        MenuHolder = menuHolder;
        return true;
    }
    private void Launch()
    {
        UnPause();
        if(Game is not null)
        {
            Game.Cleanup();
            if (GameSavingEnabled)
            {
                if(!SwData.TrySaveGame(0)) ErEngine.LogWarning("failed to save game");
            }
            CommandQueue.Clear();
        }
        if(!SwData.TryLoadMap(out var mapData))
        {
            ErEngine.Quit();
            return;
        }
        SwData.LoadGame(0);
        Game = new(mapData, 1);
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
        else PollMenu();
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
        if (SettingSavingEnabled)
        {
            if(!SwData.TrySaveSettings()) ErEngine.LogWarning("failed to save settings");
        }
        if (GameSavingEnabled)
        {
            if(!SwData.TrySaveGame(0)) ErEngine.LogWarning("failed to save game");
        }
    }
    private void PollMenu()
    {
        MenuInput.Poll();
        if(MenuInput.UiCancelJustDown) MenuHolder.Cancel();
        if(MenuInput.UiConfirmJustDown) MenuHolder.Confirm();
        if(MenuInput.UiUpJustDown) MenuHolder.Up();
        if(MenuInput.UiDownJustDown) MenuHolder.Down();
        if(MenuInput.UiLeftJustDown) MenuHolder.Left();
        if(MenuInput.UiRightJustDown) MenuHolder.Right();
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
