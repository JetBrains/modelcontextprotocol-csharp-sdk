#nullable enable

namespace System.Net.ServerSentEvents;

/// <summary>
/// The resource strings the vendored System.Net.ServerSentEvents sources use, copied from its Strings.resx at the same commit.
/// <para>Declared in their namespace, so the vendored files bind to it ahead of any inaccessible <c>System.SR</c> from referenced assemblies.</para>
/// </summary>
internal static class SR
{
    public static string InvalidOperation_EnumerateOnlyOnce => "The enumerable may be enumerated only once.";
    public static string ArgumentException_CannotContainLineBreaks => "The argument cannot contain line breaks.";
    public static string ArgumentException_CannotBeNegative => "The argument cannot be a negative value.";
}
