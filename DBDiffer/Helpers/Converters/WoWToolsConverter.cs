using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using DBDiffer.DiffResults;

namespace DBDiffer.Helpers.Converters
{
    internal class WoWToolsConverter : JsonConverter<WoWToolsDiff>
    {
        public override WoWToolsDiff Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => throw new NotImplementedException();

        public override void Write(Utf8JsonWriter writer, WoWToolsDiff diff, JsonSerializerOptions options)
        {
            var record = JsonSerializer.SerializeToElement(diff.Record, JsonSerialization.CreateOptions());

            // open the entry
            writer.WriteStartObject();

            // write the db record
            writer.WritePropertyName("row");
            writer.WriteStartObject();
            var writtenProperties = new HashSet<string>();
            foreach (var prop in record.EnumerateObject())
            {
                WriteRecordValue(writer, prop.Name, GetMemberValue(diff.Record, prop.Name) ?? prop.Value, options);
                writtenProperties.Add(prop.Name);
            }

            if (diff.Record is DynamicObject dynamicRecord)
            {
                foreach (var name in dynamicRecord.GetDynamicMemberNames())
                {
                    if (writtenProperties.Contains(name) || !dynamicRecord.TryGetMember(new FakeBinder(name), out var value))
                        continue;

                    WriteRecordValue(writer, name, value, options);
                }
            }
            writer.WriteEndObject();

            // write Operation as it's string representation
            writer.WritePropertyName("op");
            writer.WriteStringValue(diff.Operation.ToString());
            // use default serialization for the diffs
            writer.WritePropertyName("diff");
            JsonSerializer.Serialize(writer, diff.Diffs, options);

            // close the entry
            writer.WriteEndObject();
        }

        private static object GetMemberValue(object record, string name)
        {
            if (record is DynamicObject dynamicRecord && dynamicRecord.TryGetMember(new FakeBinder(name), out var value))
                return value;

            var type = record.GetType();
            return (object)type.GetField(name, BindingFlags.Instance | BindingFlags.Public)?.GetValue(record)
                ?? type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public)?.GetValue(record);
        }

        private static void WriteRecordValue(Utf8JsonWriter writer, string name, object value, JsonSerializerOptions options)
        {
            if (value is byte[] bytes)
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    writer.WritePropertyName($"{name}[{i}]");
                    writer.WriteNumberValue(bytes[i]);
                }
            }
            else if (value is Array array)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    writer.WritePropertyName($"{name}[{i}]");
                    JsonSerializer.Serialize(writer, array.GetValue(i), array.GetType().GetElementType(), options);
                }
            }
            else if (value is JsonElement element && element.ValueKind == JsonValueKind.Array)
            {
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    writer.WritePropertyName($"{name}[{index++}]");
                    item.WriteTo(writer);
                }
            }
            else
            {
                writer.WritePropertyName(name);
                if (value is JsonElement jsonElement)
                    jsonElement.WriteTo(writer);
                else
                    JsonSerializer.Serialize(writer, value, value?.GetType() ?? typeof(object), options);
            }
        }
    }
}
