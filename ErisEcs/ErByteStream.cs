using ErisMath;

namespace ErisEcs;

public class ErByteStream
{
    private readonly List<byte> Data;
    private readonly byte[] Bytes = new byte[sizeof(double) * 4];
    public int Head{get; private set;}
    public int Length{get; private set;}
    public ErByteStream(int capacity = 0)
    {
        if(capacity > 0) Data = new(capacity);
        else Data = [];
    }
    public void SetHead(int head){Head = head;}
    public void Reserve(int size)
    {
        if(Head + size > Data.Capacity) Data.Capacity = Head + size;
        int grow = Data.Capacity - Data.Count;
        if(grow > 0)
        {
            for (int i = 0; i < grow; i++)
            {
                Data.Add(0);
            }
        }
    }
    private void WriteBytesUnchecked(byte[] bytes)
    {
        for (int idx = 0; idx < bytes.Length; idx++)
        {
            Data[idx + Head] = bytes[idx]; 
        }
        Head += bytes.Length;
        if(Head > Length) Length = Head;
    }
    private byte[] ReadBytesUnchecked(int size)
    {
        for (int idx = 0; idx < size; idx++)
        {
            Bytes[idx] = Data[Head];
            Head++;
        }
        return Bytes;
    }
    public void Reset()
    {
        Head = 0;
    }
    public void Clear()
    {
        Reset();
        Length = 0;
    }
    public int BytesRemaining()
    {
        return Length - Head;
    }
    public bool HasRemaining(int size)
    {
        return BytesRemaining() >= size;
    }
    public void WriteByte(byte value)
    {
        Reserve(1);
        Data[Head] = value;
        Head++;
        if(Head > Length) Length = Head;
    }
    public void WriteBytes(byte[] values)
    {
        Reserve(values.Length);
        WriteBytesUnchecked(values);
    }
    public void Extend(ErByteStream byteStream)
    {
        Reserve(byteStream.BytesRemaining());
        while(byteStream.TryReadByte(out byte b)) WriteByte(b);
    }
    public void Extend(ErByteStream byteStream, int length)
    {
        Reserve(length);
        for (int idx = 0; idx < length; idx++)
        {
            if(!byteStream.TryReadByte(out var b)) return;
            WriteByte(b);
        }
    }
    public void WriteBool(bool value)
    {
        WriteByte((byte)(value ? 1 : 0));
    }
    public void WriteBools(bool[] value)
    {
        Reserve(sizeof(bool) * value.Length);
        for (int idx = 0; idx < value.Length; idx++)
        {
            Data[Head] = (byte)(value[idx] ? 0 : 1);
            Head++;
        }
    }
    public void WriteI32(int value)
    {
        WriteBytes(BitConverter.GetBytes(value));
    }
    public void WriteI32s(in int[] value)
    {
        Reserve(sizeof(int) * value.Length);
        foreach (var item in value)
        {
            WriteBytesUnchecked(BitConverter.GetBytes(item));
        }
    }
    public void WriteI64(long value)
    {
        WriteBytes(BitConverter.GetBytes(value));
    }
    public void WriteI64s(in long[] value)
    {
        Reserve(sizeof(long) * value.Length);
        foreach (var item in value)
        {
            WriteBytesUnchecked(BitConverter.GetBytes(item));
        }
    }
    public void WriteU32(uint value)
    {
        WriteBytes(BitConverter.GetBytes(value));
    }
    public void WriteU32s(in uint[] value)
    {
        Reserve(sizeof(uint) * value.Length);
        foreach (var item in value)
        {
            WriteBytesUnchecked(BitConverter.GetBytes(item));
        }
    }
    public void WriteU64(ulong value)
    {
        WriteBytes(BitConverter.GetBytes(value));
    }
    public void WriteU64s(in ulong[] value)
    {
        Reserve(sizeof(ulong) * value.Length);
        foreach (var item in value)
        {
            WriteBytesUnchecked(BitConverter.GetBytes(item));
        }
    }
    public void WriteVec2I(ErVec2I value)
    {
        Reserve(sizeof(int) * 2);
        WriteBytesUnchecked(BitConverter.GetBytes(value.X));
        WriteBytesUnchecked(BitConverter.GetBytes(value.Y));
    }
    public void WriteVec2Is(in ErVec2I[] value)
    {
        Reserve(sizeof(int) * 2 * value.Length);
        for (int idx = 0; idx < value.Length; idx++)
        {
            WriteBytesUnchecked(BitConverter.GetBytes(value[idx].X));
            WriteBytesUnchecked(BitConverter.GetBytes(value[idx].Y));
        }
    }
    public void WriteRect2I(ErRect2I value)
    {
        Reserve(sizeof(int) * 4);
        WriteBytesUnchecked(BitConverter.GetBytes(value.Position.X));
        WriteBytesUnchecked(BitConverter.GetBytes(value.Position.Y));
        WriteBytesUnchecked(BitConverter.GetBytes(value.Size.X));
        WriteBytesUnchecked(BitConverter.GetBytes(value.Size.Y));
    }
    public void WriteRect2Is(in ErRect2I[] value)
    {
        Reserve(sizeof(int) * 4 * value.Length);
        for (int idx = 0; idx < value.Length; idx++)
        {
            WriteBytesUnchecked(BitConverter.GetBytes(value[idx].Position.X));
            WriteBytesUnchecked(BitConverter.GetBytes(value[idx].Position.Y));
            WriteBytesUnchecked(BitConverter.GetBytes(value[idx].Size.X));
            WriteBytesUnchecked(BitConverter.GetBytes(value[idx].Size.Y));        
        }
    }
    public void WriteF64(double value)
    {
        WriteBytes(BitConverter.GetBytes(value));
    }
    public void WriteF64s(in double[] value)
    {
        Reserve(sizeof(double) * value.Length);
        foreach (var item in value)
        {
            WriteBytesUnchecked(BitConverter.GetBytes(item));
        }
    }
    public void WriteVec2(ErVec2 value)
    {
        Reserve(sizeof(double) * 2);
        WriteBytesUnchecked(BitConverter.GetBytes(value.X));
        WriteBytesUnchecked(BitConverter.GetBytes(value.Y));
    }
    public void WriteVec2s(in ErVec2[] value)
    {
        Reserve(sizeof(double) * 2 * value.Length);
        for (int idx = 0; idx < value.Length; idx++)
        {
            WriteBytesUnchecked(BitConverter.GetBytes(value[idx].X));
            WriteBytesUnchecked(BitConverter.GetBytes(value[idx].Y));
        }
    }
    public void WriteRect2(ErRect2 value)
    {
        Reserve(sizeof(double) * 4);
        WriteBytesUnchecked(BitConverter.GetBytes(value.Position.X));
        WriteBytesUnchecked(BitConverter.GetBytes(value.Position.Y));
        WriteBytesUnchecked(BitConverter.GetBytes(value.Size.X));
        WriteBytesUnchecked(BitConverter.GetBytes(value.Size.Y));
    }
    public void WriteRect2s(ErRect2[] value)
    {
        Reserve(sizeof(double) * 4 * value.Length);
        for (int idx = 0; idx < value.Length; idx++)
        {
            WriteBytesUnchecked(BitConverter.GetBytes(value[idx].Position.X));
            WriteBytesUnchecked(BitConverter.GetBytes(value[idx].Position.Y));
            WriteBytesUnchecked(BitConverter.GetBytes(value[idx].Size.X));
            WriteBytesUnchecked(BitConverter.GetBytes(value[idx].Size.Y));        
        }
    }
    public bool TryPeekByte(out byte value)
    {
        value = default;
        if(BytesRemaining() < 1) return false;
        value = Data[Head];
        return true;
    }
    public bool TryReadByte(out byte value)
    {
        if(!TryPeekByte(out value)) return false;
        Head++;
        return true;
    }
    public bool TryReadBytes(int size, out byte[] value)
    {
        value = null!;
        if(BytesRemaining() < size) return false;
        for (int idx = 0; idx < size; idx++)
        {
            Bytes[idx] = Data[Head];
            Head++;
        }
        value = Bytes;
        return true;
    }
    public bool TryReadBytes(in byte[] value)
    {
        if(!HasRemaining(value.Length)) return false;
        for (int idx = 0; idx < value.Length; idx++)
        {
            value[idx] = Data[Head];
            Head++;
        }
        return true;
    }
    public bool TryReadBool(out bool value)
    {
        value = default;
        if(!TryPeekByte(out byte val)) return false;
        value = val != 0;
        Head++;
        return true;
    }
    public bool TryReadBools(in bool[] value)
    {
        if(!HasRemaining(value.Length)) return false;
        for (int idx = 0; idx < value.Length; idx++)
        {
            value[idx] = Data[Head] != 0;
            Head++;
        }
        return true;
    }
    public bool TryReadI32(out int value)
    {
        value = default;
        if(!TryReadBytes(sizeof(int), out var bytes)) return false;
        value = BitConverter.ToInt32(bytes);
        return true;
    }
    public bool TryReadI32s(in int[] value)
    {
        if(!HasRemaining(sizeof(int) * value.Length)) return false;
        for (int idx = 0; idx < value.Length; idx++)
        {
            value[idx] = BitConverter.ToInt32(ReadBytesUnchecked(sizeof(int)));
        }
        return true;
    }
    public bool TryReadI64(out long value)
    {
        value = default;
        if(!TryReadBytes(sizeof(long), out var bytes)) return false;
        value = BitConverter.ToInt64(bytes);
        return true;
    }
    public bool TryReadU32(out uint value)
    {
        value = default;
        if(!TryReadBytes(sizeof(uint), out var bytes)) return false;
        value = BitConverter.ToUInt32(bytes);
        return true;
    }
    public bool TryReadU32s(in uint[] value)
    {
        if(!HasRemaining(sizeof(uint) * value.Length)) return false;
        for (int idx = 0; idx < value.Length; idx++)
        {
            value[idx] = BitConverter.ToUInt32(ReadBytesUnchecked(sizeof(uint)));
        }
        return true;
    }
    public bool TryReadU64s(in ulong[] value)
    {
        if(!HasRemaining(sizeof(ulong) * value.Length)) return false;
        for (int idx = 0; idx < value.Length; idx++)
        {
            value[idx] = BitConverter.ToUInt64(ReadBytesUnchecked(sizeof(ulong)));
        }
        return true;
    }
    public bool TryReadVec2I(out ErVec2I value)
    {
        value = default;
        if(!HasRemaining(sizeof(int) * 2)) return false;
        int x = BitConverter.ToInt32(ReadBytesUnchecked(sizeof(int)));
        int y = BitConverter.ToInt32(ReadBytesUnchecked(sizeof(int)));
        value = new(x,y);
        return true;
    }
    public bool TryReadVec2Is(in ErVec2I[] value)
    {
        if(!HasRemaining(sizeof(int) * 2 * value.Length)) return false;
        for (int idx = 0; idx < value.Length; idx++)
        {
            int x = BitConverter.ToInt32(ReadBytesUnchecked(sizeof(int)));
            int y = BitConverter.ToInt32(ReadBytesUnchecked(sizeof(int)));
            value[idx] = new(x,y);
        }
        return true;
    }
    public bool TryReadRect2I(out ErRect2I value)
    {
        value = default;
        if(!HasRemaining(sizeof(int) * 4)) return false;
        int px = BitConverter.ToInt32(ReadBytesUnchecked(sizeof(int)));
        int py = BitConverter.ToInt32(ReadBytesUnchecked(sizeof(int)));
        int sx = BitConverter.ToInt32(ReadBytesUnchecked(sizeof(int)));
        int sy = BitConverter.ToInt32(ReadBytesUnchecked(sizeof(int)));
        value = new(px,py,sx,sy);
        return true;
    }
    public bool TryReadRect2Is(ref ErRect2I[] value)
    {
        if(!HasRemaining(sizeof(int) * 4 * value.Length)) return false;
        for (int idx = 0; idx < value.Length; idx++)
        {
            int px = BitConverter.ToInt32(ReadBytesUnchecked(sizeof(int)));
            int py = BitConverter.ToInt32(ReadBytesUnchecked(sizeof(int)));
            int sx = BitConverter.ToInt32(ReadBytesUnchecked(sizeof(int)));
            int sy = BitConverter.ToInt32(ReadBytesUnchecked(sizeof(int)));
            value[idx] = new(px,py,sx,sy);
        }
        return true;
    }
    public bool TryReadF64(out double value)
    {
        value = default;
        if(!TryReadBytes(sizeof(double), out var bytes)) return false;
        value = BitConverter.ToDouble(bytes);
        return true;
    }
    public bool TryReadF64s(in double[] value)
    {
        if(!HasRemaining(sizeof(double) * value.Length)) return false;
        for (int idx = 0; idx < value.Length; idx++)
        {
            value[idx] = BitConverter.ToDouble(ReadBytesUnchecked(sizeof(double)));
        }
        return true;
    }
    public bool TryReadVec2(out ErVec2 value)
    {
        value = default;
        if(!HasRemaining(sizeof(double) * 2)) return false;
        double x = BitConverter.ToDouble(ReadBytesUnchecked(sizeof(double)));
        double y = BitConverter.ToDouble(ReadBytesUnchecked(sizeof(double)));
        value = new(x,y);
        return true;
    }
    public bool TryReadVec2s(in ErVec2[] value)
    {
        if(!HasRemaining(sizeof(double) * 2 * value.Length)) return false;
        for (int idx = 0; idx < value.Length; idx++)
        {
            double x = BitConverter.ToDouble(ReadBytesUnchecked(sizeof(double)));
            double y = BitConverter.ToDouble(ReadBytesUnchecked(sizeof(double)));
            value[idx] = new(x,y);
        }
        return true;
    }
    public bool TryReadRect2(out ErRect2 value)
    {
        value = default;
        if(!HasRemaining(sizeof(double) * 4)) return false;
        double px = BitConverter.ToDouble(ReadBytesUnchecked(sizeof(double)));
        double py = BitConverter.ToDouble(ReadBytesUnchecked(sizeof(double)));
        double sx = BitConverter.ToDouble(ReadBytesUnchecked(sizeof(double)));
        double sy = BitConverter.ToDouble(ReadBytesUnchecked(sizeof(double)));
        value = new(px,py,sx,sy);
        return true;
    }
    public bool TryReadRect2s(ref ErRect2[] value)
    {
        if(!HasRemaining(sizeof(double) * 4 * value.Length)) return false;
        for (int idx = 0; idx < value.Length; idx++)
        {
            double px = BitConverter.ToDouble(ReadBytesUnchecked(sizeof(double)));
            double py = BitConverter.ToDouble(ReadBytesUnchecked(sizeof(double)));
            double sx = BitConverter.ToDouble(ReadBytesUnchecked(sizeof(double)));
            double sy = BitConverter.ToDouble(ReadBytesUnchecked(sizeof(double)));
            value[idx] = new(px,py,sx,sy);
        }
        return true;
    }
}
