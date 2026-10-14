#nullable enable

namespace System.Diagnostics.Metrics;

/// <summary>
/// Stand-in for the System.Diagnostics.DiagnosticSource 9 type of the same name, which version 8 lacks.
/// <para>Version 8 cannot pass advice to an instrument, so the bucket boundaries are dropped and a histogram uses the listener's default buckets.</para>
/// </summary>
internal sealed class InstrumentAdvice<T> where T : struct
{
    /// <summary>Accepted for source compatibility and not applied.</summary>
    public IReadOnlyList<T>? HistogramBucketBoundaries { get; init; }
}

/// <summary>
/// Provides the <c>advice</c> overload of <see cref="Meter.CreateHistogram{T}(string, string?, string?)" /> that System.Diagnostics.DiagnosticSource 8 lacks.
/// </summary>
internal static class MeterAdviceExtensions
{
    /// <summary>Creates the histogram without the advice; see <see cref="InstrumentAdvice{T}" />.</summary>
    public static Histogram<T> CreateHistogram<T>(this Meter meter, string name, string? unit, string? description, InstrumentAdvice<T> advice) where T : struct =>
        meter.CreateHistogram<T>(name, unit, description);
}
