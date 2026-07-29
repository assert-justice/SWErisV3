using System.Diagnostics.CodeAnalysis;

namespace ErisMath;

public readonly struct ErVec3
{
    public double X{get; init;}
    public double Y{get; init;}
    public double Z{get; init;}
    public ErVec3(double x, double y, double z)
    {
        X = x; Y = y; Z = z;
    }
    public ErVec3(ErVec2 vec2)
    {
        X = vec2.X; Y = vec2.Y;
    }
    public static ErVec3 operator +(ErVec3 left, ErVec3 right)=>new(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
    public static ErVec3 operator -(ErVec3 left, ErVec3 right)=>new(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
    public static ErVec3 operator *(ErVec3 left, ErVec3 right)=>new(left.X * right.X, left.Y * right.Y, left.Z * right.Z);
    public static ErVec3 operator /(ErVec3 left, ErVec3 right)=>new(left.X / right.X, left.Y / right.Y, left.Z / right.Z);
    public static ErVec3 operator *(ErVec3 left, double right)=>new(left.X * right, left.Y * right, left.Z * right);
    public static ErVec3 operator /(ErVec3 left, double right)=>new(left.X / right, left.Y / right, left.Z * right);
    public static bool operator == (ErVec3 left, ErVec3 right)=>left.X == right.X && left.Y == right.Y;
    public static bool operator != (ErVec3 left, ErVec3 right)=>left.X != right.X || left.Y != right.Y;
    public static implicit operator ErVec3(ErVec3I value) => new(value.X, value.Y, value.Z);
    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is ErVec3 vec && X == vec.X && Y == vec.Y && Z == vec.Z;
    }
    public override string ToString()
    {
        return $"({X},{Y},{Z})";
    }
    public override int GetHashCode()
    {
        return ToString().GetHashCode();
    }
}