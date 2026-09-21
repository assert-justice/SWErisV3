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
    public static readonly PriDb Prototypes = new();
    private static readonly List<nint> PalletLookup = [];
    private static readonly Dictionary<string, Func<string,PriNode?>> Converters;
    // Note: these are the file extensions where the path is extended relative to the game data path
    private static readonly HashSet<string> NormalizedExtensions = [".png"];
    static SwData()
    {
        static PriNode? json(string filepath)
        {
            if(!TryLoadPrion(filepath, out var node)) return null;
            return node;
        }
        Converters = new()
        {
            {".json", json},
        };
    }
    public static bool TryInit()
    {
        if(!ErTexture.TryGetPaletteHandles(out var palletHandles, "game_data/palettes.png")) return ErEngine.LogError("unable to load palettes");
        foreach (var item in palletHandles)
        {
            PalletLookup.Add(item);
        }
        if(!TryLoadAndExpand(out var data, Path.Join(GAME_DATA_PATH, "prototypes.json"))) return ErEngine.LogError("unable to load prototypes");
        Prototypes.SetData(data);
        return true;
    }
    public static int PaletteCount => PalletLookup.Count;
    public static bool TryGetPallet(out nint palletHandle, int palletIdx)
    {
        palletHandle = default;
        // Note, a pallet index of 0 is the default pallet, so valid pallet indicies start at 1
        // We decrement the pallet index to get it back in range
        palletIdx--;
        if(palletIdx < 0 || palletIdx >= PalletLookup.Count) return false;
        palletHandle = PalletLookup[palletIdx];
        return true;
    }
    public static bool TryGetPalletTexture(out ErTexture texture, string filepath, int palletIdx)
    {
        texture = default!;
        if(!TryGetPallet(out nint palletHandle, palletIdx)) return ErEngine.LogError("invalid pallet id ", palletIdx);
        if(!ErTexture.TryFromPath(filepath, palletHandle, out texture)) return ErEngine.LogError("failed to get palleted texture at filepath ", filepath);
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
    public static bool TryLoadAndExpand(out PriNode data, string filepath)
    {
        data = PriNull.Null;
        string dp = Path.GetDirectoryName(filepath)!;
        if(!TryLoadPrion(filepath, out var src)) return false;
        data = Expand(src, dp);
        return true;
    }
    private static PriNode Expand(PriNode srcNode, string dirpath)
    {
        if(srcNode.TryAs(out string filepath))
        {
            if(TryExpand(out var node, filepath, dirpath)) return node;
        }
        else if(srcNode is PriDict srcDict)
        {
            PriDict dict = [];
            foreach (var (key, value) in srcDict.Data)
            {
                dict.Data.Add(key, Expand(value, dirpath));
            }
            return dict;
        }
        else if(srcNode is PriList srcList)
        {
            PriList list = [];
            foreach (var item in srcList.Data)
            {
                list.Data.Add(Expand(item, dirpath));
            }
            return list;
        }
        return srcNode.DeepCopy();
    }
    private static bool TryExpand(out PriNode node, string filepath, string dirpath)
    {
        node = PriNull.Null;
        filepath = Path.Join(dirpath, filepath);
        if(!Path.HasExtension(filepath)) return false;
        string ext = Path.GetExtension(filepath);
        if(Converters.TryGetValue(ext, out var fn))
        {
            if(fn(filepath) is not PriNode n) return false;
            node = Expand(n, Path.GetDirectoryName(filepath)!);
            return true;
        }
        else if (NormalizedExtensions.Contains(ext))
        {
            node = new PriString(filepath);
            return true;
        }
        return false;
    }
}
