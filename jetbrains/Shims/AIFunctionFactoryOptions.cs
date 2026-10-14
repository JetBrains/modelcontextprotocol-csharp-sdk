#nullable enable

using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace ModelContextProtocol.Server;

// This type shadows Microsoft.Extensions.AI.AIFunctionFactoryOptions for the files of ModelContextProtocol.Server.
// Upstream's AIFunctionMcpServer{Tool,Prompt,Resource} name it unqualified and import Microsoft.Extensions.AI at compilation-unit level.
// Name lookup searches the enclosing namespace before those imports, so their CreateAIFunctionFactoryOptions build this type instead, with no upstream edit.
// The implicit conversion hands AIFunctionFactory.Create the real options.
// A member upstream starts to use that is missing here fails the build, which is the cue to extend this type.

/// <summary>
/// Stand-in for <see cref="Microsoft.Extensions.AI.AIFunctionFactoryOptions" /> whose <see cref="ParameterBindingOptions" /> has plain setters.
/// <para>The real <c>ParameterBindingOptions</c> has <c>init</c> accessors.
/// In the netstandard2.0 and net462 assets of Microsoft.Extensions.AI.Abstractions they carry <c>modreq</c> of an <c>IsExternalInit</c> the assembly declares itself.
/// Its net8.0 asset neither declares that type nor forwards it to System.Runtime, unlike System.Text.Json's.
/// So a call compiled against the netstandard2.0 asset throws <see cref="MissingMethodException" /> on .NET 8+ wherever the host deploys the net8.0 asset.
/// Setting the properties through reflection binds by name and works with every asset.</para>
/// </summary>
internal sealed class AIFunctionFactoryOptions
{
    private static readonly PropertyInfo s_bindParameterProperty = typeof(Microsoft.Extensions.AI.AIFunctionFactoryOptions.ParameterBindingOptions).GetProperty(nameof(Microsoft.Extensions.AI.AIFunctionFactoryOptions.ParameterBindingOptions.BindParameter))!;
    private static readonly PropertyInfo s_excludeFromSchemaProperty = typeof(Microsoft.Extensions.AI.AIFunctionFactoryOptions.ParameterBindingOptions).GetProperty(nameof(Microsoft.Extensions.AI.AIFunctionFactoryOptions.ParameterBindingOptions.ExcludeFromSchema))!;

    /// <summary>Same as the real member.</summary>
    public string? Name { get; set; }

    /// <summary>Same as the real member.</summary>
    public string? Description { get; set; }

    /// <summary>Same as the real member.</summary>
    public Func<object?, Type?, CancellationToken, ValueTask<object?>>? MarshalResult { get; set; }

    /// <summary>Same as the real member.</summary>
    public JsonSerializerOptions? SerializerOptions { get; set; }

    /// <summary>Same as the real member.</summary>
    public AIJsonSchemaCreateOptions? JsonSchemaCreateOptions { get; set; }

    /// <summary>Same as the real member, returning the stand-in <see cref="ParameterBindingOptions" />.</summary>
    public Func<ParameterInfo, ParameterBindingOptions>? ConfigureParameterBinding { get; set; }

    /// <summary>Stand-in for the real nested type, with plain setters.</summary>
    public struct ParameterBindingOptions
    {
        /// <summary>Same as the real member.</summary>
        public Func<ParameterInfo, AIFunctionArguments, object?>? BindParameter { get; set; }

        /// <summary>Same as the real member.</summary>
        public bool ExcludeFromSchema { get; set; }
    }

    public static implicit operator Microsoft.Extensions.AI.AIFunctionFactoryOptions(AIFunctionFactoryOptions options)
    {
        Func<ParameterInfo, ParameterBindingOptions>? configure = options.ConfigureParameterBinding;
        return new Microsoft.Extensions.AI.AIFunctionFactoryOptions
        {
            Name = options.Name,
            Description = options.Description,
            MarshalResult = options.MarshalResult,
            SerializerOptions = options.SerializerOptions,
            JsonSchemaCreateOptions = options.JsonSchemaCreateOptions,
            ConfigureParameterBinding = (configure is null) ? null : parameter => ToReal(configure(parameter)),
        };
    }

    private static Microsoft.Extensions.AI.AIFunctionFactoryOptions.ParameterBindingOptions ToReal(ParameterBindingOptions binding)
    {
        // A boxed struct is set in place through reflection, then unboxed as the result.
        object real = default(Microsoft.Extensions.AI.AIFunctionFactoryOptions.ParameterBindingOptions);
        s_bindParameterProperty.SetValue(real, binding.BindParameter);
        s_excludeFromSchemaProperty.SetValue(real, binding.ExcludeFromSchema);
        return (Microsoft.Extensions.AI.AIFunctionFactoryOptions.ParameterBindingOptions)real;
    }
}
