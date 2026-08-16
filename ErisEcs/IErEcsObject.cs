namespace ErisEcs;

public interface IErEcsObject
{
    //
    public abstract int SizeBytes{get;}
    public bool TryRead(ErByteStream byteStream);
    public void Write(ErByteStream byteStream);
}
// public abstract class ErEcsObject
// {
//     public abstract int SizeBytes{get;}
//     public bool IsFreeQueued{get; private set;}
//     public bool IsInitialized{get; private set;}
//     public void Init()
//     {
//         InitImpl();
//     }
//     protected virtual void InitImpl(){}
//     public bool TryRead(ErByteStream byteStream)
//     {
//         return TryReadImpl(byteStream);
//     }
//     protected virtual bool TryReadImpl(ErByteStream byteStream)
//     {
//         return true;
//     }
//     public void Write(ErByteStream byteStream)
//     {
//         if(!IsFreeQueued) return;
//         WriteImpl(byteStream);
//     }
//     protected virtual void WriteImpl(ErByteStream byteStream){}
// }
