using Eris;
using Eris.App;
using Eris.Renderer;
using ErisMath;
using Prion.Node;
using SpoonWitch.Command;
using SpoonWitch.Data;
using SpoonWitch.Game;
using SpoonWitch.Game.Entity.Actor.Player;
using SpoonWitch.Game.Map.MapData;
using SpoonWitch.UI.Node;
using SpoonWitch.Utils;

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
    private bool IsGameVisible;
    private readonly SwClock TextClock = new();
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
        CommandQueue.AddHandler("launch", Launch);
        CommandQueue.AddHandler("pause", (_)=>Pause());
        CommandQueue.AddHandler("unpause", (_)=>UnPause());
        CommandQueue.AddHandler("main_menu", (_)=>MenuHolder.SetMenu("main_menu"));
        CommandQueue.AddHandler("log", LogHandler);
        CommandQueue.AddHandler("warning", WarnHandler);
        CommandQueue.AddHandler("error", ErrorHandler);
        CommandQueue.AddHandler("show_text", ShowText);
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
        if(!SwData.TryLoadPalettes()) return ErEngine.LogWarning("failed to load palettes");
        if(!SwData.TryLoadUiConfig()) return ErEngine.LogWarning("failed to load ui config");
        if(!SwData.TryLoadPrototypes()) return ErEngine.LogWarning("failed to load prototypes");
        if(!SwUiNode.TryFromPrion(SwData.UiConfig.Get("menu_config"), out SwMenuHolder menuHolder))
        {
            return ErEngine.LogWarning("failed to load menu config");
        }
        MenuHolder = menuHolder;
        return true;
    }
    private void Launch(PriNode command)
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
        Game = new(mapData, command);
        IsGameVisible = true;
    }
    private void LogHandler(PriNode command)
    {
        ErEngine.Log(command.Get("text"));
    }
    private void WarnHandler(PriNode command)
    {
        ErEngine.LogWarning(command.Get("text"));
    }
    private void ErrorHandler(PriNode command)
    {
        ErEngine.LogError(command.Get("text"));
    }
    private void HideText()
    {
        MenuHolder.Visible = false;
    }
    private void ShowText(PriNode command)
    {
        if(!command.TryGet("title", out string title)) title = string.Empty;
        if(!command.TryGet("text", out string text)) text = string.Empty;
        // if duration is 0 or missing, text will go away when the user hits a button
        // otherwise the text will stay on screen for the duration
        string menuName;
        if(!command.TryGet("duration", out double duration) || duration == 0)
        {
            IsPaused = true;
            menuName = "text";
        }
        else
        {
            // do stuff with duration
            TextClock.Start(duration);
            TextClock.OnFinish = HideText;
            menuName = "text_temp";
        }
        // find text menu
        SwMenu? textMenu = null;
        foreach (var item in MenuHolder.Children)
        {
            if(item is not SwMenu menu) continue;
            if(menu.Id != menuName) continue;
            textMenu = menu;
            break;
        }
        if(textMenu is null)
        {
            ErEngine.LogWarning("no text menu!");
            return;
        }
        if(textMenu.Children[0] is SwText titleNode) titleNode.Text = title;
        else ErEngine.LogWarning("text menu is missing title node");
        if(textMenu.Children[1] is SwText textNode) textNode.Text = text;
        else ErEngine.LogWarning("text menu is missing text node");
        MenuHolder.SetMenu(menuName);
        MenuHolder.Visible = true;
    }
    private void Pause()
    {
        IsPaused = true;
        MenuHolder.Visible = true;
        MenuHolder.SetMenu("pause");
        IsGameVisible = false;
    }
    private void UnPause()
    {
        IsPaused = false;
        MenuHolder.Visible = false;
        IsGameVisible = true;
    }
    public void Update()
    {
        TextClock.Update(ErEngine.DeltaTime);
        CommandQueue.Process();
        if(!IsPaused) Game?.Update(ErEngine.DeltaTime);
        if (MenuHolder.Visible)
        {
            if(IsPaused) PollMenu();
            MenuHolder.Update(ErEngine.DeltaTime);
        }
    }
    public void Draw()
    {
        ErEngine.Renderer.PushViewport(ErVec2.Zero, RenderTexture);
        ErEngine.Renderer.SetClearColor(ErColor.Black);
        ErEngine.Renderer.Clear();
        if(IsGameVisible) Game?.Draw();
        if(MenuHolder.Visible) MenuHolder.Draw();
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
