using ErisMath;

namespace Eris.Utils.Grid2D;

public class ErHashGrid2D<T> : ErGrid2D<T>
{
    private readonly Dictionary<ErVec2I,T> Data = [];
    private T? LastValue;
    private bool HasLastValue = false;
    private ErVec2I LastCoord;
    public IEnumerable<(ErVec2I,T)> Entries
    {
        get
        {
            foreach (var (coord,value) in Data)
            {
                yield return (coord, value);
            }
        }
    }
    public IEnumerable<ErVec2I> Coords => Data.Keys;
    public IEnumerable<T> Values => Data.Values;
    public override T? Get(ErVec2I coord)
    {
        if(HasLastValue && coord == LastCoord) return LastValue;
        if(!Data.TryGetValue(coord, out T? value)) return default;
        HasLastValue = true;
        LastValue = value;
        LastCoord = coord;
        return value;
    }
    public override bool TryGet(ErVec2I coord, out T value)
    {
        value = default!;
        if(!Data.TryGetValue(coord, out var val)) return false;
        value = val;
        return true;
    }
    public override void Set(ErVec2I coord, T value)
    {
        Data[coord] = value;
    }
    public virtual bool Remove(ErVec2I coord)
    {
        return Data.Remove(coord);
    }
    public virtual bool ContainsCoord(ErVec2I coord)
    {
        return Data.ContainsKey(coord);
    }
}
