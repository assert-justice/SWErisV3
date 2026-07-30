namespace Eris.Utils;

public class ErWrapper<T>
{
    private T _Value = default!;
    private readonly Func<T> Factory;
    public ErWrapper(Func<T> factory)
    {
        Factory = factory;
    }
    public T Value
    {
        get
        {
            if (!HasValue)
            {
                HasValue = true;
                _Value = Factory();
            }
            return _Value;
        }
    }
    private bool HasValue = false;

}