using ErisMath;
using Prion.Node;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Effect;

public enum SwDamageType: byte
{
    Untyped,
    Bludgeoning,
    Space,
}
public readonly struct SwDamage
{
    public (SwDamageType,double)[] Entries{get; init;}
    public ErVec2 SourcePos{get; init;}
    public SwDamage()
    {
        Entries = [];
    }
    public SwDamage((SwDamageType,double)[] entries, ErVec2? sourcePos = null)
    {
        SourcePos = sourcePos ?? ErVec2.Zero;
        Entries = entries;
    }
    public PriNode ToPri()
    {
        PriDict dict = [];
        PriList damageList = [];
        foreach (var (type,value) in Entries)
        {
            PriDict entry = [];
            entry.TrySet("type", type.ToString());
            entry.TrySet("value", value);
            damageList.Add(entry);
        }
        dict.TrySet("verb", "damage");
        dict.TrySet("entries", damageList);
        return dict;
    }
    public static bool TryFromPri(PriNode node, out SwDamage damage)
    {
        damage = default;
        var sourcePos = SwPrion.GetVec2(node, "source_pos_x", "source_pos_y");
        if(!node.TryGet("entries", out PriList list)) return false;
        var damages = new (SwDamageType,double)[list.Data.Count];
        for (int idx = 0; idx < damages.Length; idx++)
        {
            var item = list.Data[idx];
            if(!item.TryGet("type", out string typeStr)) return false;
            if(!Enum.TryParse(typeStr, true, out SwDamageType type)) return false;
            if(!item.TryGet("value", out double value)) return false;
            damages[idx] = (type, value);
        }
        damage = new(damages, sourcePos);
        return true;
    }
}