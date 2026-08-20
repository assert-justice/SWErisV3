using Prion.Node;

namespace Eris.Input;

public class ErInputProfile
{
    public static IEnumerable<ErVAxis2> GetAxes2(PriNode node, string[] names)
    {
        if(!node.Get("axes2").TryAs(out PriDict dict))
        {
            ErEngine.LogWarning("no axes2 found");
            yield break;
        }
        foreach (var name in names)
        {
            if(!dict.Data.TryGetValue(name, out var entry))
            {
                ErEngine.LogWarning("no axes2 named '", name ,"' found");
                yield break;
            }
            yield return new(entry);
        }
    }
    public static bool TryGetAxes2(PriNode node, string[] names, out ErVAxis2[] axis2s)
    {
        axis2s = [..GetAxes2(node, names)];
        return axis2s.Length == names.Length;
    }
    public static IEnumerable<ErVButton> GetButtons(PriNode node, string[] names)
    {
        if(!node.Get("buttons").TryAs(out PriDict dict))
        {
            ErEngine.LogWarning("no button found");
            yield break;
        }
        foreach (var name in names)
        {
            if(!dict.Data.TryGetValue(name, out var entry))
            {
                ErEngine.LogWarning("no button named '", name ,"' found");
                yield break;
            }
            yield return new(entry);
        }
    }
    public static bool TryGetButtons(PriNode node, string[] names, out ErVButton[] axis2s)
    {
        axis2s = [..GetButtons(node, names)];
        return axis2s.Length == names.Length;
    }
    public static IEnumerable<T> GetEnumArray<T>(PriNode items) where T : struct, Enum
    {
        foreach (var item in items.Values)
        {
            if(!item.TryAs(out string name))
            {
                ErEngine.LogWarning("bad name");
                continue;
            }
            if(!Enum.TryParse(name, true, out T res))
            {
                ErEngine.LogWarning("bad enum parse");
                continue;
            }
            yield return res;
        }
    }
}