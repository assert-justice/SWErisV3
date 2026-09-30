using ErisMath;

namespace Eris.Utils.Grid2D;

public abstract class ErGrid2D<T>
{
    public virtual T? Get(ErVec2I coord)
    {
        if(TryGet(coord, out var value)) return value;
        else return default;
    }
    public virtual T Get(ErVec2I coord, T defaultValue)
    {
        if(TryGet(coord, out var value)) return value;
        value = defaultValue;
        Set(coord, value);
        return value;
    }
    public virtual T Get(ErVec2I coord, Func<T> factory)
    {
        if(TryGet(coord, out var value)) return value;
        value = factory();
        Set(coord, value);
        return value;
    }
    public virtual T Get(ErVec2I coord, Func<ErVec2I,T> factory)
    {
        if(TryGet(coord, out var value)) return value;
        value = factory(coord);
        Set(coord, value);
        return value;
    }
    public abstract bool TryGet(ErVec2I coord, out T value);
    public abstract void Set(ErVec2I coord, T value);
}
