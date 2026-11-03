using System.Text.Json;
using JsonSerializer = GTFO.API.JSON.JsonSerializer;

namespace LocalProgression
{
    internal static class JSON
    {
        private static readonly JsonSerializerOptions _setting = new(JsonSerializer.DefaultSerializerSettings);

        public static T Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, _setting)!;
        }

        public static string Serialize<T>(T value)
        {
            return JsonSerializer.Serialize(value, _setting)!;
        }
    }
}
