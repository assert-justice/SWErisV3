using System.Text.Json.Nodes;
using Prion.Node;

namespace Prion.Parser;

public class PriParser
{
    private PriParserInternal? _ParserInternal;
    private PriParserInternal ParserInternal{get=>_ParserInternal??=new();}
    public PriPrettyPrinter PrettyPrinter{private get; set;} = new();
    private static PriParser? _Parser;
    public static PriParser Parser{get => _Parser ??= new();}
    public bool TryParse(string src, out PriNode priNode)
    {
        bool res = ParserInternal.TryParse(src, out priNode);
        if(priNode is PriError error)
        {
            Console.WriteLine(error.ToString());
        }
        return res;
    }
    public string PrettyPrint(PriNode priNode)
    {
        return PrettyPrinter.PrettyPrint(priNode);
    }
    public PriNode JsonToPrion(JsonNode? jsonNode)
    {
        return PriJsonConverter.JsonToPrion(jsonNode);
    }
}
