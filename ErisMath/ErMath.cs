namespace ErisMath;

public static class ErMath
{
    public const double EPSILON = 0.001;
    public const double PI = Math.PI;
    public const double TAU = Math.PI * 2;
    public const double HALF_PI = Math.PI / 2;
    public static double DegToRad(double degrees)
    {
        return degrees / 360 * TAU;
    }
    public static double RadToDeg(double radians)
    {
        return radians / TAU * 360;
    }
    public static double Atan2(double y, double x)
    {
        return Math.Atan2(y, x);
    }
    public static int Mod(int x, int m) {
        return (x%m + m)%m;
    }
    public static int FloorToInt(double value){return (int)Math.Floor(value);}
    public static int CeilToInt(double value){return (int)Math.Ceiling(value);}
    public static int RoundToInt(double value){return (int)Math.Round(value);}
    public static int TruncToInt(double value){return (int)Math.Truncate(value);}
    public static double RoundAngle(double angle, int segments, double offset = 0)
    {
        double mul = segments / TAU;
        return Math.Round((angle + offset) * mul) / mul;
    }
    public static int RoundAngleToInt(double angle, int segments, double offset = 0)
    {
        double mul = segments / TAU;
        return Mod(RoundToInt((angle + offset) * mul), segments);
    }
    public static double Lerp(double start, double end, double weight)
    {
        return start + (end - start) * weight;
    }
    public static ErVec2 Lerp(ErVec2 start, ErVec2 end, double weight)
    {
        return start + (end - start) * weight;
    }
    public static ErVec3 Lerp(ErVec3 start, ErVec3 end, double weight)
    {
        return start + (end - start) * weight;
    }
    public static ErRect2 Lerp(ErRect2 start, ErRect2 end, double weight)
    {
        var sPos = start.Position;
        var ePos = end.Position;
        var sSize = start.Size;
        var eSize = end.Size;
        return new(Lerp(sPos, ePos, weight), Lerp(sSize, eSize, weight));
    }
    public static IEnumerable<int> Range(int start, int end, int step = 1)
    {
        if(end < start) step = -step;
        int idx = start;
        while (true)
        {
            if(idx < start && idx < end) yield break;
            if(idx > start && idx > end) yield break;
            yield return idx;
            idx += step;
        }
    }
    // public static double AbsAverage(IEnumerable<double> values)
    // {
    //     double max = double.MinValue;
    //     double min = double.MaxValue;
    //     foreach (var value in values)
    //     {
    //         if(value > max){max = value;}
    //         if(value < min){min = value;}
    //     }
    //     double maxAbs = Math.Abs(max);
    //     double minAbs = Math.Abs(min);
    //     double totalAbs = maxAbs - minAbs;
    // }
}