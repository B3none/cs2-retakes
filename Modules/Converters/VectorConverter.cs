using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakesPlugin.Modules.Converters;

public class VectorConverter : JsonConverter<Vector>
{
    public override Vector Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Expected a string value.");
        }

        var stringValue = reader.GetString();
        if (stringValue == null)
        {
            throw new JsonException("String value is null.");
        }

        var values = stringValue.Split(' '); // Split by space

        if (values.Length != 3)
        {
            throw new JsonException("String value is not in the correct format (X Y Z).");
        }

        if (!float.TryParse(values[0], out var x) ||
            !float.TryParse(values[1], out var y) ||
            !float.TryParse(values[2], out var z))
        {
            throw new JsonException("Unable to parse float values.");
        }

        return new Vector(x, y, z);
    }

    public override void Write(Utf8JsonWriter writer, Vector value, JsonSerializerOptions options)
    {
        // Convert Vector object to string representation (example assumes ToString() returns desired format)
        var vectorString = value.ToString();
        writer.WriteStringValue(vectorString);
    }
}
