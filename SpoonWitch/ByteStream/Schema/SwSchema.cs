using ErisMath;

namespace SpoonWitch.ByteStream.Schema;

public interface ISwSchema
{
    public int SizeOf{get;}
    public void Write(SwByteStream byteStream);
    public bool TryRead(SwByteStream byteStream);
}
public struct SwSchemaByte : ISwSchema
{
    public byte Value;
    public readonly int SizeOf => sizeof(byte);
    // public readonly byte Value => _Value;
    public bool TryRead(SwByteStream byteStream)
    {
        return byteStream.TryReadByte(out Value);
    }
    public readonly void Write(SwByteStream byteStream)
    {
        byteStream.WriteByte(Value);
    }
}
public struct SwSchemaBool : ISwSchema
{
    private bool _Value;
    public readonly int SizeOf => sizeof(bool);
    public SwSchemaBool(){}
    public SwSchemaBool(bool value){_Value = value;}
    public readonly bool Value => _Value;
    public bool TryRead(SwByteStream byteStream)
    {
        return byteStream.TryReadBool(out _Value);
    }
    public readonly void Write(SwByteStream byteStream)
    {
        byteStream.WriteBool(_Value);
    }
}
public struct SwSchemaI32 : ISwSchema
{
    // private int Value;
    public readonly int SizeOf => sizeof(int);
    public int Value;
    public readonly void Write(SwByteStream byteStream)
    {
        byteStream.WriteI32(Value);
    }
    public bool TryRead(SwByteStream byteStream)
    {
        return byteStream.TryReadI32(out Value);
    }
}
public struct SwSchemaF64 : ISwSchema
{
    // private double _Value;
    public readonly int SizeOf => sizeof(double);
    public double Value;// => _Value;
    public readonly void Write(SwByteStream byteStream)
    {
        byteStream.WriteF64(Value);
    }
    public bool TryRead(SwByteStream byteStream)
    {
        return byteStream.TryReadF64(out Value);
    }
}
public struct SwSchemaVec2 : ISwSchema
{
    private ErVec2 _Value;
    public readonly int SizeOf => sizeof(double) * 2;
    public readonly ErVec2 Value => _Value;
    public SwSchemaVec2(){}
    public SwSchemaVec2(ErVec2 value){_Value = value;}
    public readonly void Write(SwByteStream byteStream)
    {
        byteStream.WriteVec2(_Value);
    }
    public bool TryRead(SwByteStream byteStream)
    {
        return byteStream.TryReadVec2(out _Value);
    }
    // public void SetValue(ErVec2 value){Value = value;}
}
public struct SwSchemaRect2 : ISwSchema
{
    private double[] Val = [0,0,0,0];
    public readonly int SizeOf => sizeof(double) * 4;
    public readonly ErRect2 Value => new(Val[0],Val[1],Val[2],Val[3]);
    public SwSchemaRect2(){}
    // public SwSchemaRect2(ErVec2 value){}
    public readonly void Write(SwByteStream byteStream)
    {
        byteStream.WriteF64s(Val);
    }
    public bool TryRead(SwByteStream byteStream)
    {
        return byteStream.TryReadF64s(ref Val);
        // return byteStream.TryReadRect2I(out _Value);
    }
}
public class SwDataBlob : ISwSchema
{
    private readonly int _SizeOf;
    public int SizeOf => _SizeOf;
    private (int,ISwSchema)[] Fields;
    private readonly Dictionary<string,int> Lookup;
    public SwDataBlob((string,ISwSchema)[] fields)
    {
        Fields = new (int,ISwSchema)[fields.Length];
        Lookup = new(fields.Length);
        int offset = 0;
        for (int slot = 0; slot < fields.Length; slot++)
        {
            var (name, value) = fields[slot];
            Fields[slot] = (offset,value);
            offset += value.SizeOf;
            // Note: duplicate field names are prohibited
            Lookup.Add(name, slot);
        }
        _SizeOf = offset;
    }
    public bool TryRead(SwByteStream byteStream)
    {
        foreach (var (_,field) in Fields)
        {
            if(!field.TryRead(byteStream)) return false;
        }
        return true;
    }
    public void Write(SwByteStream byteStream)
    {
        foreach (var (_,field) in Fields)
        {
            field.Write(byteStream);
        }
    }
    public bool TryGet<T>(SwByteStream bs, string field, out T value) where T: ISwSchema
    {
        value = default!;
        if(!Lookup.TryGetValue(field, out int slot)) return false;
        return TryGet(bs, slot, out value);
    }
    public bool TryGet<T>(SwByteStream bs, int slot, out T value) where T: ISwSchema
    {
        value = default!;
        var (offset,schema) = Fields[slot];
        if(schema is not T sc) return false;
        int head = bs.Head;
        bs.SetHead(head + offset);
        bool res = sc.TryRead(bs);
        if(res) value = sc;
        bs.SetHead(head);
        return res;
    }
    protected T Get<T>(int slot)
    {
        var (_,schema) = Fields[slot];
        if(schema is not T sc) throw new($"type mismatch, expected '{schema.GetType()}', recieved '{typeof(T)}'");
        return sc;
    }
    public void Set<T>(SwByteStream bs, string field, in T value) where T: ISwSchema
    {
        if(!Lookup.TryGetValue(field, out int slot)) throw new($"No field of name '{field}' exists.");
        Set(bs, slot, in value);
    }
    public void Set<T>(SwByteStream bs, int slot, in T value) where T: ISwSchema
    {
        var (offset,schema) = Fields[slot];
        if(schema is not T) throw new($"type mismatch, expected '{schema.GetType()}', recieved '{value.GetType()}'");
        int head = bs.Head;
        bs.SetHead(head + offset);
        value.Write(bs);
    }
    protected void Set<T>(int slot, T value) where T: ISwSchema
    {
        var (offset,schema) = Fields[slot];
        if(schema is not T sch) throw new($"type mismatch, expected '{schema.GetType()}', recieved '{value.GetType()}'");
        Fields[slot] = (offset,sch);
    }
}