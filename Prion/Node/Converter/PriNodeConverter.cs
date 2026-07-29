// using System.Numerics;

// namespace Prion.Node.Converter;

// internal static class PriNodeConverter
// {
//     private static readonly Dictionary<Type,Converter> Converters = [];
//     static PriNodeConverter()
//     {
//         AddConverter(new NumberConverter<float>(PriNumber.NumberMode.Float, 32));
//         AddConverter(new NumberConverter<double>(PriNumber.NumberMode.Float, 64));
//         AddConverter(new NumberConverter<sbyte>(PriNumber.NumberMode.SignedInt, 8));
//         AddConverter(new NumberConverter<short>(PriNumber.NumberMode.SignedInt, 16));
//         AddConverter(new NumberConverter<int>(PriNumber.NumberMode.SignedInt, 32));
//         AddConverter(new NumberConverter<long>(PriNumber.NumberMode.SignedInt, 64));
//         AddConverter(new NumberConverter<byte>(PriNumber.NumberMode.SignedInt, 8));
//         AddConverter(new NumberConverter<ushort>(PriNumber.NumberMode.SignedInt, 16));
//         AddConverter(new NumberConverter<uint>(PriNumber.NumberMode.SignedInt, 32));
//         AddConverter(new NumberConverter<ulong>(PriNumber.NumberMode.SignedInt, 64));
//         AddConverter(new StringConverter());
//         AddConverter(new BoolConverter());
//     }
//     private static void AddConverter(Converter converter)
//     {
//         Converters.Add(converter.ValueType, converter);
//     }
//     public static bool TryToPrion<T>(T value, out PriNode priNode)
//     {
//         priNode = PriNull.Null;
//         if(!Converters.TryGetValue(typeof(T), out var converter)) return false;
//         return converter.TryToPrion(value, out priNode);
//     }
//     public static bool TryToValue<T>(PriNode priNode, out T value)
//     {
//         value = default!;
//         if(!Converters.TryGetValue(typeof(T), out var converter)) return false;
//         return converter.TryToValue(priNode, out value);
//     }
//     public static bool TryGetConverter(Type type, out Converter converter)
//     {
//         return Converters.TryGetValue(type, out converter!);
//     }
//     public abstract class Converter
//     {
//         public abstract Type ValueType{get;}
//         protected static bool TryToValueInternal<T, U>(U input, out T value)
//         {
//             value = default!;
//             if(input is not T val) return false;
//             value = val;
//             return true;
//         }
//         public virtual bool TryToPrion<T>(T value, out PriNode priNode)
//         {
//             priNode = PriNull.Null;
//             return false;
//         }
//         public virtual bool TryToValue<T>(PriNode priNode, out T value)
//         {
//             value = default!;
//             return false;
//         }
//     }
//     private class NumberConverter<U>: Converter where U: INumber<U>
//     {
//         public override Type ValueType => typeof(U);
//         private readonly int SizeInBits;
//         private readonly PriNumber.NumberMode NumberMode;
//         public NumberConverter(PriNumber.NumberMode mode, int sizeInBits){
//             NumberMode = mode;
//             SizeInBits = sizeInBits;
//         }
//         public override bool TryToPrion<T>(T value, out PriNode priNode)
//         {
//             base.TryToPrion(value, out priNode);
//             if(value is not U f) return false;
//             // U val = (U)Convert.ChangeType()
//             // Convert.ToDecimal(f);
//             priNode = new PriNumber(Convert.ToDecimal(f), NumberMode, SizeInBits);
//             return true;
//         }
//         public override bool TryToValue<T>(PriNode priNode, out T value)
//         {
//             base.TryToValue(priNode, out value);
//             U val;
//             if(priNode is PriVariant<U> priVariant) val = priVariant.Value;
//             else if(priNode is PriNumber priNumber) val = (U)Convert.ChangeType(priNumber.Value, typeof(U));
//             else return false;
//             return TryToValueInternal(val, out value);
//         }
//     }
//     private class StringConverter : Converter
//     {
//         public override Type ValueType => typeof(string);
//         public override bool TryToPrion<T>(T value, out PriNode priNode)
//         {
//             base.TryToPrion(value, out priNode);
//             if(value is not string str) return false;
//             priNode = new PriString(str);
//             return true;
//         }
//         public override bool TryToValue<T>(PriNode priNode, out T value)
//         {
//             base.TryToValue(priNode, out value);
//             string val;
//             if(priNode is PriVariant<string> priVariant) val = priVariant.Value;
//             else if(priNode is PriString priString) val = priString.Value;
//             else return false;
//             return TryToValueInternal(val, out value);
//         }
//     }
//     private class BoolConverter : Converter
//     {
//         public override Type ValueType => typeof(bool);
//         public override bool TryToPrion<T>(T value, out PriNode priNode)
//         {
//             base.TryToPrion(value, out priNode);
//             if(value is not bool b) return false;
//             priNode = b ? PriBool.True : PriBool.False;
//             return true;
//         }
//         public override bool TryToValue<T>(PriNode priNode, out T value)
//         {
//             base.TryToValue(priNode, out value);
//             bool val;
//             if(priNode is PriVariant<bool> priVariant) val = priVariant.Value;
//             else if(priNode is PriBool priBool) val = priBool.Value;
//             else return false;
//             return TryToValueInternal(val, out value);
//         }
//     }
// }