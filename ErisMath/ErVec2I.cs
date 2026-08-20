using System.Diagnostics.CodeAnalysis;

namespace ErisMath;

public readonly struct ErVec2I: IEquatable<ErVec2I>
{
    public int X{get; init;}
    public int Y{get; init;}
    public ErVec2I(){}
    public ErVec2I(int x, int y){X = x; Y = y;}
    public int GetArea()
    {
        return Math.Abs(X * Y);
    }
    public int GetManhattan()
    {
        return Math.Abs(X) + Math.Abs(Y);
    }
    public static readonly ErVec2I Zero = new(0, 0);
    public static readonly ErVec2I One = new(1, 1);
    public static readonly ErVec2I Neg = new(-1, -1);
    public static readonly ErVec2I Left = new(-1, 0);
    public static readonly ErVec2I Right = new(1, 0);
    public static readonly ErVec2I Up = new(0, -1);
    public static readonly ErVec2I Down = new(0, 1);
    public static readonly ErVec2I Max = new(int.MaxValue, int.MaxValue);
    public static readonly ErVec2I Min = new(int.MinValue, int.MinValue);
    public static ErVec2I operator +(ErVec2I left, ErVec2I right)=>new(left.X + right.X, left.Y + right.Y);
    public static ErVec2I operator -(ErVec2I left, ErVec2I right)=>new(left.X - right.X, left.Y - right.Y);
    public static ErVec2I operator *(ErVec2I left, ErVec2I right)=>new(left.X * right.X, left.Y * right.Y);
    public static ErVec2I operator /(ErVec2I left, ErVec2I right)=>new(ErMath.FloorToInt((double)left.X / right.X), ErMath.FloorToInt((double)left.Y / right.Y));
    public static ErVec2I operator *(ErVec2I left, int right)=>new(left.X * right, left.Y * right);
    public static ErVec2I operator /(ErVec2I left, int right)=>new(ErMath.FloorToInt((double)left.X / right), ErMath.FloorToInt((double)left.Y / right));
    public static bool operator == (ErVec2I left, ErVec2I right)=>left.X == right.X && left.Y == right.Y;
    public static bool operator != (ErVec2I left, ErVec2I right)=>left.X != right.X || left.Y != right.Y;
    public static explicit operator ErVec2I(ErVec2 value) => new(ErMath.RoundToInt(value.X), ErMath.RoundToInt(value.Y));
    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is ErVec2I vec && IsEqual(vec);
    }
    private bool IsEqual(ErVec2I vec)
    {
        return X == vec.X && Y == vec.Y;
    }
    public override string ToString()
    {
        return $"({X},{Y})";
    }
    public override int GetHashCode()
    {
        return ToString().GetHashCode();
    }

    public bool Equals(ErVec2I other)
    {
        return IsEqual(other);
    }
}