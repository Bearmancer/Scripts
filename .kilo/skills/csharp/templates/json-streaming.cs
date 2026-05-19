

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;


namespace JsonStreamingExamples
{
    public record User(
        int Id,
        string Name,
        [property: JsonPropertyName("email_address")]
        string Email);

    public record Order(
        int Id,
        int UserId,
        List<string> Items,
        decimal Total);


    public static class JsonReaderExamples
    {
        public static void StreamReadJsonArray(string jsonPath)
        {
            var options = new JsonReaderOptions { AllowTrailingCommas = true };
            
            using var stream = File.OpenRead(jsonPath);
            using var reader = new Utf8JsonReader(stream, options);

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.StartArray:
                        Console.WriteLine("Started array");
                        break;

                    case JsonTokenType.StartObject:
                        Console.WriteLine("Started object");
                        break;

                    case JsonTokenType.PropertyName:
                        string? propertyName = reader.GetString();
                        Console.WriteLine($"Property: {propertyName}");
                        break;

                    case JsonTokenType.String:
                        string? value = reader.GetString();
                        Console.WriteLine($"String value: {value}");
                        break;

                    case JsonTokenType.Number:
                        reader.TryGetInt64(out long numValue);
                        Console.WriteLine($"Number value: {numValue}");
                        break;

                    case JsonTokenType.EndObject:
                        Console.WriteLine("Ended object");
                        break;
                }
            }
        }

        public static IEnumerable<User> StreamParseUsers(string jsonPath)
        {
            using var stream = File.OpenRead(jsonPath);
            using var reader = new Utf8JsonReader(stream);

            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    using var doc = JsonDocument.ParseValue(ref reader);
                    var user = JsonSerializer.Deserialize<User>(doc.RootElement.GetRawText());
                    if (user != null)
                    {
                        yield return user;
                    }
                }
            }
        }
    }


    public static class JsonWriterExamples
    {
        public static void StreamWriteUsers(List<User> users, string outputPath)
        {
            using var stream = File.Create(outputPath);
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

            writer.WriteStartArray();

            foreach (var user in users)
            {
                writer.WriteStartObject();
                writer.WriteNumber("id", user.Id);
                writer.WriteString("name", user.Name);
                writer.WriteString("email_address", user.Email);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.Flush();
        }

        public static void WriteOrderStream(List<Order> orders, string outputPath)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };

            using var stream = File.Create(outputPath);
            using var writer = new StreamWriter(stream);

            writer.Write('[');
            bool first = true;

            foreach (var order in orders)
            {
                if (!first) writer.Write(',');
                string json = JsonSerializer.Serialize(order, options);
                writer.Write(json);
                first = false;
            }

            writer.Write(']');
            writer.Flush();
        }
    }


    public class DateTimeJsonConverter : JsonConverter<DateTime>
    {
        private const string DateFormat = "yyyy-MM-dd";

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? dateString = reader.GetString();
            if (DateTime.TryParse(dateString, out var date))
            {
                return date;
            }
            throw new JsonException($"Invalid date format: {dateString}");
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(DateFormat));
        }
    }
}
