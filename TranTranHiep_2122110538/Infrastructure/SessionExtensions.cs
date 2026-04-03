using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace TranTranHiep_2122110538.Infrastructure;

public static class SessionExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static void SetJson<T>(this ISession session, string key, T value)
    {
        session.SetString(key, JsonSerializer.Serialize(value, JsonOptions));
    }

    public static T? GetJson<T>(this ISession session, string key)
    {
        var s = session.GetString(key);
        return s == null ? default : JsonSerializer.Deserialize<T>(s, JsonOptions);
    }
}
