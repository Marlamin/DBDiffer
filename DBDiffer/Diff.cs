using System.Text.Json.Serialization;

namespace DBDiffer
{
    public class Diff
    {
        public Diff(DiffOperation operation, string path, object curvalue = null, object prevvalue = null)
        {
            Operation = operation;
            Property = path ?? "";
            CurrentValue = curvalue;
            PreviousValue = prevvalue;
        }

        [JsonPropertyName("op")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public DiffOperation Operation { get; set; }

        [JsonPropertyName("property")]
        public string Property { get; private set; }

        [JsonPropertyName("currentvalue")]
        public object CurrentValue { get; set; }

        [JsonPropertyName("previousvalue")]
        public object PreviousValue { get; set; }

        public override string ToString() => $"{Operation} {Property} {CurrentValue} {PreviousValue}";
    }

    internal static class JsonSerialization
    {
        public static System.Text.Json.JsonSerializerOptions CreateOptions(bool indented = false, System.Text.Json.Serialization.JsonConverter converter = null)
        {
            var options = new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = indented,
                IncludeFields = true,
                Converters = { new JsonStringEnumConverter() },
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull

            };
            options.Converters.Add(new JsonStringEnumConverter());
            if (converter != null)
                options.Converters.Add(converter);
            return options;
        }
    }
}
