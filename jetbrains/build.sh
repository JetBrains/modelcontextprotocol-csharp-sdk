#!/usr/bin/env bash
# Builds and packs the JetBrains fork flavor: netstandard2.0 only, against the JetBrains main solution package lock.
# usage: jetbrains/build.sh <step> <path-to-MainSolutionPackageVersions.xml> [extra dotnet args...]
#   all      builds and packs in one go
#   build    only builds, so the assemblies in artifacts/jetbrains/bin can be signed in place
#   pack     packs what build left there, without building again
# Packages land in artifacts/jetbrains/packages.
# Runs under Git Bash on Windows too.
set -euo pipefail
# Git Bash paths are POSIX; dotnet.exe takes them in Windows form, which cygpath gives.
native() { if command -v cygpath > /dev/null; then cygpath -w "$1"; else printf '%s' "$1"; fi; }
step=$1; shift
lock=$(realpath "$1"); shift
fork=$(cd "$(dirname "$0")" && pwd)
root=$(dirname "$fork")
out="$root/artifacts/jetbrains"
projects=(
  src/ModelContextProtocol.Core/ModelContextProtocol.Core.csproj  # client and server, stdio and HTTP client transports
)
case "$step" in
  all)     command=(pack) ;;
  build)   command=(build) ;;
  pack)    command=(pack --no-build) ;;
  *)       echo "Unknown step '$step'; expected all, build or pack." >&2; exit 2 ;;
esac
for proj in "${projects[@]}"; do
  args=(
    -c Release
    -p:TargetFrameworks=netstandard2.0  # one asset serves netfx and netcore alike, as the main solution's bin dir needs
    -p:CustomAfterMicrosoftCommonTargets="$(native "$fork/JetBrains.Fork.targets")"  # the inner, per-framework evaluation
    -p:CustomAfterMicrosoftCommonCrossTargetingTargets="$(native "$fork/JetBrains.Fork.targets")"  # the outer evaluation, where restore reads its settings
    -p:JbPackageLockFile="$(native "$lock")"
    -p:ArtifactsDir="$(native "$out")/"  # keeps the fork's restore and obj apart from an upstream build of the same tree
  )
  if [ "$step" = all ] || [ "$step" = pack ]; then args+=(-o "$(native "$out/packages")"); fi
  dotnet "${command[@]}" "$(native "$root/$proj")" "${args[@]}" "$@"
done
