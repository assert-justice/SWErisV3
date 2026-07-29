using Eris;
using ErisMath;

namespace SpoonWitch.ByteStream;

public class SwByteStream
{
    private readonly List<byte> Data = [];
    private readonly byte[] Bytes = new byte[8];
    public int Head{get; private set;}
    public int Length{get; private set;}
    public void SetHead(int head){Head = head;}
    public void Reserve(int size)
    {
        // int grow = Head + size - Data.Count;
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
    public void Extend(SwByteStream byteStream)
    {
        Reserve(byteStream.BytesRemaining());
        while(byteStream.TryReadByte(out byte b)) WriteByte(b);
    }
    public void Extend(SwByteStream byteStream, int length)
    {
        Reserve(length);
        for (int idx = 0; idx < length; idx++)
        {
            if(!byteStream.TryReadByte(out var b)) return;
            WriteByte(b);
        }
        // while(byteStream.TryReadByte(out byte b)) WriteByte(b);
    }
    public void WriteBool(bool value)
    {
        WriteByte((byte)(value ? 1 : 0));
    }
    public void WriteBools(bool[] value)
    {
        Reserve(value.Length);
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
    public void WriteI32s(int[] value)
    {
        Reserve(sizeof(int));
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
    public void WriteVec2Is(ErVec2I[] value)
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
    public void WriteRect2Is(ErRect2I[] value)
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
    public void WriteF64s(double[] value)
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
    public void WriteVec2s(ErVec2[] value)
    {
        Reserve(sizeof(double) * 2 * value.Length);
        for (int idx = 0; idx < value.Length; idx++)
        {
            WriteBytesUnchecked(BitConverter.GetBytes(value[idx].X));
            WriteBytesUnchecked(BitConverter.GetBytes(value[idx].Y));
        }
    }
    // public void WriteRect2(ErRect2 value)
    // {
    //     Reserve(sizeof(double) * 4);
    //     WriteBytesUnchecked(BitConverter.GetBytes(value.Position.X));
    //     WriteBytesUnchecked(BitConverter.GetBytes(value.Position.Y));
    //     WriteBytesUnchecked(BitConverter.GetBytes(value.Size.X));
    //     WriteBytesUnchecked(BitConverter.GetBytes(value.Size.Y));
    // }
    // public void WriteRect2s(ErRect2[] value)
    // {
    //     Reserve(sizeof(double) * 4 * value.Length);
    //     for (int idx = 0; idx < value.Length; idx++)
    //     {
    //         WriteBytesUnchecked(BitConverter.GetBytes(value[idx].Position.X));
    //         WriteBytesUnchecked(BitConverter.GetBytes(value[idx].Position.Y));
    //         WriteBytesUnchecked(BitConverter.GetBytes(value[idx].Size.X));
    //         WriteBytesUnchecked(BitConverter.GetBytes(value[idx].Size.Y));        
    //     }
    // }
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
    public bool TryReadBytes(ref byte[] value)
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
    public bool TryReadBools(ref bool[] value)
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
    public bool TryReadI32s(ref int[] value)
    {
        value = default!;
        if(!HasRemaining(sizeof(int) * value.Length)) return false;
        for (int idx = 0; idx < value.Length; idx++)
        {
            value[idx] = BitConverter.ToInt32(ReadBytesUnchecked(sizeof(int)));
        }
        return true;
    }
    public bool TryReadVec2I(ref ErVec2I value)
    {
        if(!HasRemaining(sizeof(int) * 2)) return false;
        int x = BitConverter.ToInt32(ReadBytesUnchecked(sizeof(int)));
        int y = BitConverter.ToInt32(ReadBytesUnchecked(sizeof(int)));
        value = new(x,y);
        return true;
    }
    public bool TryReadVec2Is(ref ErVec2I[] value)
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
    // public bool TryReadRect2I(ref ErRect2I value)
    // {
    //     if(!HasRemaining(sizeof(int) * 4)) return false;
    //     int px = BitConverter.ToInt32(ReadBytesUnchecked(sizeof(int)));
    //     int py = BitConverter.ToInt32(ReadBytesUnchecked(sizeof(int)));
    //     int sx = BitConverter.ToInt32(ReadBytesUnchecked(sizeof(int)));
    //     int sy = BitConverter.ToInt32(ReadBytesUnchecked(sizeof(int)));
    //     value = new(px,py,sx,sy);
    //     return true;
    // }
    // public bool TryReadRect2Is(ref ErRect2I[] value)
    // {
    //     if(!HasRemaining(sizeof(int) * 4 * value.Length)) return false;
    //     for (int idx = 0; idx < value.Length; idx++)
    //     {
    //         int px = BitConverter.ToInt32(ReadBytesUnchecked(sizeof(int)));
    //         int py = BitConverter.ToInt32(ReadBytesUnchecked(sizeof(int)));
    //         int sx = BitConverter.ToInt32(ReadBytesUnchecked(sizeof(int)));
    //         int sy = BitConverter.ToInt32(ReadBytesUnchecked(sizeof(int)));
    //         value[idx] = new(px,py,sx,sy);
    //     }
    //     return true;
    // }
    public bool TryReadF64(out double value)
    {
        value = default;
        if(!TryReadBytes(sizeof(double), out var bytes)) return false;
        value = BitConverter.ToDouble(bytes);
        return true;
    }
    public bool TryReadF64s(ref double[] value)
    {
        value = [];
        if(!HasRemaining(sizeof(double) * value.Length)) return false;
        value = new double[value.Length];
        for (int idx = 0; idx < value.Length; idx++)
        {
            value[idx] = BitConverter.ToDouble(ReadBytesUnchecked(sizeof(int)));
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
    public bool TryReadVec2s(ref ErVec2[] value)
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
    // public bool TryReadRect2I(out ErRect2 value)
    // {
    //     value = default;
    //     if(!HasRemaining(sizeof(double) * 4)) return false;
    //     double px = BitConverter.ToDouble(ReadBytesUnchecked(sizeof(double)));
    //     double py = BitConverter.ToDouble(ReadBytesUnchecked(sizeof(double)));
    //     double sx = BitConverter.ToDouble(ReadBytesUnchecked(sizeof(double)));
    //     double sy = BitConverter.ToDouble(ReadBytesUnchecked(sizeof(double)));
    //     value = new(px,py,sx,sy);
    //     return true;
    // }
    // public bool TryReadRect2Is(ref ErRect2[] value)
    // {
    //     if(!HasRemaining(sizeof(double) * 4 * value.Length)) return false;
    //     for (int idx = 0; idx < value.Length; idx++)
    //     {
    //         double px = BitConverter.ToDouble(ReadBytesUnchecked(sizeof(double)));
    //         double py = BitConverter.ToDouble(ReadBytesUnchecked(sizeof(double)));
    //         double sx = BitConverter.ToDouble(ReadBytesUnchecked(sizeof(double)));
    //         double sy = BitConverter.ToDouble(ReadBytesUnchecked(sizeof(double)));
    //         value[idx] = new(px,py,sx,sy);
    //     }
    //     return true;
    // }
}

// public class ErisDataBuffer
// {
//     // public enum BufferMode
//     // {
//     //     None = 0,
//     //     Read = 1,
//     //     Write = 2,
//     //     ReadWrite = 3,
//     // }
//     private readonly List<byte> Data = [];
//     public int Head{get; private set;}
//     public void SetHead(int head){Head = head;}
//     // public BufferMode Mode = BufferMode.ReadWrite;
//     // private void SetMode(BufferMode mode){Mode = mode;}
//     private bool TryGetSpan(int size, out ReadOnlySpan<byte> bytes)
//     {
//         bytes = default;
//         if(BytesRemaining() < size) return false;
//         bytes = CollectionsMarshal.AsSpan(Data[Head..(Head+size)]);
//         Head += size;
//         return true;
//     }
//     private void Reserve(int size)
//     {
//         if(Data.Capacity < Head + size) Data.Capacity = Head + size;
//     }
//     private void WriteBytesUnchecked(byte[] bytes)
//     {
//         for (int idx = 0; idx < bytes.Length; idx++)
//         {
//             Data[idx + Head] = bytes[idx]; 
//         }
//         Head += bytes.Length;
//     }
//     // public bool CanRead()
//     // {
//     //     return (Mode & BufferMode.Read) == BufferMode.Read;
//     // }
//     // public bool CanWrite()
//     // {
//     //     return (Mode & BufferMode.Read) == BufferMode.Read;
//     // }
//     public void Reset()
//     {
//         Head = 0;
//     }
//     public void Clear()
//     {
//         Data.Clear();
//         Reset();
//     }
//     public void Trim()
//     {
//         Data.Capacity = Head;
//     }
//     public nint BytesRemaining()
//     {
//         return Data.Count - Head;
//     }
//     public void WriteByte(byte value)
//     {
//         Reserve(1);
//         Data[Head] = value;
//         Head++;
//     }
//     public void WriteBytes(byte[] values)
//     {
//         Reserve(values.Length);
//         WriteBytesUnchecked(values);
//     }
//     public bool TryPeekByte(out byte value)
//     {
//         value = default;
//         if(BytesRemaining() < 1) return false;
//         value = Data[Head];
//         return true;
//     }
//     public bool TryReadByte(out byte value)
//     {
//         if(!TryPeekByte(out value)) return false;
//         Head++;
//         return true;
//     }
//     public void WriteI32(int value)
//     {
//         WriteBytes(BitConverter.GetBytes(value));
//     }
//     public void WriteI32s(int[] value)
//     {
//         Reserve(value.Length * 4);
//         foreach (var item in value)
//         {
//             WriteBytesUnchecked(BitConverter.GetBytes(item));
//         }
//     }
//     public bool TryReadI32(out int value)
//     {
//         value = default;
//         if(!TryGetSpan(4, out var bytes)) return false;
//         value = BitConverter.ToInt32(bytes);
//         return true;
//     }
//     public void WriteI64(long value)
//     {
//         WriteBytes(BitConverter.GetBytes(value));
//     }
//     public void WriteI64s(long[] value)
//     {
//         Reserve(value.Length * 8);
//         foreach (var item in value)
//         {
//             WriteBytesUnchecked(BitConverter.GetBytes(item));
//         }
//     }
//     public bool TryReadI64(out long value)
//     {
//         value = default;
//         if(!TryGetSpan(8, out var bytes)) return false;
//         value = BitConverter.ToInt64(bytes);
//         return true;
//     }
//     public void WriteF32(float value)
//     {
//         WriteBytes(BitConverter.GetBytes(value));
//     }
//     public void WriteF32s(float[] value)
//     {
//         Reserve(value.Length * 4);
//         foreach (var item in value)
//         {
//             WriteBytesUnchecked(BitConverter.GetBytes(item));
//         }
//     }
//     public bool TryReadF32(out float value)
//     {
//         value = default;
//         if(!TryGetSpan(4, out var bytes)) return false;
//         value = BitConverter.ToSingle(bytes);
//         return true;
//     }
//     public void WriteF64(double value)
//     {
//         WriteBytes(BitConverter.GetBytes(value));
//     }
//     public void WriteF64s(double[] values)
//     {
//         Reserve(values.Length * 8);
//         foreach (var value in values)
//         {
//             WriteBytesUnchecked(BitConverter.GetBytes(value));
//         }
//     }
//     public bool TryReadF64(out double value)
//     {
//         value = default;
//         if(!TryGetSpan(8, out var bytes)) return false;
//         value = BitConverter.ToDouble(bytes);
//         return true;
//     }
//     public bool TryReadF64s(int length, out double[] value)
//     {
//         value = [];
//         if(BytesRemaining() < length * 8) return false;
//         value = new double[length];
//         for (int idx = 0; idx < length; idx++)
//         {
//             var bytes = CollectionsMarshal.AsSpan(Data[(Head+8*idx)..(Head+8*idx+8)]);
//             value[idx] = BitConverter.ToDouble(bytes);
//         }
//         return true;
//     }
//     public bool TryReadTwoF64(out double a, out double b)
//     {
//         a = default;
//         b = default;
//         if(BytesRemaining() < 16) return false;
//         var bytes = CollectionsMarshal.AsSpan(Data[Head..(Head+8)]);
//         a = BitConverter.ToDouble(bytes);
//         bytes = CollectionsMarshal.AsSpan(Data[(Head+8)..(Head+16)]);
//         b = BitConverter.ToDouble(bytes);
//         Head+=16;
//         return true;
//     }
//     public void WriteVec2(ErisVec2 value)
//     {
//         Reserve(16);
//         WriteBytesUnchecked(BitConverter.GetBytes(value.X));
//         WriteBytesUnchecked(BitConverter.GetBytes(value.Y));
//     }
//     public void WriteVec2s(ErisVec2[] value)
//     {
//         Reserve(value.Length * 16);
//         foreach (var item in value)
//         {
//             WriteBytesUnchecked(BitConverter.GetBytes(item.X));
//             WriteBytesUnchecked(BitConverter.GetBytes(item.Y));
//         }
//     }
//     public bool TryReadVec2(out ErisVec2 value)
//     {
//         value = default;
//         if(!TryReadTwoF64(out double x, out double y)) return false;
//         value = new(x,y);
//         return true;
//     }
//     public bool TryReadVec2s(int length, out ErisVec2[] value)
//     {
//         value = [];
//         if(BytesRemaining() < length * 16) return false;
//         value = new ErisVec2[length];
//         if(!TryReadF64s(length * 2, out var doubles)) return false;
//         for (int idx = 0; idx < length; idx++)
//         {
//             value[idx] = new(doubles[idx*2], doubles[idx*2+1]);
//         }
//         return true;
//     }
//     public bool TryReadTwoVec2(out ErisVec2 a, out ErisVec2 b)
//     {
//         a = default;
//         b = default;
//         if(!TryReadF64s(4, out var value)) return false;
//         a = new(value[0],value[1]);
//         b = new(value[2],value[3]);
//         return true;
//     }
//     public void WriteRect2(ErisRect2 value)
//     {
//         Reserve(32);
//         WriteBytesUnchecked(BitConverter.GetBytes(value.Position.X));
//         WriteBytesUnchecked(BitConverter.GetBytes(value.Position.Y));
//         WriteBytesUnchecked(BitConverter.GetBytes(value.Size.X));
//         WriteBytesUnchecked(BitConverter.GetBytes(value.Size.Y));
//     }
//     public void WriteRect2s(ErisRect2[] value)
//     {
//         Reserve(value.Length * 32);
//         foreach (var item in value)
//         {
//             WriteBytesUnchecked(BitConverter.GetBytes(item.Position.X));
//             WriteBytesUnchecked(BitConverter.GetBytes(item.Position.Y));
//             WriteBytesUnchecked(BitConverter.GetBytes(item.Size.X));
//             WriteBytesUnchecked(BitConverter.GetBytes(item.Size.Y));
//         }
//     }
//     public bool TryReadRect2(out ErisRect2 value)
//     {
//         value = default;
//         if(!TryReadTwoVec2(out var pos, out var size)) return false;
//         value = new(pos, size);
//         return true;
//     }
//     public bool TryReadRect2s(int length, out ErisRect2[] value)
//     {
//         value = [];
//         if(BytesRemaining() < length * 32) return false;
//         value = new ErisRect2[length];
//         if(!TryReadF64s(length * 4, out var doubles)) return false;
//         for (int idx = 0; idx < length; idx++)
//         {
//             value[idx] = new(doubles[idx*2], doubles[idx*2+1],  doubles[idx*2+2],  doubles[idx*2+3]);
//         }
//         return true;
//     }
//     public bool TryReadTwoRect2(out ErisRect2 a, out ErisRect2 b)
//     {
//         a = default;
//         b = default;
//         if(!TryReadF64s(8, out var value)) return false;
//         a = new(value[0],value[1],value[2],value[3]);
//         b = new(value[4],value[5],value[6],value[7]);
//         return true;
//     }
//     // public static ErisDataBuffer NewBuffer(out Action<BufferMode> setMode)
//     // {
//     //     ErisDataBuffer buffer = new();
//     //     setMode = buffer.SetMode;
//     //     return buffer;
//     // }
// }