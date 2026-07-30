using System.Text.Json.Nodes;
using Eris;
using Eris.App;
using Eris.Renderer;
using ErisMath;
using Prion.Node;
using Prion.Parser;
using SpoonWitch.Command;
using SpoonWitch.Game;

namespace SpoonWitch;

public class SwApp : IErApp
{
    public const int INTERNAL_WIDTH = 640;
    public const int INTERNAL_HEIGHT = 360;
    public const int HUD_HEIGHT = 40;
    private SwGame? Game;
    private static int NextId;
    private ErTexture RenderTexture;
    public static readonly SwCommandStore CommandStore = new();
    public static int Main()
    {
        SwApp app = new();
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
        Game = new();
        // Game.Init();
        // Game.AddEntity(SwPlayer.Primary);
        SwGame.TryLoadMap("game_data/map/demo_map.ldtk");
    }
    public void Update()
    {
        Game?.Update();
        CommandStore.Flush();
    }
    public void Draw()
    {
        ErEngine.Renderer.PushViewport(ErVec2.Zero, RenderTexture);
        Game?.Draw();
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
}