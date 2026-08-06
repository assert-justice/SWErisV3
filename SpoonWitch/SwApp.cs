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
        if(!TryLoadDb("game_data/settings/example_settings.json", "game_data/settings/default_settings.json", Settings)) ErEngine.Log("bad settings");
        TryInitMenu();
        Launch();
    }
    private bool TryInitMenu()
    {
        if(!TryLoadPrion("game_data/menus/menus.json", out var node)) return false;
        if(!SwUiNode.TryFromPrion(node, out SwMenuHolder menuHolder)) return false;
        MenuHolder = menuHolder;
        return true;
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
    public static bool TryLoadDb(string path, string defaultPath, PriDb db)
    {
        if(!TryLoadPrion(defaultPath, out var defNode)) return ErEngine.LogWarning("bad db default path");
        if(!db.TrySet("", defNode)) return ErEngine.LogWarning("failed to set");
        if(TryLoadPrion(path, out var node))
        {
            // merge
            if(!db.TryMerge("", node)) return ErEngine.LogWarning("failed to merge");
        }
        return true;
    }
    public static bool TrySaveDb(string path, PriDb db)
    {
        return false;
    }
}