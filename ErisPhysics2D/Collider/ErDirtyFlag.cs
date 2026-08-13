namespace ErisPhysics2D.Collider;

internal class ErDirtyFlag<T> where T: IEquatable<T>
{
    public T Value
    {
        get => _Value;
        set
        {
            if(!_Value.Equals(value))
            {
                IsDirty = true;
                _Value = value;
            }
        }
    }
    public bool IsDirty{get; private set;}
    private T _Value;
    public ErDirtyFlag(T value, bool isDirty = false)
    {
        _Value = value;
        IsDirty = isDirty;
    }
    public void Clean()
    {
        IsDirty = false;
    }
}