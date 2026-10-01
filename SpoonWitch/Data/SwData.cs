using System.Text.Json.Nodes;
using Eris;
using Eris.Renderer;
using Prion.Db;
using Prion.Node;
using Prion.Parser;
using SpoonWitch.Game.Map.MapData;

namespace SpoonWitch.Data;

public static class SwData
{
    public static readonly string ManifestPath = "game_data/manifest.json";
    public static readonly PriDb Settings = new();
    public static readonly PriDb SaveData = new();
    public static readonly PriDb Manifest = new();
    public static readonly PriDb Prototypes = new();
    public static readonly PriDb UiConfig = new();
    private static string GameDataPath => Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SpoonWitch");
    // public static string DefaultFontPath{get; private set;} = null!;
    private static readonly List<nint> PalletLookup = [];
    public static bool TryLoadManifest()
    {
        if(!TryLoadAndExpand(out var data, ManifestPath, [".json", ".ldtk", ".ttf", ".png"])) return ErEngine.LogError("unable to load manifest");
        Manifest.SetData(data);
        if(!TryLoadSettings()) return ErEngine.LogError("unable to load settings");
        return true;
    }
    public static bool TryLoadPrototypes()
    {
        if(!Manifest.TryGet("prototypes", out string prototypesPath)) return ErEngine.LogWarning("no prototype path");
        if(!TryLoadAndExpand(out var data, prototypesPath, [".png"], [".json"])) return ErEngine.LogWarning("failed to load prototypes");
        Prototypes.SetData(data);
        return true;
    }
    public static bool TryLoadUiConfig()
    {
        if(!Manifest.TryGet("ui_config", out string uiConfigPath)) return ErEngine.LogWarning("no ui config path");
        if(!TryLoadAndExpand(out var data, uiConfigPath, [".png", ".ttf"], [".json"])) return ErEngine.LogWarning("failed to load ui config");
        UiConfig.SetData(data);
        return true;
    }
    public static bool TryLoadPallets()
    {
        if(!Manifest.TryGet("palettes", out string paletteFilepath)) return ErEngine.LogError("no valid pallet filepath");
        if(!ErTexture.TryGetPaletteHandles(out var paletteHandles, paletteFilepath)) return ErEngine.LogError("unable to load palettes");
        foreach (var item in paletteHandles)
        {
            PalletLookup.Add(item);
        }
        return true;
    }
    public static bool TryLoadMap(out SwMapData mapData)
    {
        mapData = default;
        if(!Manifest.TryGet("map", out string mapDataLdtkPath)) return ErEngine.LogWarning("no map path");
        if(!TryLoadAndExpand(out var mapDataLdtk, mapDataLdtkPath, [".png"], [".json", ".ldtk"])) return ErEngine.LogWarning("failed to load map");
        if(!SwMapData.TryConvertLdtkData(out var mapDataPri, mapDataLdtk)) return ErEngine.LogWarning("failed to convert map data");
        if(!SwMapData.TryFromData(out mapData, mapDataPri)) return ErEngine.LogWarning("failed to load map data");
        return true;
    }
    public static bool TryLoadSettings()
    {
        // get default settings
        if(!Manifest.TryGet("default_settings", out string defaultSettingsPath)) return ErEngine.LogWarning("no map path");
        if(!TryLoadAndExpand(out var defaultSettings, defaultSettingsPath, [], [".json"])) return ErEngine.LogWarning("failed to load map");
        // merge user settings if present
        string userSettingsPath = Path.Join(GameDataPath, "settings.json");
        PriDict settings = [];
        settings.Merge(defaultSettings);
        if (File.Exists(userSettingsPath))
        {
            if(!TryLoadPrion(userSettingsPath, out var userSettings)) ErEngine.LogWarning("unable to read user settings");
            else settings.Merge(userSettings);
        }
        Settings.SetData(settings);
        return true;
    }
    public static bool TrySaveSettings()
    {
        try
        {
            if(!Directory.Exists(GameDataPath)) Directory.CreateDirectory(GameDataPath);
            string userSettingsPath = Path.Join(GameDataPath, "settings.json");
            PriDict settings = [];
            // todo: don't hardcode this
            settings.TrySet("game_version", "0.0.1");
            settings.Merge(Settings.Data);
            string text = PriJsonConverter.PrionToJson(settings)?.ToJsonString()!;
            File.WriteAllText(userSettingsPath, text);
            return true;
        }
        catch
        {
            return false;
        }
    }
    // Todo: implement this
    public static PriList ListSaveGames()
    {
        PriList list = [];
        return list;
    }
    private static PriDict GetDefaultSave()
    {
        PriDict res = [];
        // todo: don't hardcode this
        res.TrySet("game_version", "0.0.1");
        return res;
    }
    public static void LoadGame(int slotId)
    {
        var saveData = GetDefaultSave();
        if(TryLoadGame(slotId, out var localSaveData)) saveData.Merge(localSaveData);
        SaveData.SetData(saveData);
    }
    private static bool TryLoadGame(int slotId, out PriNode saveData)
    {
        saveData = PriNull.Null;
        try
        {
            string saveFilepath = Path.Join(GameDataPath, $"saves/save_{slotId}");
            if (!File.Exists(saveFilepath)) return false;
            string saveText = File.ReadAllText(saveFilepath);
            if(!TryParseJsonToPrion(saveText, out saveData)) return ErEngine.LogWarning("unable to parse save data");
            return true;
        }
        catch
        {
            return false;
        }
    }
    public static bool TrySaveGame(int slotId)
    {
        try
        {
            if(!Directory.Exists(GameDataPath)) Directory.CreateDirectory(GameDataPath);
            string saveDirpath = Path.Join(GameDataPath, "saves");
            if(!Directory.Exists(saveDirpath)) Directory.CreateDirectory(saveDirpath);
            string text = PriJsonConverter.PrionToJson(SaveData.Data)?.ToJsonString()!;
            File.WriteAllText(Path.Join(saveDirpath, $"save_{slotId}"), text);
            return true;
        }
        catch
        {
            return false;
        }
    }
    public static int PaletteCount => PalletLookup.Count;
    public static bool TryGetPallet(out nint palletHandle, int palletIdx)
    {
        palletHandle = default;
        // Note, a pallet index of 0 is the default pallet, so valid pallet indices start at 1
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
        if(!ErTexture.TryFromPath(filepath, palletHandle, out texture)) return ErEngine.LogError("failed to get paletted texture at filepath ", filepath);
        return true;
    }
    private static bool TryLoadPrion(string filepath, out PriNode priNode)
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
    public static bool TryLoadTexture(out ErTexture texture, PriNode filepathPri)
    {
        texture = default!;
        if(!filepathPri.TryAs(out string filepath)) return ErEngine.LogWarning("failed to load texture, missing filepath");
        if(!ErTexture.TryFromPath(filepath, out texture)) return ErEngine.LogWarning("failed to load texture at path ", filepath);
        return true;
    }
    // public static bool TryLoadFont()
    private static bool TryLoadAndExpand(out PriNode data, string filepath, string[]? normalizedExtensions = null, string[]? prionExtensions = null)
    {
        data = PriNull.Null;
        string dp = Path.GetDirectoryName(filepath)!;
        if(!TryLoadPrion(filepath, out var src)) return false;
        HashSet<string> normExt = normalizedExtensions is null ? [] : [..normalizedExtensions];
        HashSet<string> prionExt = prionExtensions is null ? [] : [..prionExtensions];
        data = Expand(src, dp, normExt, prionExt);
        return true;
    }
    private static PriNode Expand(PriNode srcNode, string dirpath, HashSet<string> normExt, HashSet<string> prionExt)
    {
        if(srcNode.TryAs(out string filepath))
        {
            if(TryExpand(out var node, filepath, dirpath, normExt, prionExt)) return node;
        }
        else if(srcNode is PriDict srcDict)
        {
            PriDict dict = [];
            foreach (var (key, value) in srcDict.Data)
            {
                dict.Data.Add(key, Expand(value, dirpath, normExt, prionExt));
            }
            return dict;
        }
        else if(srcNode is PriList srcList)
        {
            PriList list = [];
            foreach (var item in srcList.Data)
            {
                list.Data.Add(Expand(item, dirpath, normExt, prionExt));
            }
            return list;
        }
        return srcNode.DeepCopy();
    }
    private static bool TryExpand(out PriNode node, string filepath, string dirpath, HashSet<string> normExt, HashSet<string> prionExt)
    {
        node = PriNull.Null;
        filepath = Path.Join(dirpath, filepath);
        if(!Path.HasExtension(filepath)) return false;
        string ext = Path.GetExtension(filepath);
        if(prionExt.Contains(ext))
        {
            if(!TryLoadPrion(filepath, out var n)) return false;
            node = Expand(n, Path.GetDirectoryName(filepath)!, normExt, prionExt);
            return true;
        }
        else if (normExt.Contains(ext))
        {
            node = new PriString(filepath);
            return true;
        }
        return false;
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
}
