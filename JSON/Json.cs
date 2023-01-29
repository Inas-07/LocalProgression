using System;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.IO;

namespace ScanPosOverride.JSON
{
    internal static class Json
    {
        private static readonly JsonSerializerOptions _setting = new()
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            IncludeFields = false,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            IgnoreReadOnlyProperties = true
        };

        static Json()
        {
            _setting.Converters.Add(new JsonStringEnumConverter());
            _setting.Converters.Add(new Il2CppListConverterFactory());

            if (MTFOPartialDataUtil.IsLoaded && MTFOPartialDataUtil.Initialized)
            {
                _setting.Converters.Add(MTFOPartialDataUtil.PersistentIDConverter);
                Logger.Log("PartialData Support Found!");
            }
        }

        public static T Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, _setting);
        }

        public static object Deserialize(Type type, string json)
        {
            return JsonSerializer.Deserialize(json, type, _setting);
        }

        public static string Serialize(object value, Type type)
        {
            return JsonSerializer.Serialize(value, type, _setting);
        }

        public static void Load<T>(string file, out T config) where T : new()
        {
            if (file.Length < ".json".Length)
            {
                config = default;
                return;
            }

            if (file.Substring(file.Length - ".json".Length) != ".json")
            {
                file += ".json";
            }

            string filePath = Path.Combine(MTFO.Managers.ConfigManager.CustomPath, "ScanPositionOverride", file);

            file = File.ReadAllText(filePath);
            config = Deserialize<T>(file);
        }
    }
}
