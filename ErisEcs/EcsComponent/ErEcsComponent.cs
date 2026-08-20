namespace ErisEcs.EcsComponent;

public abstract class ErEcsBaseComponent
{
    public virtual bool IsDrawable => false;
    public virtual void RegisterComponent(){}
    public virtual void Ready(){}
    public virtual void Update(){}
    public virtual void Draw(ErEcsBaseComponent nextTickComponent){}
    public abstract bool TryRead(ErByteStream byteStream);
    public abstract void Write(ErByteStream byteStream);
}

public abstract class ErEcsComponent<T>(T entity) : ErEcsBaseComponent where T: ErEcsEntity
{
    private readonly T Entity = entity;
}
