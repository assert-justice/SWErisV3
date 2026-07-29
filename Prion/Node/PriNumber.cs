namespace Prion.Node;

public class PriNumber: PriNode
{
    public enum NumberMode
    {
        SignedInt,
        UnsignedInt,
        Float,
    }
    public enum NumberRadix
    {
        Decimal,
        Hex,
        Binary,
    }
    private static readonly Dictionary<Type,Func<PriNumber, PriNode>> Converters = [];
    static PriNumber(){
        Converters.Add(typeof(double), (n)=> new PriVariant<double>(n.ToF64()));
        Converters.Add(typeof(float), (n)=> new PriVariant<float>(n.ToF32()));
        Converters.Add(typeof(sbyte), (n)=> new PriVariant<sbyte>(n.ToI8()));
        Converters.Add(typeof(short), (n)=> new PriVariant<short>(n.ToI16()));
        Converters.Add(typeof(int), (n)=> new PriVariant<int>(n.ToI32()));
        Converters.Add(typeof(long), (n)=> new PriVariant<long>(n.ToI64()));
        Converters.Add(typeof(byte), (n)=> new PriVariant<byte>(n.ToU8()));
        Converters.Add(typeof(ushort), (n)=> new PriVariant<ushort>(n.ToU16()));
        Converters.Add(typeof(uint), (n)=> new PriVariant<uint>(n.ToU32()));
        Converters.Add(typeof(ulong), (n)=> new PriVariant<ulong>(n.ToU64()));
    }
    public readonly NumberMode Mode;
    public readonly byte[] Data;
    public readonly NumberRadix Radix;
    public override PriNodeKind Kind => PriNodeKind.Number;
    private PriNumber(byte[] data, NumberMode mode = NumberMode.SignedInt, NumberRadix radix = NumberRadix.Decimal)
    {
        Data = data;
        Mode = mode;
        Radix = radix;
    }
    public PriNumber(double value): this(BitConverter.GetBytes(value), NumberMode.Float){}
    public PriNumber(float value): this(BitConverter.GetBytes(value), NumberMode.Float){}
    public PriNumber(sbyte value, NumberRadix radix = NumberRadix.Decimal): this([(byte)value], NumberMode.SignedInt, radix){}
    public PriNumber(short value, NumberRadix radix = NumberRadix.Decimal): this(BitConverter.GetBytes(value), NumberMode.SignedInt, radix){}
    public PriNumber(int value, NumberRadix radix = NumberRadix.Decimal): this(BitConverter.GetBytes(value), NumberMode.SignedInt, radix){}
    public PriNumber(long value, NumberRadix radix = NumberRadix.Decimal): this(BitConverter.GetBytes(value), NumberMode.SignedInt, radix){}
    public PriNumber(byte value, NumberRadix radix = NumberRadix.Decimal): this([value], NumberMode.UnsignedInt, radix){}
    public PriNumber(ushort value, NumberRadix radix = NumberRadix.Decimal): this(BitConverter.GetBytes(value), NumberMode.UnsignedInt, radix){}
    public PriNumber(uint value, NumberRadix radix = NumberRadix.Decimal): this(BitConverter.GetBytes(value), NumberMode.UnsignedInt, radix){}
    public PriNumber(ulong value, NumberRadix radix = NumberRadix.Decimal): this(BitConverter.GetBytes(value), NumberMode.UnsignedInt, radix){}
    public double ToF64()
    {
        switch (Mode)
        {
            case NumberMode.SignedInt:
                return ToI64();
            case NumberMode.UnsignedInt:
                return ToU64();
            case NumberMode.Float:
            break;
        }
        if(Data.Length == sizeof(double)) return BitConverter.ToDouble(Data);
        else if(Data.Length == sizeof(float)) return BitConverter.ToSingle(Data);
        throw new Exception("bad data size");
    }
    public float ToF32(){return (float)ToF64();}
    public sbyte ToI8(){return (sbyte)Data[0];}
    public short ToI16(){return (short)ToI64();}
    public int ToI32(){return (int)ToI64();}
    public long ToI64()
    {
        switch (Mode)
        {
            case NumberMode.SignedInt:
                break;
            case NumberMode.UnsignedInt:
                return (long)ToU64();
            case NumberMode.Float:
                return (long)ToF64();
        }
        switch (Data.Length)
        {
            case 1:
                return ToI8();
            case 2:
                return BitConverter.ToInt16(Data);
            case 4:
                return BitConverter.ToInt32(Data);
            case 8:
                return BitConverter.ToInt64(Data);
        }
        throw new Exception("bad data size");
    }
    public byte ToU8(){return Data[0];}
    public ushort ToU16(){return (ushort)ToU64();}
    public uint ToU32(){return (uint)ToU64();}
    public ulong ToU64()
    {
        switch (Mode)
        {
            case NumberMode.SignedInt:
                return (ulong)ToI64();
                // return ToI64();
            case NumberMode.UnsignedInt:
                break;
            case NumberMode.Float:
                return (ulong)ToF64();
        }
        switch (Data.Length)
        {
            case 1:
                return ToU8();
            case 2:
                return BitConverter.ToUInt16(Data);
            case 4:
                return BitConverter.ToUInt32(Data);
            case 8:
                return BitConverter.ToUInt64(Data);
        }
        throw new Exception("bad data size");
    }
    public override bool TryAs<T>(out T value)
    {
        if(base.TryAs(out value)) return true;
        if(!Converters.TryGetValue(typeof(T), out var fn)) return false;
        return fn(this).TryAs(out value);
    }
    public override string ToString()
    {
        Sb.Clear();
        switch (Mode)
        {
            case NumberMode.SignedInt:
                long l = ToI64();
                if(Radix == NumberRadix.Hex) Sb.Append(string.Format("0x{0:x}", l));
                else if(Radix == NumberRadix.Binary) Sb.Append(string.Format("0b{0:b}", l));
                else Sb.Append(l);
            break;
            case NumberMode.UnsignedInt:
                ulong u = ToU64();
                if(Radix == NumberRadix.Hex) Sb.Append(string.Format("0x{0:x}", u));
                else if(Radix == NumberRadix.Binary) Sb.Append(string.Format("0b{0:b}", u));
                else Sb.Append(u);
            break;
            case NumberMode.Float:
                Sb.Append(ToF64());
            break;
        }
        return Sb.ToString();
    }
}