using System.Text;
using Prion.Node;

namespace Prion.Parser;

internal class PriParserInternal
{
    private readonly PriScanner Scanner = new();
    private readonly StringBuilder Sb = new();
    private PriError? Error;
    private bool SetError(string message, out PriNode priNode)
    {
        Error = Scanner.GetError(message);
        priNode = Error;
        // throw new(message);
        return false;
    }
    private bool SetError(string message, int charIndex, out PriNode priNode)
    {
        Error = Scanner.GetError(message, charIndex);
        priNode = Error;
        return false;
    }
    private bool MatchSymbol(char c)
    {
        Scanner.Whitespace();
        return Scanner.Match(c);
    }
    private bool TryParseString(out PriNode priNode)
    {
        // Note: opening quote already consumed
        int start = Scanner.Current;
        Sb.Clear();
        while(Scanner.TryNext(out char c))
        {
            // Todo: handle escape characters
            if(c=='\\'){}
            if(c == '"')
            {
                priNode = new PriString(Sb.ToString());
                return true;
            }
            Sb.Append(c);
        }
        return SetError("Unexpected eof while parsing string", start, out priNode);
    }
    private bool TryParseNumber(out PriNode priNode, bool negate = false)
    {
        priNode = PriNull.Null;
        Scanner.Whitespace();
        int start = Scanner.Current;
        if(!Scanner.TryNext(out char c)) return false;
        if(!char.IsAsciiDigit(c)) return false;
        PriNumber.NumberRadix radix = PriNumber.NumberRadix.Decimal;
        int size = 0;
        bool hasDecimal = false;
        PriNumber.NumberMode mode = PriNumber.NumberMode.SignedInt;
        Sb.Clear();
        // Handle radix prefixes
        if(c == '0' && Scanner.MatchFirst(['x','b','o'], out char radixChar))
        {
            switch (radixChar)
            {
                case 'x':
                    radix = PriNumber.NumberRadix.Hex;
                    break;
                case 'b':
                    radix = PriNumber.NumberRadix.Binary;
                    break;
                default:
                    return SetError("Octal not yet supported", out priNode);
            }
        }
        else Sb.Append(c);
        while(Scanner.TryPeek(out c))
        {
            if (char.IsAsciiDigit(c))
            {
                Sb.Append(c);
                Scanner.Advance();
                continue;
            }
            if(c == '_')
            {
                Scanner.Advance();
                continue;
            }
            if(c == '.')
            {
                if(hasDecimal) return SetError("Only at most one decimal point allowed in a number literal.", out priNode);
                if(radix != PriNumber.NumberRadix.Decimal) return SetError("Invalid number literal, found a decimal point in a number with an non decimal radix.", start, out priNode);
                hasDecimal = true;
                mode = PriNumber.NumberMode.Float;
                Sb.Append(c);
                Scanner.Advance();
                if(!Scanner.TryPeek(out c) || !char.IsAsciiDigit(c)) Sb.Append('0');
                continue;
            }
            if(!Scanner.MatchFirst(['i','u','f'], out char modeChar))break;
            switch (modeChar)
            {
                case 'i':
                    mode = PriNumber.NumberMode.SignedInt;
                    break;
                case 'u':
                    mode = PriNumber.NumberMode.UnsignedInt;
                    break;
                case 'f':
                    mode = PriNumber.NumberMode.Float;
                    break;
                default:
                    break;
            }
            if(Scanner.MatchFirst(["64","32","16","8"], out string sizeStr))
            {
                size = int.Parse(sizeStr);
            }
            break;
        }
        string literal = Sb.ToString();
        if(hasDecimal && mode != PriNumber.NumberMode.Float) return SetError("Invalid number literal, found a decimal point in a number with an integer suffix.", start, out priNode);
        if(size == 0) size = mode == PriNumber.NumberMode.Float ? 64 : 32;
        if(mode == PriNumber.NumberMode.Float && size != 64 && size != 32)
        {
            return SetError($"Float size '{size}' is not supported", out priNode);
        }
        if (negate && mode == PriNumber.NumberMode.UnsignedInt) return SetError("Attempted to negate an unsigned integer", start, out priNode);
        // {
        //     if(mode == PriNumber.NumberMode.UnsignedInt) return SetError("Attempted to negate an unsigned integer", start, out priNode);
        //     // value = -value;
        // }
        // decimal value = 0;
        // byte[] data = [];
        System.Globalization.NumberStyles style = System.Globalization.NumberStyles.Integer;
        switch (radix)
        {
            case PriNumber.NumberRadix.Hex:
                style = System.Globalization.NumberStyles.HexNumber;
                break;
            case PriNumber.NumberRadix.Binary:
                style = System.Globalization.NumberStyles.BinaryNumber;
                break;
            default:
                break;
        }
        switch (mode)
        {
            case PriNumber.NumberMode.Float:
                if(!double.TryParse(literal, out double d)) return SetError($"Failed to parse float '{literal}'", start, out priNode);
                switch (size)
                {
                    case 32:
                        priNode = new PriNumber((float)d);
                    break;
                    case 64:
                        priNode = new PriNumber(d);
                    break;
                    default:
                        return SetError($"Float size '{size}' is not supported", out priNode);
                }
                // value = (decimal)d;
                break;
            case PriNumber.NumberMode.SignedInt:
                if(!long.TryParse(literal, style, null, out long l)) return SetError("Failed to parse integer", start, out priNode);
                switch (size)
                {
                    case 8:
                        priNode = new PriNumber((sbyte)l, radix);
                    break;
                    case 16:
                        priNode = new PriNumber((short)l, radix);
                    break;
                    case 32:
                        priNode = new PriNumber((int)l, radix);
                    break;
                    case 64:
                        priNode = new PriNumber(l, radix);
                    break;
                }
                // value = l;
                break;
            case PriNumber.NumberMode.UnsignedInt:
                if(!ulong.TryParse(literal, style, null, out ulong ul)) return SetError("Failed to parse unsigned integer", start, out priNode);
                switch (size)
                {
                    case 8:
                        priNode = new PriNumber((byte)ul, radix);
                    break;
                    case 16:
                        priNode = new PriNumber((ushort)ul, radix);
                    break;
                    case 32:
                        priNode = new PriNumber((uint)ul, radix);
                    break;
                    case 64:
                        priNode = new PriNumber(ul, radix);
                    break;
                }
                // value = ul;
                break;
            default:
                break;
        }
        // Console.WriteLine($"literal: {literal}, value: {value}, mode: {mode}, size: {size}, radix: {radix}");
        // priNode = new PriNumber(data, mode, radix);
        return true;
    }
    private bool TryParseIdent(out PriNode priNode)
    {
        priNode = PriNull.Null;
        Scanner.Whitespace();
        int start = Scanner.Current;
        if(!Scanner.TryPeek(out char c)) return SetError("Expected identifier, found eof", out priNode);
        Sb.Clear();
        // Handle ascii identifiers. Note this will also parse keywords like true, false, and null.
        if (char.IsAsciiLetter(c) || c == '_')
        {
            while(Scanner.TryPeek(out c))
            {
                if(char.IsAsciiLetter(c) || char.IsAsciiDigit(c) || c == '_')
                {
                    Sb.Append(c);
                    Scanner.Advance();
                }
                else break;
            }
        }
        // Handle all other key types
        else
        {
            if(!TryParseValue(out priNode)) return false;
            if(!priNode.IsImmutable) return SetError("Identifiers must be immutable", start, out priNode);
            Sb.Append(priNode.ToString());
        }
        // Handle special key suffixes. 
        // A key that ends in a question mark (?) is nullable
        // A key that ends in a hash is not accessible but should be retained
        if(Scanner.MatchFirst(['?','#'], out char suffix)) Sb.Append(suffix);
        // Note: this is a variant and not a pri string because we don't want it to be quoted when displayed.
        priNode = new PriVariant<string>(Sb.ToString());
        return true;
    }
    private bool TryParseList(out PriNode priNode)
    {
        // Note: opening bracket already consumed
        priNode = PriNull.Null;
        List<PriNode> nodes = [];
        bool finished = false;
        while (!Scanner.AtEof())
        {
            int start = Scanner.Current;
            if(MatchSymbol(']')) {finished = true; break;}
            if(!TryParseValue(out var valueNode)) return false;
            nodes.Add(valueNode);
            if(Scanner.Current == start) return SetError("Internal error, breaking infinite loop", out priNode);
            if(MatchSymbol(',')) continue;
            if(MatchSymbol(']')) {finished = true; break;}
            return SetError("Elements in a list must be comma separated", out priNode);
        }
        if (finished)
        {
            priNode = new PriList(nodes);
            return true;
        }
        return SetError("Unexpected eof while parsing list", out priNode);
    }
    private bool TryParseDict(out PriNode priNode)
    {
        // Note: opening brace already consumed
        priNode = PriNull.Null;
        Dictionary<string, PriNode> dict = [];
        bool finished = false;
        while (!Scanner.AtEof())
        {
            // Get key
            if(MatchSymbol('}')) {finished = true; break;}
            int start = Scanner.Current;
            if(!TryParseIdent(out var keyNode)) return false;
            if(keyNode.ToString() is not string key) return SetError("Bad key, should be unreachable", out priNode);
            if(dict.ContainsKey(key)) return SetError($"Key '{key}' already exists in the dictionary. Duplicate keys are not allowed.", start, out priNode);
            if (!MatchSymbol(':')) return SetError("Expected colon after field identifier", start, out priNode);
            if(!TryParseValue(out var valueNode)) return false;
            dict.Add(key, valueNode);
            if(MatchSymbol(',')) continue;
            if(MatchSymbol('}')) {finished = true; break;}
            return SetError("Expected comma or end brace after dictionary value", out priNode);
        }
        if (finished)
        {
            priNode = new PriDict(dict);
            return true;
        }
        return SetError("Unexpected eof while parsing dictionary", out priNode);
    }

    private bool TryParseValue(out PriNode priNode)
    {
        priNode = PriNull.Null;
        Scanner.Whitespace();
        if (Scanner.AtEof())
        {
            Error = Scanner.GetError("Unexpected eof");
            priNode = Error;
            return false;
        }
        if(Scanner.Match('"')) return TryParseString(out priNode);
        if(Scanner.Match('[')) return TryParseList(out priNode);
        if(Scanner.Match('{')) return TryParseDict(out priNode);
        if (Scanner.Match("null"))
        {
            priNode = PriNull.Null;
            return true;
        }
        if (Scanner.Match("true"))
        {
            priNode = PriBool.True;
            return true;
        }
        if (Scanner.Match("false"))
        {
            priNode = PriBool.False;
            return true;
        }
        bool negate = false;
        if(MatchSymbol('-')) negate = true;
        if(TryParseNumber(out priNode, negate)) return true;
        if(priNode.IsError) return false;
        if(Error is not null) return false;
        Scanner.TryPeek(out char c);
        return SetError($"Unexpected character '{c}'", out priNode);
    }
    public bool TryParse(string src, out PriNode priNode)
    {
        Scanner.SetSrc(src);
        if(!TryParseValue(out priNode)) return false;
        if(priNode.IsError) return false;
        if(Error is not null)
        {
            priNode = Error;
            return false;
        }
        Scanner.Whitespace();
        if (!Scanner.AtEof())
        {
            return SetError("Parsing did not reach end of file", out priNode);
        }
        return true;
    }
}