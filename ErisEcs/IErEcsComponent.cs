namespace ErisEcs;

public abstract class ErEcsComponent
{
    public abstract int SizeBytes{get;}
    private bool WasConstructorCalled = false;
    public void Init()
    {
        WasConstructorCalled = true;
        InitImpl();
    }
    protected virtual void InitImpl(){}
    public virtual void Update(){}
    public virtual void Draw(){}
    public abstract bool TryRead(ErByteStream byteStream);
    public abstract void Write(ErByteStream byteStream);
}