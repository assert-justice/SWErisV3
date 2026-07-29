// using Prion.Node;

// namespace Prion.Validator;

// public class PriValidData
// {
//     public readonly string TypeName;
//     private readonly PriDict Dict = null!;
//     private PriError? Error;
//     public bool HasError{get => Error is not null;}
//     public PriValidData(string typeName, PriNode node)
//     {
//         TypeName = typeName;
//         if(node is PriDict dict) Dict = dict;
//         else Error = new($"Node for type '{typeName}' is not a dictionary.");
//     }
//     public bool TryGet<T>(string key, out T value)
//     {
//         value = default!;
//         if(HasError) return false;
//         if (Dict.Get(key, out value)) return true;
//         Error = new($"Node for type '{TypeName}' is missing a '{key}' field.");
//         return false;
//     }
//     public bool GetError(out PriError error)
//     {
//         error = Error!;
//         return false;
//     }
//     // public static bool TryValidate(PriNode node, string name, out PriValidDict validDict)
//     // {
//     //     validDict = null!;
//     //     return true;
//     // }
// }