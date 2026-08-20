using ErisMath;
using Prion.Node;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Entity.Actor;

public enum SwDamageType: byte
{
    Untyped,
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
            entry.TrySet("type", (byte)type);
            entry.TrySet("value", value);
            damageList.Add(entry);
        }
        dict.TrySet("verb", "damage");
        SwPrion.TrySetVec2(dict, ErVec2.Zero, "source_pos_x", "source_pos_y");
        dict.TrySet("entries", damageList);
        return dict;
    }
    public static bool TryFromPri(PriNode node, out SwDamage damage)
    {
        damage = default;
        var sourcePos = SwPrion.GetVec2(node, "source_pos_x", "source_pos_y");
        if(!node.TryGet("entries", out PriList list)) return false;
        (SwDamageType,double)[] damages = new (SwDamageType,double)[list.Data.Count];
        for (int idx = 0; idx < damages.Length; idx++)
        {
            var item = list.Data[idx];
            if(!item.TryGet("type", out byte type)) return false;
            if(!item.TryGet("value", out double value)) return false;
            damages[idx] = ((SwDamageType)type, value);
        }
        damage = new(damages, sourcePos);
        return true;
    }
}