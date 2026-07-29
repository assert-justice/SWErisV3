using System.Diagnostics.CodeAnalysis;

namespace ErisMath;

public readonly struct ErVec3I
{
    public int X{get; init;}
    public int Y{get; init;}
    public int Z{get; init;}
    public ErVec3I(int x, int y, int z)
    {
        X = x; Y = y; Z = z;
    }
    public ErVec3I(ErVec2I vec2I)
    {
        X = vec2I.X; Y = vec2I.Y;
    }
    public static ErVec3I operator +(ErVec3I left, ErVec3I right)=>new(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
    public static ErVec3I operator -(ErVec3I left, ErVec3I right)=>new(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
    public static ErVec3I operator *(ErVec3I left, ErVec3I right)=>new(left.X * right.X, left.Y * right.Y, left.Z * right.Z);
    public static ErVec3I operator /(ErVec3I left, ErVec3I right)=>new(ErMath.FloorToInt((double)left.X / right.X), ErMath.FloorToInt((double)left.Y / right.Y), ErMath.FloorToInt((double)left.Z / right.Z));
    public static ErVec3I operator *(ErVec3I left, int right)=>new(left.X * right, left.Y * right, left.Z * right);
    public static ErVec3I operator /(ErVec3I left, int right)=>new(ErMath.FloorToInt((double)left.X / right), ErMath.FloorToInt((double)left.Y / right), ErMath.FloorToInt((double)left.Z * right));
    public static bool operator == (ErVec3I left, ErVec3I right)=>left.X == right.X && left.Y == right.Y;
    public static bool operator != (ErVec3I left, ErVec3I right)=>left.X != right.X || left.Y != right.Y;
    public static explicit operator ErVec3I(ErVec3 value) => new(ErMath.RoundToInt(value.X), ErMath.RoundToInt(value.Y), ErMath.RoundToInt(value.Z));
    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is ErVec3I vec && X == vec.X && Y == vec.Y && Z == vec.Z;
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