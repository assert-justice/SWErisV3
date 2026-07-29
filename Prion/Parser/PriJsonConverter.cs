using System.Text.Json.Nodes;
using Prion.Node;

namespace Prion.Parser;

public class PriJsonConverter
{
    public static JsonNode? PrionToJson(PriNode priNode)
    {
        switch (priNode.Kind)
        {
            case PriNodeKind.Bool:
                if (priNode is PriBool priBool) return JsonValue.Create(priBool.Value);
                break;
            case PriNodeKind.Dict:
                if (priNode is PriDict priDict) return PriDictToJson(priDict);
                break;
            case PriNodeKind.Error:
                // Todo: add warning?
                return null;
            case PriNodeKind.List:
                if(priNode is PriList priList) return PriListToJson(priList);
                break;
            case PriNodeKind.Null:
                return null;
            case PriNodeKind.Number:
                if(priNode is PriNumber priNumber) return JsonValue.Create(priNumber.ToF64());
                break;
            case PriNodeKind.String:
                if(priNode is PriString priString) return JsonValue.Create(priString.Value);
                break;
            case PriNodeKind.Variant:
                try
                {
                    return JsonNode.Parse(priNode.ToString() ?? "null");
                }
                catch
                {
                    // Todo: warn the variant could not be parsed.
                }
                break;
            default:
                break;
        }
        return null;
    }
    public static JsonObject PriDictToJson(PriDict priDict)
    {
        JsonObject res = [];
        foreach (var (key,value) in priDict.Data)
        {
            res[key] = PrionToJson(value);
        }
        return res;
    }
    public static JsonArray PriListToJson(PriList priList)
    {
        JsonArray res = [];
        foreach (var value in priList.Values)
        {
            res.Add(PrionToJson(value));
        }
        return res;
    }
    public static PriNode JsonToPrion(JsonNode? jsonNode)
    {
        if(jsonNode is null) return PriNull.Null;
        switch (jsonNode.GetValueKind())
        {
            case System.Text.Json.JsonValueKind.Undefined:
            case System.Text.Json.JsonValueKind.Null:
                return PriNull.Null;
            case System.Text.Json.JsonValueKind.String:
                return new PriString(jsonNode.ToString());
            case System.Text.Json.JsonValueKind.Number:
                // Todo: handle infinity/nan bs
                if(!double.TryParse(jsonNode.ToString(), out double d)) return PriNull.Null;
                if(d == MathF.Round((float)d)) return new PriNumber((int)d);
                else return new PriNumber(d);
            case System.Text.Json.JsonValueKind.True:
                return PriBool.True;
            case System.Text.Json.JsonValueKind.False:
                return PriBool.False;
            case System.Text.Json.JsonValueKind.Object:
                return JsonObjectToPrion(jsonNode.AsObject());
            case System.Text.Json.JsonValueKind.Array:
                return JsonArrayToPrion(jsonNode.AsArray());
            default:
                return PriNull.Null;
        }
    }
    public static PriDict JsonObjectToPrion(JsonObject jsonObject)
    {
        PriDict dict = new();
        foreach (var (key,value) in jsonObject)
        {
            dict.Data.Add(key, JsonToPrion(value));
        }
        return dict;
    }
    public static PriList JsonArrayToPrion(JsonArray jsonArray)
    {
        PriList list = new();
        foreach (var item in jsonArray)
        {
            list.Values.Add(JsonToPrion(item));
        }
        return list;
    }
}