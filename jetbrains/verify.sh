#!/usr/bin/env bash
# Runs the fork's smoke checks against the package jetbrains/build.sh packed last.
# usage: jetbrains/verify.sh <host> [<host>...]
#   Each host is a command that runs a managed app: a dotnet runs the net8.0 build, a mono runs the net472 build.
#   The dotnet on PATH builds the checks and hosts the upstream HTTP server, so it must be a .NET 10 SDK.
# Checks per host: enum wire names, stdio client against the fork's own stdio server, and HTTP client against the upstream ASP.NET Core server over Streamable HTTP and legacy SSE.
set -euo pipefail
fork=$(cd "$(dirname "$0")" && pwd)
root=$(dirname "$fork")
out="$root/artifacts/jetbrains"
verify="$fork/verify"
version=$(dotnet msbuild "$root/src/ModelContextProtocol.Core/ModelContextProtocol.Core.csproj" -getProperty:VersionPrefix)

# A private package folder, so a repacked fork package of an unchanged version is never served from a stale cache.
export NUGET_PACKAGES="$out/verify-packages"
rm -rf "$NUGET_PACKAGES/jetbrains.modelcontextprotocol.core"
# A net8.0 build runs on a host that has only a newer .NET.
export DOTNET_ROLL_FORWARD=Major
for name in Smoke HttpServer; do
  build=(
    -c Release
    -p:JbUpstreamVersion="$version"
    -p:BaseIntermediateOutputPath="$out/verify-obj/$name/"  # keeps build output out of the source folder
    -p:BaseOutputPath="$out/verify-bin/$name/"
    -v quiet
  )
  dotnet build "$verify/$name/$name.csproj" "${build[@]}"
done

port=18765
ASPNETCORE_URLS="http://127.0.0.1:$port" dotnet "$out/verify-bin/HttpServer/Release/net10.0/HttpServer.dll" > "$out/verify-httpserver.log" 2>&1 &
server=$!
trap 'kill $server 2>/dev/null || true' EXIT
for _ in $(seq 1 60); do curl -s -o /dev/null "http://127.0.0.1:$port/" && break; sleep 0.5; done

failures=0
for host in "$@"; do
  case "$(basename "$host")" in
    mono*) app=("$host" "$out/verify-bin/Smoke/Release/net472/Smoke.exe") ;;
    *)     app=("$host" "$out/verify-bin/Smoke/Release/net8.0/Smoke.dll") ;;
  esac
  for check in "json" "stdio $host" "http http://127.0.0.1:$port/ StreamableHttp" "http http://127.0.0.1:$port/sse Sse"; do
    # shellcheck disable=SC2086 # the check is a word list on purpose
    if ! timeout 120 "${app[@]}" $check; then failures=$((failures + 1)); fi
  done
done
echo "verify: $failures failed check run(s)"
exit $((failures > 0))
