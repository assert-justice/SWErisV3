using ErisMath;

namespace SpoonWitch.Utils;

public static class SwRandom
{
    public static ErVec2 GetRandomPointOnCircle(ErVec2 center, double radius)
    {
        double angle = Random.Shared.NextDouble();
        return ErVec2.FromAngle(angle * ErMath.TAU) * radius + center;
    }
    public static ErVec2 GetRandomPointInRect(ErRect2 rect)
    {
        double x = Random.Shared.NextDouble();
        double y = Random.Shared.NextDouble();
        x = ErMath.Lerp(rect.Left,rect.Right,x);
        y = ErMath.Lerp(rect.Top,rect.Bottom,y);
        return new(x,y);
    }
}
