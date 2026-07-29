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
        // PriValidData dict = new("ErColor", priNode);
        if(priNode is not PriDict dict) return false;
        if(!dict.TryGet("r", out byte r)) return false;
        if(!dict.TryGet("g", out byte g)) return false;
        if(!dict.TryGet("b", out byte b)) return false;
        if(!dict.TryGet("a", out byte a)) return false;
        // dict.TryGet("g", out byte g);
        // dict.TryGet("b", out byte b);
        // dict.TryGet("a", out byte a);
        // if(dict.HasError) return dict.GetError(out error);
        // ErEngine.Logger.BeginLog("Unable to parse eris color, ");
        // if(priNode is not PriDict dict) return ErEngine.Logger.CommitError("not a dictionary");
        // if(!dict.TryGet("r", out byte r)) return ErEngine.Logger.CommitError("missing r field");
        // if(!dict.TryGet("g", out byte g)) return ErEngine.Logger.CommitError("missing g field");
        // if(!dict.TryGet("b", out byte b)) return ErEngine.Logger.CommitError("missing b field");
        // if(!dict.TryGet("a?", out byte a)) a = 255;
        value = new(r,g,b,a);
        return true;
    }

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