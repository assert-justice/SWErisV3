using ErisMath;
using Prion.Node;

namespace SpoonWitch.Game.Entity.Actor;

public enum SwDamageType: byte
{
    Untyped,
}
public readonly struct SwDamage
{
    public double Value{get; init;}
    public ErVec2 SourcePos{get; init;}
    public SwDamageType Type{get; init;}
    public SwDamage(){}
    public SwDamage(double value, ErVec2 sourcePos, SwDamageType type = SwDamageType.Untyped)
    {
        Value = value;
        SourcePos = sourcePos;
        Type = type;
    }
    public PriNode ToPri()
    {
        PriDict dict = new();
        dict.Data["value"] = new PriNumber(Value);
        dict.Data["source_pos_x"] = new PriNumber(SourcePos.X);
        dict.Data["source_pos_y"] = new PriNumber(SourcePos.Y);
        dict.Data["type"] = new PriNumber((byte)Type);
        return dict;
    }
    public static bool TryFromPri(PriNode node, out SwDamage damage)
    {
        damage = default;
        if(!node.TryGet("value", out double value)) return false;
        if(!ErVec2.TryFromPrion(out var sourcePos, node, "source_pos_x", "source_pos_y")) return false;
        if(!node.TryGet("type", out byte type)) return false;
        damage = new(value, sourcePos, (SwDamageType)type);
        return true;
    }
}