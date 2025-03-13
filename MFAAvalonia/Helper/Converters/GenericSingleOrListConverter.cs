using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace MFAAvalonia.Helper.Converters;

/// <summary>
/// 支持单个对象或对象数组的泛型转换器
/// （兼容 Newtonsoft.Json 的高级动态处理特性）[1](@ref)
/// </summary>
/// <typeparam name="T">目标类型</typeparam>
[JsonObject(ItemRequired = Required.AllowNull)]
public class GenericSingleOrListConverter<T> : JsonConverter
{
    public override bool CanConvert(Type objectType) =>
        objectType == typeof(T) || objectType == typeof(IEnumerable<T>);

    public override object ReadJson(JsonReader reader,
        Type objectType,
        object existingValue,
        JsonSerializer serializer)
    {
        var token = JToken.Load(reader);

        return token switch
        {
            JObject obj => [
                obj.ToObject<T>(serializer)
            ],
            JArray arr => arr.ToObject<List<T>>(serializer),
            JValue { Type: JTokenType.Null } => null,
            _ => HandlePrimitive(token, serializer)
        };
    }

    private List<T> HandlePrimitive(JToken token, JsonSerializer serializer)
    {
        // 特殊处理字符串类型
        if (typeof(T) == typeof(string))
        {
            return new List<T>
            {
                (T)(object)token.ToString()
            };
        }

        if (typeof(T).IsPrimitive || IsSupportedValueType())
        {
            return new List<T>
            {
                token.ToObject<T>(serializer)
            };
        }

        throw new JsonSerializationException($"类型 {typeof(T)} 不支持基础值转换");

        bool IsSupportedValueType() =>
            typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) == null;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var list = value switch
        {
            IEnumerable<T> collection => collection,
            T single => new List<T>
            {
                single
            },
            _ => throw new JsonException("不支持的序列化类型")
        };

        serializer.Serialize(writer, list);
    }
}
