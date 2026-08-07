using System.Text.Json.Nodes;
using Eris;
using Eris.Renderer;
using ErisMath;
using Prion.Db;
using Prion.Node;
using Prion.Parser;

namespace SpoonWitch.Data;

public static class SwData
{
    private static readonly Dictionary<float,ErFont> FontLookup = [];
    public static string FontPath{get; set;} = "game_data/fonts/PixAntiqua.ttf";
    public const string GAME_DATA_PATH = "game_data";
    public static readonly PriDb Settings = new();
    public static readonly PriDb SaveData = new();
    public static readonly PriDb Manifest = new();
    public static bool TryInit()
    {
        return true;
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
    public static bool TryReadVector(PriNode node, string xName, string yName, out ErVec2 value)
    {
        value = default;
        if(!node.TryGet(xName, out double x)) return false;
        if(!node.TryGet(yName, out double y)) return false;
        value = new(x,y);
        return true;
    }
    public static bool TryReadVector(PriNode node, string xName, string yName, out ErVec2I value)
    {
        value = default;
        if(!node.TryGet(xName, out int x)) return false;
        if(!node.TryGet(yName, out int y)) return false;
        value = new(x,y);
        return true;
    }
    public static ErVec2 ReadVector(PriNode node, string xName, string yName, double defaultX = 0, double defaultY = 0)
    {
        if(!node.TryGet(xName, out double x)) x = defaultX;
        if(!node.TryGet(yName, out double y)) y = defaultY;
        return new(x,y);
    }
    public static ErVec2I ReadVector(PriNode node, string xName, string yName, int defaultX = 0, int defaultY = 0)
    {
        if(!node.TryGet(xName, out int x)) x = defaultX;
        if(!node.TryGet(yName, out int y)) y = defaultY;
        return new(x,y);
    }
    // public static ErVec2I TryWriteVector(PriNode node, )
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
        if(!TryGetManJsonPath(dbPath, out node, out var filepath)) return false;
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