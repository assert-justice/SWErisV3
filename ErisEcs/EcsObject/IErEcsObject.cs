namespace ErisEcs.EcsObject;

public interface IErEcsObject
{
    public bool TryRead(ErByteStream byteStream);
    public void Write(ErByteStream byteStream);
}
