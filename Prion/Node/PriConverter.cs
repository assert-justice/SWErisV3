namespace Prion.Node;

internal static class PriConverter
{
    private readonly struct PriCon
    {
        public Type ValType{get; init;}
        public Func<object,PriNode> ToPri{get; init;}
        public Func<PriNode,object?> FromPri{get; init;}
    }
    private static readonly Dictionary<Type, PriCon> Converters;
    static PriConverter()
    {
        PriCon[] converters = [
            new(){
                ValType = typeof(bool),
                // A nested ternary in a lambda? I may have gone too far in a few places...
                ToPri = o => o is bool b ? (b ? PriBool.True : PriBool.False) : PriNull.Null,
                FromPri = p => p is PriBool b ? b.Value : null,
            },
            new(){
                ValType = typeof(string),
                ToPri = o => o is string s ? new PriString(s) : new PriString(o.ToString() ?? "null"),
                FromPri = p => p is PriString s ? s.Value : null,
            },
            new(){
                ValType = typeof(sbyte),
                ToPri = o => o is sbyte val ? new PriNumber(val) : PriNull.Null,
                FromPri = p => p is PriNumber n ? n.ToI8() : null,
            },
            new(){
                ValType = typeof(short),
                ToPri = o => o is short val ? new PriNumber(val) : PriNull.Null,
                FromPri = p => p is PriNumber n ? n.ToI16() : null,
            },
            new(){
                ValType = typeof(int),
                ToPri = o => o is int val ? new PriNumber(val) : PriNull.Null,
                FromPri = p => p is PriNumber n ? n.ToI32() : null,
            },
            new(){
                ValType = typeof(long),
                ToPri = o => o is long val ? new PriNumber(val) : PriNull.Null,
                FromPri = p => p is PriNumber n ? n.ToI64() : null,
            },
            new(){
                ValType = typeof(byte),
                ToPri = o => o is byte val ? new PriNumber(val) : PriNull.Null,
                FromPri = p => p is PriNumber n ? n.ToU8() : null,
            },
            new(){
                ValType = typeof(ushort),
                ToPri = o => o is ushort val ? new PriNumber(val) : PriNull.Null,
                FromPri = p => p is PriNumber n ? n.ToU16() : null,
            },
            new(){
                ValType = typeof(uint),
                ToPri = o => o is uint val ? new PriNumber(val) : PriNull.Null,
                FromPri = p => p is PriNumber n ? n.ToU32() : null,
            },
            new(){
                ValType = typeof(ulong),
                ToPri = o => o is ulong val ? new PriNumber(val) : PriNull.Null,
                FromPri = p => p is PriNumber n ? n.ToU64() : null,
            },
            new(){
                ValType = typeof(float),
                ToPri = o => o is float val ? new PriNumber(val) : PriNull.Null,
                FromPri = p => p is PriNumber n ? n.ToF32() : null,
            },
            new(){
                ValType = typeof(double),
                ToPri = o => o is double val ? new PriNumber(val) : PriNull.Null,
                FromPri = p => p is PriNumber n ? n.ToF64() : null,
            },
        ];
        Converters = new(converters.Length);
        foreach (var item in converters)
        {
            Converters.Add(item.ValType, item);
        }
    }
    public static bool TryToPri<T>(T value, out PriNode priNode)
    {
        priNode = PriNull.Null;
        if(value is null) return true;
        if(!Converters.TryGetValue(typeof(T), out var con)) return false;
        priNode = con.ToPri(value);
        return priNode is not PriNull;
    }
    public static bool TryToPri<T,U>(T value, out U priNode) where U: PriNode
    {
        priNode = default!;
        if(value is null) return typeof(U) == typeof(PriNull);
        if(!TryToPri(value, out PriNode node)) return false;
        if(node is not U n) return false;
        priNode = n;
        return true;
    }
    public static bool TryFromPri<T>(PriNode priNode, out T value)
    {
        value = default!;
        if(!Converters.TryGetValue(typeof(T), out var con)) return false;
        var val = con.FromPri(priNode);
        if(val is not T v) return false;
        value = v;
        return true;
    }
}