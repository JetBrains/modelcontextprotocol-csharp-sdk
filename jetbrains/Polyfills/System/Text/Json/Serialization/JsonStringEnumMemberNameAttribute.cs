#nullable enable

namespace System.Text.Json.Serialization;

/// <summary>
/// Stand-in for the System.Text.Json 9 attribute of the same name, which System.Text.Json 8 lacks.
/// <para>System.Text.Json 8 itself ignores it; <see cref="ModelContextProtocol.JsonStringEnumConverter{TEnum}" /> is what reads it.</para>
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
internal sealed class JsonStringEnumMemberNameAttribute(string name) : Attribute
{
    /// <summary>The JSON name of the enum member.</summary>
    public string Name { get; } = name;
}
