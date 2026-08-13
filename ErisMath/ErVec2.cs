using System.Diagnostics.CodeAnalysis;
using SDL3;

namespace ErisMath;

public readonly struct ErVec2: IEquatable<ErVec2>
{
    public double X{get; init;}
    public double Y{get; init;}
    public ErVec2(){}
    public ErVec2(double x, double y){X = x; Y = y;}
    public static readonly ErVec2 Zero = new(0, 0);
    public static readonly ErVec2 One = new(1, 1);
    public static readonly ErVec2 Neg = new(-1, -1);
    public static readonly ErVec2 Left = new(-1, 0);
    public static readonly ErVec2 Right = new(1, 0);
    public static readonly ErVec2 Up = new(0, -1);
    public static readonly ErVec2 Down = new(0, 1);
    public static ErVec2 operator -(ErVec2 value)=>new(-value.X,-value.Y);
    public static ErVec2 operator +(ErVec2 left, ErVec2 right)=>new(left.X + right.X, left.Y + right.Y);
    public static ErVec2 operator -(ErVec2 left, ErVec2 right)=>new(left.X - right.X, left.Y - right.Y);
    public static ErVec2 operator *(ErVec2 left, ErVec2 right)=>new(left.X * right.X, left.Y * right.Y);
    public static ErVec2 operator /(ErVec2 left, ErVec2 right)=>new(left.X / right.X, left.Y / right.Y);
    public static ErVec2 operator *(ErVec2 left, double right)=>new(left.X * right, left.Y * right);
    public static ErVec2 operator /(ErVec2 left, double right)=>new(left.X / right, left.Y / right);
    public static bool operator == (ErVec2 left, ErVec2 right)=>left.X == right.X && left.Y == right.Y;
    public static bool operator != (ErVec2 left, ErVec2 right)=>left.X != right.X || left.Y != right.Y;
    public static explicit operator ErVec2(ErVec2I value) => new(value.X, value.Y);
    public static ErVec2 FromAngle(double angle)
    {
        double y = Math.Sin(angle);
        double x = Math.Cos(angle);
        return new(x,y);
    }
    public double GetArea()
    {
        return Math.Abs(X * Y);
    }
    public double GetLengthSquared()
    {
        return X * X + Y * Y;
    }
    public double GetLength()
    {
        return Math.Sqrt(GetLengthSquared());
    }
    public double GetManhattan()
    {
        return Math.Abs(X) + Math.Abs(Y);
    }
    public double GetAngle()
    {
        return ErMath.Atan2(Y, X);
    }
    public bool IsNonzero()
    {
        return GetLengthSquared() > ErMath.EPSILON;
    }
    public ErVec2 Normalized()
    {
        double lenSq = GetLengthSquared();
        if(lenSq > ErMath.EPSILON) return this / Math.Sqrt(lenSq);
        else return Zero;
    }
    public ErVec2I FloorToInt()
    {
        double x = Math.Floor(X);
        double y = Math.Floor(Y);
        return new((int)x,(int)y);
    }
    public ErVec2I CeilToInt()
    {
        double x = Math.Ceiling(X);
        double y = Math.Ceiling(Y);
        return new((int)x,(int)y);
    }
    public SDL.FPoint ToSdlPoint()
    {
        return new(){X = (float)X, Y = (float)Y};
    }
    private bool IsEqual(ErVec2 other)
    {
        return X == other.X && Y == other.Y;
    }
    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is ErVec2 vec && IsEqual(vec);
    }
    public override string ToString()
    {
        return $"({X},{Y})";
    }
    public override int GetHashCode()
    {
        return ToString().GetHashCode();
    }
    public bool Equals(ErVec2 other)
    {
        return IsEqual(other);
    }
}