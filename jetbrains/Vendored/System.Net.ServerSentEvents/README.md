Parser half of `System.Net.ServerSentEvents` from dotnet/runtime v10.0.0 (commit `60629d14374c56f1cb51819049ad1fa529307f8d`), compiled into ModelContextProtocol.Core by the JetBrains fork.

Every published version of the package needs `Microsoft.Bcl.AsyncInterfaces` 9 or newer, above the main solution lock.

Changes against the runtime files, each marked `// JetBrains fork:` where it is:
- `SseParser`, `SseParser<T>` and `SseItemParser<T>` are internal; `SseItem<T>` stays public, because the public `ISseEventStreamReader`/`ISseEventStreamWriter` expose it.
- `Helpers.cs` is left out: upstream's `src/Common/ServerSentEvents/SseEventWriterHelpers.cs` is its copy.
- `SR.cs` is fork-owned and stands in for the runtime resource strings.
- `SseFormatter.cs` and `PooledByteBufferWriter.cs` are left out: upstream has its own writer in `src/Common/ServerSentEvents`.
