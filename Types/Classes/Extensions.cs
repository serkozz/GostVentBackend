using Newtonsoft.Json;

namespace Types.Classes;

public static class ObjectExtensions
{
    public static string ToJSON(this object obj)
    {
        JsonSerializerSettings settings = new JsonSerializerSettings()
        {
            Formatting = Formatting.Indented,
        };
        return JsonConvert.SerializeObject(obj, settings);
    }
}