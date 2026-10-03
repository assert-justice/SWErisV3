using ErisMath;

namespace Eris.Utils.Grid2D;

public class ErArrayGrid2D<T>(ErRect2I rect) : ErGrid2D<T>
{
    private readonly T[] Data = new T[rect.Area];
    public readonly ErRect2I Rect = rect;
    public override void Set(ErVec2I coord, T value)
    {
        if(!Rect.Contains(coord)) return;
        Data[GetCoordIdx(coord)] = value;
    }
    public override bool TryGet(ErVec2I coord, out T value)
    {
        value = default!;
        if(!Rect.Contains(coord)) return false;
        value = Data[GetCoordIdx(coord)];
        return true;
    }
    public void Fill(T value)
    {
        for (int idx = 0; idx < Data.Length; idx++)
        {
            Data[idx] = value;
        }
    }
    public void Fill(Func<T> factory)
    {
        for (int idx = 0; idx < Data.Length; idx++)
        {
            Data[idx] = factory();
        }
    }
    public void Fill(Func<ErVec2I, T> factory)
    {
        foreach (var coord in Rect.GetInnerCoords())
        {
            Data[GetCoordIdx(coord)] = factory(coord);
        }
    }
    private int GetCoordIdx(ErVec2I coord)
    {
        coord -= Rect.Position;
        return coord.X + coord.Y * Rect.Size.X;
    }
}
