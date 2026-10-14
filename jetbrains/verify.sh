#!/usr/bin/env bash
# Runs the fork's smoke checks against the package jetbrains/build.sh packed last.
# usage: jetbrains/verify.sh <host> [<host>...]
#   Each host runs one build of the checks:
#     a dotnet command runs the net8.0 build, on .NET 8 or, failing that, on the newest .NET the host has;
#     a mono command runs the net472 build on mono;
#     "direct" runs the net472 executable itself, which on Windows means .NET Framework.
#   The dotnet on PATH builds the checks and hosts the upstream HTTP server, so it must be a .NET 10 SDK.
#   DOTNET_ROLL_FORWARD=LatestMajor in the environment runs the net8.0 build on the newest .NET instead.
# Checks per host: enum wire names, stdio client against the fork's own stdio server, and HTTP client against the upstream ASP.NET Core server over Streamable HTTP and legacy SSE.
# Runs under Git Bash on Windows too.
set -euo pipefail
# Git Bash paths are POSIX; dotnet.exe takes them in Windows form, which cygpath gives.
native() { if command -v cygpath > /dev/null; then cygpath -w "$1"; else printf '%s' "$1"; fi; }
fork=$(cd "$(dirname "$0")" && pwd)
root=$(dirname "$fork")
out="$root/artifacts/jetbrains"
verify="$fork/verify"
version=$(dotnet msbuild "$(native "$root/src/ModelContextProtocol.Core/ModelContextProtocol.Core.csproj")" -getProperty:VersionPrefix | tr -d '\r')

# A private package folder, so a repacked fork package of an unchanged version is never served from a stale cache.
export NUGET_PACKAGES="$(native "$out/verify-packages")"
rm -rf "$out/verify-packages/jetbrains.modelcontextprotocol.core"
# A net8.0 build runs on a host that has only a newer .NET.
export DOTNET_ROLL_FORWARD="${DOTNET_ROLL_FORWARD:-Major}"
for name in Smoke HttpServer; do
  build=(
    -c Release
    -p:JbUpstreamVersion="$version"
    -p:BaseIntermediateOutputPath="$(native "$out/verify-obj/$name")/"  # keeps build output out of the source folder
    -p:BaseOutputPath="$(native "$out/verify-bin/$name")/"
    -v quiet
  )
  dotnet build "$(native "$verify/$name/$name.csproj")" "${build[@]}"
done

port=18765
ASPNETCORE_URLS="http://127.0.0.1:$port" dotnet "$(native "$out/verify-bin/HttpServer/Release/net10.0/HttpServer.dll")" > "$out/verify-httpserver.log" 2>&1 &
server=$!
trap 'kill $server 2>/dev/null || true' EXIT
for _ in $(seq 1 60); do curl -s -o /dev/null "http://127.0.0.1:$port/" && break; sleep 0.5; done

failures=0
# Each check gets its words as separate arguments, so a host path with spaces stays one argument.
check() { if ! timeout 120 "${app[@]}" "$@"; then failures=$((failures + 1)); fi; }
for host in "$@"; do
  case "$(basename "$host")" in
    direct) app=("$out/verify-bin/Smoke/Release/net472/Smoke.exe") ;;
    mono*)  app=("$host" "$(native "$out/verify-bin/Smoke/Release/net472/Smoke.exe")") ;;
    *)      app=("$host" "$(native "$out/verify-bin/Smoke/Release/net8.0/Smoke.dll")") ;;
  esac
  check json
  # The smoke client starts the stdio server through the host, and on Windows the client is a Windows process, so it takes the path in Windows form.
  # A bare command name is resolved first, since only a path converts.
  if [ "$host" = direct ]; then
    check stdio direct
  else
    check stdio "$(native "$(command -v "$host" || printf '%s' "$host")")"
  fi
  check http "http://127.0.0.1:$port/" StreamableHttp
  check http "http://127.0.0.1:$port/sse" Sse
done
echo "verify: $failures failed check run(s)"
exit $((failures > 0))
