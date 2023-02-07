using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;

namespace Hast.VhdlBuilder.Representation.Expression;

[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class BinaryOperator : IVhdlElement
{
    private readonly string _source;

    public static readonly BinaryOperator Add = new("+");
    public static readonly BinaryOperator And = new("and");
    public static readonly BinaryOperator Divide = new("/");
    public static readonly BinaryOperator Equality = new("=");
    public static readonly BinaryOperator ExclusiveOr = new("xor");
    public static readonly BinaryOperator GreaterThan = new(">");
    public static readonly BinaryOperator GreaterThanOrEqual = new(">=");
    public static readonly BinaryOperator InEquality = new("/=");
    public static readonly BinaryOperator LessThan = new("<");
    public static readonly BinaryOperator LessThanOrEqual = new("<=");
    public static readonly BinaryOperator Modulus = new("mod");
    public static readonly BinaryOperator Multiply = new("*");
    public static readonly BinaryOperator Or = new("or");
    public static readonly BinaryOperator Remainder = new("rem");
    public static readonly BinaryOperator ShiftLeftLogical = new("sll");
    public static readonly BinaryOperator ShiftRightLogical = new("srl");
    public static readonly BinaryOperator Subtract = new("-");

    public static readonly JsonConverter JsonConverter = new BinaryOperatorJsonConverter();

    private BinaryOperator(string source) => _source = source;

    public string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions) => _source;

    private sealed class BinaryOperatorJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(BinaryOperator);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // Load the JSON for the Result into a JObject
            var jObject = JObject.Load(reader);

            return new BinaryOperator((string)jObject["Source"]);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var token = JToken.FromObject(value);
            if (token.Type != JTokenType.Object)
            {
                token.WriteTo(writer);
            }
            else
            {
                var jObject = (JObject)token;
                jObject.AddFirst(new JProperty("Source", ((BinaryOperator)value)._source));
                jObject.WriteTo(writer);
            }
        }
    }
}
