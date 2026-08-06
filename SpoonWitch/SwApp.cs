using System.Text.Json.Nodes;
using Eris;
using Eris.App;
using Eris.Renderer;
using ErisMath;
using Prion.Db;
using Prion.Node;
using Prion.Parser;
using SpoonWitch.Command;
using SpoonWitch.Game;
using SpoonWitch.UI.Menu;

namespace SpoonWitch;

public class SwApp : IErApp
{
    public const int INTERNAL_WIDTH = 640;
    public const int INTERNAL_HEIGHT = 360;
    public const int HUD_HEIGHT = 40;
    public static readonly ErVec2 ScreenSize = new(INTERNAL_WIDTH, INTERNAL_HEIGHT);
    public static readonly ErVec2 CameraSize = new(INTERNAL_WIDTH, INTERNAL_HEIGHT - HUD_HEIGHT);
    private SwGame? Game;
    private SwMenuHolder? MenuHolder;
    private static int NextId;
    private ErTexture RenderTexture;
    public static readonly SwCommandStore CommandStore = new();
    public static readonly PriDb Settings = new();
    public static readonly PriDb SaveData = new();
    public static bool Debug{get; private set;} = false;
    public static int Main()
    {
        SwApp app = new();
        ErEngine.Renderer.SetWindow("Spoon Witch", new(1920, 1080));
        ErEngine.Run(app);
        return 0;
    }
    public SwApp()
    {
        // Note: I care about null safety promise
        RenderTexture = default!;
    }
    public void Init()
    {
        RenderTexture = ErTexture.GetRenderTexture(INTERNAL_WIDTH,INTERNAL_HEIGHT);
        if(!TryLoadPrion("game_data/settings/default_settings.json", out var settings)) ErEngine.Log("bad settings");
        else Settings.TrySet("", settings);
        // if(Settings.TryGet("gamepad", out PriNode node)) ErEngine.Log(node);
        // Settings.TrySet("gamepad/auto_charge", PriBool.False);
        // if(Settings.TryGet("", out node)) ErEngine.Log(node);
        // if(!ErFont.TryLoad("game_data/fonts/PixAntiqua.ttf", 16, out var font)) ErEngine.LogWarning("failed to load font");
        // else Font = font;
        MenuHolder = new()
        {
            Visible = true,
        };
    }
    private void Launch()
    {
        Game = new();
        Game.TryLoadMap("game_data/map/demo_map2.ldtk");
    }
    public void Update()
    {
        CommandStore.Flush();
        Game?.Update();
    }
    public void Draw()
    {
        ErEngine.Renderer.PushViewport(ErVec2.Zero, RenderTexture);
        ErEngine.Renderer.Clear();
        Game?.Draw();
        if(MenuHolder is not null && MenuHolder.Visible)
        {
            MenuHolder.Draw();
        }
        ErEngine.Renderer.PopViewport();
        RenderTexture.DrawFullscreen();
    }
    public void Cleanup()
    {
        //
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
    public static bool TryLoadPrion(string filepath, out PriNode priNode)
    {
        priNode = PriNull.Null;
        try
        {
            string text = File.ReadAllText(filepath);
            var json = JsonNode.Parse(text);
            priNode = PriParser.Parser.JsonToPrion(json);
        }
        catch
        {
            return false;
        }
        return true;
    }
    public static bool TryParseJsonToPrion(string src, out PriNode priNode)
    {
        priNode = PriNull.Null;
        try
        {
            var json = JsonNode.Parse(src);
            priNode = PriParser.Parser.JsonToPrion(json);
        }
        catch(Exception e)
        {
            return ErEngine.LogWarning(e);
        }
        return true;
    }
    private static readonly string FontPath = "game_data/fonts/PixAntiqua.ttf";
    private static readonly Dictionary<float,ErFont> FontLookup = [];
    public static bool TryGetFont(float size, out ErFont font)
    {
        if(!FontLookup.TryGetValue(size, out font!))
        {
            if(!ErFont.TryLoad(FontPath, size, out font)) return false;
            FontLookup[size] = font; 
        }
        return true;
    }
}