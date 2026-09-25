using ErisMath;
using Prion.Node;

namespace SpoonWitch.Utils;

public static class SwPrion
{
    public static ErVec2 GetVec2(PriNode priNode, string xName = "x", string yName = "y", ErVec2? defaultVec = null)
    {
        var def = defaultVec ?? ErVec2.Zero;
        if(!priNode.TryGet(xName, out double x)) x = def.X;
        if(!priNode.TryGet(yName, out double y)) y = def.Y;
        return new(x,y);
    }
    public static bool TryGetVec2(out ErVec2 value, PriNode priNode, string xName = "x", string yName = "y")
    {
        value = default;
        if(!priNode.TryGet(xName, out double x)) return false;
        if(!priNode.TryGet(yName, out double y)) return false;
        value = new(x,y);
        return true;
    }
    public static bool TrySetVec2(PriNode priNode, ErVec2 value, string xName = "x", string yName = "y")
    {
        if(!priNode.TrySet(xName, value.X)) return false;
        if(!priNode.TrySet(yName, value.Y)) return false;
        return true;
    }
    public static bool TrySetVec2(PriNode priNode, string key, ErVec2 value, string xName = "x", string yName = "y")
    {
        PriDict dict = [];
        TrySetVec2(dict, value, xName, yName);
        return priNode.TrySet(key, dict);
    }
    public static ErVec2I GetVec2I(PriNode priNode, string xName = "x", string yName = "y", ErVec2I? defaultVec = null)
    {
        var def = defaultVec ?? ErVec2I.Zero;
        if(!priNode.TryGet(xName, out int x)) x = def.X;
        if(!priNode.TryGet(yName, out int y)) y = def.Y;
        return new(x,y);
    }
    public static ErRect2I GetRect2I(PriNode priNode, string xName = "x", string yName = "y", string wName = "w", string hName = "h", ErRect2I? defaultRect = null)
    {
        var def = defaultRect ?? default;
        var pos = GetVec2I(priNode, xName, yName, def.Position);
        var size = GetVec2I(priNode, wName, hName, def.Size);
        return new(pos, size);
    }
    public static bool TryGetVec2I(out ErVec2I value, PriNode priNode, string xName = "x", string yName = "y")
    {
        value = default;
        if(!priNode.TryGet(xName, out int x)) return false;
        if(!priNode.TryGet(yName, out int y)) return false;
        value = new(x,y);
        return true;
    }
    public static bool TrySetVec2I(PriNode priNode, ErVec2I value, string xName = "x", string yName = "y")
    {
        if(!priNode.TrySet(xName, value.X)) return false;
        if(!priNode.TrySet(yName, value.Y)) return false;
        return true;
    }
}