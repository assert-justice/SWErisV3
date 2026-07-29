using Prion.Node;

namespace Prion.Parser;

public class PriScanner
{
    public string Src{get; private set;} = string.Empty;
    public string Filename{get; private set;} = "[anonymous file]";
    public int Current{get; private set;}
    public void SetSrc(string src)
    {
        Src = src;
        Current = 0;
    }
    public bool AtEof()
    {
        return Current >= Src.Length;
    }
    public bool TryPeek(out char c)
    {
        c = default;
        if(AtEof()) return false;
        c = Src[Current];
        return true;
    }
    public bool TryNext(out char c)
    {
        if(!TryPeek(out c)) return false;
        Current++;
        return true;
    }
    public void Advance()
    {
        if(!AtEof()) Current++;
    }
    public void Whitespace()
    {
        while(TryPeek(out char c))
        {
            if (Match("//"))
            {
                // Handle comments
                while(TryNext(out char ch)){if(ch == '\n') break;}
                continue;
            }
            if(!char.IsWhiteSpace(c)) break;
            Current++;
        }
    }
    // Todo: add case insensitive option
    public bool Match(char c)
    {
        if(!TryPeek(out char ch)) return false;
        if(c != ch) return false;
        Current++;
        return true;
    }
    public bool Match(string pattern)
    {
        int start = Current;
        foreach (char c in pattern)
        {
            if(!TryNext(out char ch) || ch != c)
            {
                Current = start;
                return false;
            }
        }
        return true;
    }
    public bool MatchFirst(char[] chars)
    {
        foreach (char c in chars)
        {
            if(Match(c)) return true;
        }
        return false;
    }
    public bool MatchFirst(char[] chars, out char c)
    {
        c = default;
        foreach (char ch in chars)
        {
            if (Match(ch))
            {
                c = ch;
                return true;
            }
        }
        return false;
    }
    public bool MatchFirst(string[] strings)
    {
        foreach (string str in strings)
        {
            if(Match(str)) return true;
        }
        return false;
    }
    public bool MatchFirst(string[] strings, out string str)
    {
        str = string.Empty;
        foreach (string s in strings)
        {
            if (Match(s))
            {
                str = s;
                return true;
            }
        }
        return false;
    }
    public PriError GetError(string message)
    {
        return GetError(message, Current);
    }
    public PriError GetError(string message, int charIndex)
    {
        return new PriError(message, Src, charIndex, Filename);
    }
}