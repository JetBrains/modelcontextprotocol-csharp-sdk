#nullable enable

using Microsoft.Extensions.Logging;

namespace ModelContextProtocol.Server;

internal sealed partial class StreamableHttpPostTransport
{
    /// <summary>
    /// The <c>[LoggerMessage]</c> generator of Microsoft.Extensions.Logging.Abstractions 8 finds its logger only in a field, not in a primary constructor parameter as version 9 does.
    /// </summary>
    private readonly ILogger _logger = logger;
}
