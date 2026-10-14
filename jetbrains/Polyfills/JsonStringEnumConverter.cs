#nullable enable

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModelContextProtocol;

// These two types shadow System.Text.Json.Serialization.JsonStringEnumConverter{,<TEnum>} for every file of the assembly.
// Every upstream file lives in a namespace under ModelContextProtocol and imports System.Text.Json.Serialization at compilation-unit level.
// Name lookup searches the enclosing namespaces before those imports, so an unqualified JsonStringEnumConverter binds here with no upstream edit.
// A using placed inside a namespace declaration would bind to System.Text.Json first; the enum wire-name tests catch that.

/// <summary>
/// Replaces System.Text.Json 8's <c>JsonStringEnumConverter&lt;TEnum&gt;</c> with one that honors <see cref="JsonStringEnumMemberNameAttribute" />, as System.Text.Json 9 does.
/// </summary>
internal sealed class JsonStringEnumConverter<TEnum>() : System.Text.Json.Serialization.JsonStringEnumConverter<TEnum>(new EnumMemberNamePolicy(typeof(TEnum), null), allowIntegerValues: true)
    where TEnum : struct, Enum;

/// <summary>
/// Replaces System.Text.Json 8's non-generic <c>JsonStringEnumConverter</c> with one that honors <see cref="JsonStringEnumMemberNameAttribute" />, as System.Text.Json 9 does.
/// </summary>
internal sealed class JsonStringEnumConverter : JsonConverterFactory
{
    private readonly JsonNamingPolicy? _namingPolicy;
    private readonly bool _isAllowingIntegerValues;

    /// <summary>Parameterless, because <c>[JsonConverter(typeof(...))]</c> instantiates through it.</summary>
    public JsonStringEnumConverter() : this(null, true)
    {
    }

    /// <summary>Same parameters as the System.Text.Json constructor it replaces.</summary>
    public JsonStringEnumConverter(JsonNamingPolicy? namingPolicy = null, bool allowIntegerValues = true)
    {
        _namingPolicy = namingPolicy;
        _isAllowingIntegerValues = allowIntegerValues;
    }

    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsEnum;

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        // The generic System.Text.Json factory takes the naming policy per enum type, which is what the member-name lookup needs.
        var factory = (JsonConverterFactory)Activator.CreateInstance(typeof(System.Text.Json.Serialization.JsonStringEnumConverter<>).MakeGenericType(typeToConvert), new EnumMemberNamePolicy(typeToConvert, _namingPolicy), _isAllowingIntegerValues)!;
        return factory.CreateConverter(typeToConvert, options);
    }
}

/// <summary>
/// Names an enum member by its <see cref="JsonStringEnumMemberNameAttribute" />, falling back to the wrapped policy, then to the member name.
/// <para>System.Text.Json 8 applies a naming policy to each member name separately, for writing and for reading alike, so this reproduces the System.Text.Json 9 attribute.</para>
/// </summary>
internal sealed class EnumMemberNamePolicy(Type enumType, JsonNamingPolicy? fallback) : JsonNamingPolicy
{
    private readonly Dictionary<string, string> _names = enumType
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Select(field => (Field: field, Attribute: field.GetCustomAttribute<JsonStringEnumMemberNameAttribute>()))
        .Where(pair => pair.Attribute is not null)
        .ToDictionary(pair => pair.Field.Name, pair => pair.Attribute!.Name);

    public override string ConvertName(string name) => _names.TryGetValue(name, out string? jsonName) ? jsonName : (fallback?.ConvertName(name) ?? name);
}
