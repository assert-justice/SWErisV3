using System.Diagnostics.CodeAnalysis;
using ErisMath;
using Prion.Node;
using Prion.Validator;
using SDL3;

namespace Eris.Renderer;

public readonly struct ErColor: IPriSchema<ErColor>
{
    public byte R{get; init;}
    public byte G{get; init;}
    public byte B{get; init;}
    public byte A{get; init;}
    public ErColor(byte r, byte g, byte b, byte a = 255)
    {
        R = r; G = g; B = b; A = a;
    }
    public SDL.Color ToSdlColor()
    {
        return new(){R=R,G=G,B=B,A=A};
    }
    public SDL.FColor ToSdlFColor()
    {
        return new(){R=R/255.0f,G=G/255.0f,B=B/255.0f,A=A/255.0f};
    }
    public int ToInt()
    {
        int res = 0;
        res |= A;
        res |= B << 8;
        res |= G << 16;
        res |= R << 24;
        return res;
    }
    public static ErColor FromDoubles(double r, double g, double b, double a = 1)
    {
        return new()
        {
            R = (byte)ErMath.RoundToInt(r * 255),
            G = (byte)ErMath.RoundToInt(g * 255),
            B = (byte)ErMath.RoundToInt(b * 255),
            A = (byte)ErMath.RoundToInt(a * 255),
        };
    }
    public static int SdlColorToInt(SDL.Color color)
    {
        int res = 0;
        res |= color.A;
        res |= color.B << 8;
        res |= color.G << 16;
        res |= color.R << 24;
        return res;
    }
    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is ErColor color && R == color.R && G == color.G && B == color.B && A == color.A;
    }
    public override string ToString()
    {
        return $"Color({R},{G},{B},{A})";
    }
    public override int GetHashCode()
    {
        return ToString().GetHashCode();
    }
    public static bool TryFromPrion(PriNode priNode, out ErColor value)
    {
        value = default;
        if(!priNode.TryGet("r", out byte r)) return false;
        if(!priNode.TryGet("g", out byte g)) return false;
        if(!priNode.TryGet("b", out byte b)) return false;
        if(!priNode.TryGet("a", out byte a)) return false;
        value = new(r,g,b,a);
        return true;
    }
    // public static bool TryParse(string src, out ErColor value)
    // {
    //     value = default;
    //     src = src.Trim();
    //     if(src.StartsWith('#')) src = src[1..];
    //     else if(src.StartsWith("0x")) src = src[2..];
    //     if(src.Length != 6 && src.Length != 8) return false;
    //     if(!uint.TryParse(src, System.Globalization.NumberStyles.HexNumber | System.Globalization.NumberStyles.AllowHexSpecifier, null, out uint n)) return false;
    //     byte a;
    //     // if string has an alpha value read it
    //     if(src.Length == 8)
    //     {
    //         a = (byte)(n & 255);
    //         n <<= 8;
    //     }
    //     else a = 255;
    //     byte b = (byte)(n & 255);
    //     n <<= 8;
    //     byte g = (byte)(n & 255);
    //     n <<= 8;
    //     byte r = (byte)(n & 255);
    //     value = new(r,g,b,a);
    //     return true;
    // }
    public PriNode ToPrion()
    {
        PriDict dict = new();
        dict.TrySet("r", new PriNumber(R));
        dict.TrySet("g", new PriNumber(G));
        dict.TrySet("b", new PriNumber(B));
        dict.TrySet("a?", new PriNumber(A));
        return dict;
    }
    public static ErColor Black{get => new(0, 0, 0);}
    public static ErColor White{get => new(255, 255, 255);}
    public static ErColor Red{get => new(255, 0, 0);}
    public static ErColor Green{get => new(0, 255, 0);}
    public static ErColor Blue{get => new(0, 0, 255);}
    public static bool operator ==(ErColor left, ErColor right)
    {
        return left.Equals(right);
    }
    public static bool operator !=(ErColor left, ErColor right)
    {
        return !(left == right);
    }
}