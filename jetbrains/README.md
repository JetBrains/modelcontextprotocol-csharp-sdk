# JetBrains fork overlay

Builds `ModelContextProtocol.Core` as one netstandard2.0 asset, against the package versions of the JetBrains main solution (`DevKit/Config/MainSolutionPackageVersions.xml`), and packs it as `JetBrains.ModelContextProtocol.Core`.
The one asset serves the main solution's netfx and netcore flavors alike.

Everything the fork changes lives in this folder, apart from one file upstream does not have: `.github/workflows/jetbrains.yml`.
No upstream file is edited, so an upstream merge cannot conflict with the fork.
When upstream code changes in a way the overlay no longer fits, the fork build fails; it does not drift silently.

## Use

```
jetbrains/build.sh all [-p:JbForkRevision=N]
jetbrains/verify.sh <host>...    # dotnet, another dotnet command, mono, or direct for the netfx executable itself
```

- Nothing needs to be installed but Bash, which on Windows is Git Bash.
  `verify.sh` also needs `timeout` of GNU coreutils, and `snapshot-lock.sh` Bash 4 or newer; Linux and Git Bash have both, stock macOS neither: Homebrew `bash` gives the one, and Homebrew `coreutils` the other once its `libexec/gnubin` is on `PATH`, since it otherwise names it `gtimeout`.
  Unless `JB_DOTNET` names a dotnet, the scripts run themselves under `dotnet-sdk.cmd`, which downloads its pinned SDK once into the user profile.
  That file is a verbatim copy of `dotnet/DevKit/Scripts/dotnet-sdk.cmd` of the JetBrains monorepo, at its commit b85cdc8809fa7; it pins SDK 10.0.401.
- `build.sh` packs into `artifacts/jetbrains/packages`.
  To sign the assemblies, run its `build` step, sign `artifacts/jetbrains/bin/**/*.dll` in place, then run its `pack` step, which packs without building again.
- The package version is stable and has four components: upstream major, upstream minor, upstream patch, and `JbForkRevision`. So upstream 2.2.0 gives 2.2.0.1 on the first fork build.
  The build refuses a version without a nonzero fork revision.
  Bump `JbForkRevision` on every rebuild from a different commit, because upstream does not always bump `VersionPrefix`; a CI build counter does.
- The assembly version stays upstream's, so the assembly binds wherever the upstream one would; the file version carries the package version.
- Package validation compares each build against the upstream package of the same version, so a public API removal fails the build.
- `verify.sh` runs the checks in `verify/Smoke` on each host: enum wire names, a stdio client against the fork's own stdio server, and an HTTP client against the upstream ASP.NET Core server over Streamable HTTP and over legacy SSE.
  The bootstrapped SDK has the .NET 10 runtime only; pass another dotnet command to check on .NET 8, and `DOTNET_ROLL_FORWARD=LatestMajor` runs the net8.0 build on the newest .NET a host has.
  On Windows, `direct` means .NET Framework.

## The lock

Every build uses `lock/MainSolutionPackageVersions.subset.xml`: the main solution lock entries of only the packages this build resolves, verbatim.
Pinning acts only on packages in the restore graph, so it resolves exactly what the full lock does; a build against either gives an identical nuspec.
Committed, it ties each fork revision to one set of versions, whatever the main solution has moved to since.
`snapshot-lock.sh <full lock>` rewrites it from a main solution lock.
Run it to take a lock move over, and after every upstream merge: a dependency upstream adds is pinned only once the subset has it, and goes unpinned silently until then.
`JB_PACKAGE_LOCK` makes `build.sh` use another lock, as `snapshot-lock.sh` does.

## Builds

- **TeamCity**, the one that signs, runs `build.sh build`, the code signing step on `artifacts/jetbrains/bin`, then `build.sh pack`, with the build counter as `JbForkRevision`.
- **GitHub Actions** (`.github/workflows/jetbrains.yml`) runs on pushes and pull requests to the `jetbrains` branch, on Linux and Windows.
  It builds, runs the checks on .NET 10, and on .NET Framework on Windows, and uploads the unsigned package.

## How it hooks in

`build.sh` passes `JetBrains.Fork.targets` as `CustomAfterMicrosoftCommonTargets`, which covers the inner per-framework evaluation.
It also passes it as `CustomAfterMicrosoftCommonCrossTargetingTargets`, because restore reads its settings in the outer evaluation.
The targets import the lock file as MSBuild, since every `PackageLock` item carries its version in `Version` metadata.
They then replace every `PackageVersion` the lock knows and turn on transitive pinning.
As a result, every package in the nuspec sits at exactly its lock version.
Restore fails with NU1109 if any dependency needs more than the lock has.

## Changes, and when each can go

| Change | Why | Remove when |
|---|---|---|
| `LockAdditions.props`: Microsoft.Extensions.AI.Abstractions 9.10.2 | Not in the lock; 9.10.2 is the newest version whose netstandard2.0 dependencies are only System.Text.Json >= 8.0.6. Must be added to the lock before consuming the fork. | The lock has it. |
| `Vendored/System.Net.ServerSentEvents` | Every package version needs Microsoft.Bcl.AsyncInterfaces >= 9; the lock has 8.0.0. The parser is compiled in from dotnet/runtime v10.0.0; see its README. | The lock has Microsoft.Bcl.AsyncInterfaces >= 9; add the package to the lock instead. |
| `Polyfills/.../JsonStringEnumMemberNameAttribute.cs`, `Polyfills/JsonStringEnumConverter.cs` | System.Text.Json 8 lacks the attribute, and would write `"User"` where MCP wants `"user"`. The converter types shadow System.Text.Json's for the whole assembly by namespace lookup; see the file. | The lock has System.Text.Json >= 9. |
| `Polyfills/.../InstrumentAdvice.cs` | DiagnosticSource 8 lacks `InstrumentAdvice<T>`; histograms lose their explicit bucket boundaries. | The lock has System.Diagnostics.DiagnosticSource >= 9. |
| `Shims/StreamableHttpPostTransport.Logger.cs` | The `[LoggerMessage]` generator of Logging.Abstractions 8 needs an `ILogger` field, not a primary constructor parameter. | The lock has Microsoft.Extensions.Logging.Abstractions >= 9. |
| `Shims/AIFunctionFactoryOptions.cs` | The init accessors of `ParameterBindingOptions` bind to a different `IsExternalInit` in the netstandard2.0 asset of Microsoft.Extensions.AI.Abstractions than in its net8.0 asset, which forwards none. So the netstandard2.0 build throws `MissingMethodException` on .NET 8+ wherever the net8.0 asset is deployed. | Microsoft.Extensions.AI.Abstractions forwards `IsExternalInit` in its net8.0 asset, or the main solution deploys a single asset of it for both runtimes. |
| `PackageReference Remove="Microsoft.SourceLink.GitHub"` | Source Link 10 drags System.IO.Hashing 10 past the lock; the .NET 8+ SDK has Source Link built in. | Never needed back. |
| `NoWarn CS1574` | Upstream doc comments cref System.Text.Json 9 members. | The lock has System.Text.Json >= 9. |
| `NuGetAuditSuppress` CVE-2026-26127 | The lock's Microsoft.Bcl.Memory 9.0.0 is vulnerable; 9.0.14 fixes it. | The lock moves to 9.0.14 or newer. |

## Known limits

- One asset only. Core declares `init` accessors of its own, so a second, net8.0 asset would bring back the `IsExternalInit` mismatch for main-solution code compiled against the netstandard2.0 one.
- Public API differs from upstream's netstandard2.0 asset in one addition: `System.Net.ServerSentEvents.SseItem<T>` is public in this assembly, because the obsolete `ISseEventStreamReader`/`ISseEventStreamWriter` expose it.
  Code that compiles against both this assembly and the real System.Net.ServerSentEvents, which .NET 10 has inbox, and names `SseItem<T>` needs an extern alias.
- Main-solution code that itself sets `init` properties of Microsoft.Extensions.AI types meets the same `MissingMethodException` on netcore, unless the main solution deploys one asset of that package.
- `ModelContextProtocol` (the hosting and DI package) is not built.
  It also needs Microsoft.Extensions.Hosting.Abstractions 8.0.0 added to the lock.
  And it trips over a lock inconsistency: Microsoft.Extensions.Options 8.0.0 needs System.ComponentModel.Annotations >= 5.0.0, while the lock has 4.5.0.
- `ModelContextProtocol.AspNetCore` is netcore-only upstream and is not built.

## Merging upstream

1. Merge or rebase onto the new upstream commit; there is nothing to conflict.
2. Run `snapshot-lock.sh` against the current main solution lock, and commit the subset if it changed.
3. Run `build.sh all`. A compile error inside a `Shims` or `Polyfills` file means upstream now uses a member the stand-in lacks; extend the stand-in.
4. Run `verify.sh` on .NET 10 and on .NET Framework or mono, or let GitHub Actions do it.
5. Re-check the table: when the lock has moved, drop the rows it obsoletes.
