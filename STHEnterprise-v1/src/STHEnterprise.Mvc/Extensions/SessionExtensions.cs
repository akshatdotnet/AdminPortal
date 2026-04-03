using System.Text.Json;

public static class SessionExtensions
{
    public static void SetObject<T>(this ISession session, string key, T value)
    {
        session.SetString(key, JsonSerializer.Serialize(value));
    }

    public static T GetObject<T>(this ISession session, string key)
    {
        var value = session.GetString(key);
        return value == null ? default : JsonSerializer.Deserialize<T>(value);
    }
}

//using System.Text.Json;
//
//namespace STHEnterprise.Mvc.Extensions;
//
//public static class SessionExtensions
//{
//    public static void SetObject<T>(this ISession session, string key, T value)
//    {
//        session.SetString(key, JsonSerializer.Serialize(value));
//    }
//
//    public static T GetObject<T>(this ISession session, string key) where T : new()
//    {
//        var json = session.GetString(key);
//
//        if (string.IsNullOrEmpty(json))
//            return new T();   // 🔥 THIS PREVENTS YOUR ERROR
//
//        return JsonSerializer.Deserialize<T>(json)!;
//    }
//}
