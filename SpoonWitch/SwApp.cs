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
    public static readonly PriDb Manifest = new();
    // public static double GameSpeed => IsPaused ? GameSpeedMul : 0;
    // public static double GameSpeedMul => 1;
    // public static bool IsPaused{get; private set;} = false;
    public const string GAME_DATA_PATH = "game_data";
    // public static bool Debug{get; private set;} = ;
    public static bool Debug => true;// Settings.TryGet("debug/debug", out bool debug) && debug;
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
        if (!SwData.TryInit())
        {
            ErEngine.LogError("game initialization failed");
            return;
        }
        if(!TryLoadDb(Manifest, $"{GAME_DATA_PATH}/manifest.json"))
        {
            ErEngine.LogError("no manifest found");
            return;
        }
        if(!TryLoadDb(Settings, "game_data/settings/example_settings.json", "game_data/settings/default_settings.json")) ErEngine.LogWarning("bad settings");
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
    public static bool TryLoadDb(PriDb db, string path)
    {
        if(!TryLoadPrion(path, out var node)) return false;
        return db.TrySet("", node);
    }
    public static bool TryLoadDb(PriDb db, string path, string defaultPath)
    {
        if(!TryLoadDb(db, defaultPath)) return false;
        if(TryLoadPrion(path, out var node))
        {
            if(!db.TryMerge("", node)) return ErEngine.LogWarning("failed to merge");
        }
        return true;
    }
    public static bool TrySaveDb(string path, PriDb db)
    {
        return false;
    }
    public static bool TryGetManPath(string dbPath, out string filepath)
    {
        filepath = string.Empty;
        if(!Manifest.TryGet(dbPath, out string fPath)) return false;
        filepath = Path.Join(GAME_DATA_PATH, fPath);
        return true;
    }
    public static bool TryGetManJsonPath(string dbPath, out PriNode node, out string filepath)
    {
        node = PriNull.Null;
        if(!TryGetManPath(dbPath, out filepath)) return false;
        return TryLoadPrion(filepath, out node);
    }
    public static bool TryGetManJson(string dbPath, out PriNode node)
    {
        return TryGetManJsonPath(dbPath, out node, out _);
    }
    public static bool TryGetManJsonDirpath(string dbPath, out PriNode node, out string dirpath)
    {
        dirpath = string.Empty;
        if(!TryGetManJsonPath(dbPath, out node, out string filepath)) return false;
        var path = Path.GetDirectoryName(filepath);
        if(path is null) return false;
        dirpath = path;
        return true;
    }
    public static bool TryGetTex(PriNode priNode, string key, out ErTexture texture)
    {
        texture = default!;
        if(!priNode.TryGet(key, out string filepath)) return false;
        return ErTexture.TryFromPath(filepath, out texture);
    }
    public static bool TryGetTex(PriNode priNode, string key, string dirpath, out ErTexture texture)
    {
        texture = default!;
        if(!priNode.TryGet(key, out string filepath)) return false;
        return ErTexture.TryFromPath(Path.Join(dirpath, filepath), out texture);
    }
}