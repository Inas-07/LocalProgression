using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LocalProgression
{
    internal static class JSON
    {
        private static JsonSerializerOptions _setting;
        
        public  static T Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, _setting);
        }

        public static string Serialize<T>(T value)
        {
            return JsonSerializer.Serialize(value, _setting);
        }

        static JSON()
        {
            _setting = new()
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
                IncludeFields = false,
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
                IgnoreReadOnlyProperties = true
            };
            _setting.Converters.Add(new JsonStringEnumConverter());
        }
    }
}
