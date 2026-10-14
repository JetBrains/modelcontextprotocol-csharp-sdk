# Sourced by build.sh and verify.sh: defines native() and $dotnet, bootstrapping the .NET SDK when none is given.
#   JB_DOTNET names a dotnet to use as is, for example JB_DOTNET=dotnet for the one on PATH.
#   Inside dotnet-sdk.cmd, DOTNET_HOST_PATH names the SDK it bootstrapped.
#   Otherwise the sourcing script runs itself again under jetbrains/dotnet-sdk.cmd.
#   That one downloads its pinned SDK once into the user profile, so a build needs nothing installed beforehand.

# Git Bash paths are POSIX; dotnet.exe takes them in Windows form, which cygpath gives.
native() { if command -v cygpath > /dev/null; then cygpath -w "$1"; else printf '%s' "$1"; fi; }

if [ -n "${JB_DOTNET:-}" ]; then
  dotnet=$JB_DOTNET
elif [ -n "${DOTNET_HOST_PATH:-}" ]; then
  dotnet=$DOTNET_HOST_PATH
  if command -v cygpath > /dev/null; then dotnet=$(cygpath -u "$dotnet"); fi
  # dotnet-sdk.cmd forces DOTNET_ROLL_FORWARD=Major on what it runs; a value the caller set on purpose comes back here.
  if [ -n "${JB_CALLER_DOTNET_ROLL_FORWARD+set}" ]; then export DOTNET_ROLL_FORWARD=$JB_CALLER_DOTNET_ROLL_FORWARD; fi
else
  sdk="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/dotnet-sdk.cmd"
  # The copy of dotnet-sdk.cmd stays verbatim, as Rider ships the same script to user machines, where forcing Major is what it is for.
  # So a DOTNET_ROLL_FORWARD the caller set, such as the LatestMajor of verify.sh, is carried over the re-exec instead.
  if [ -n "${DOTNET_ROLL_FORWARD+set}" ]; then export JB_CALLER_DOTNET_ROLL_FORWARD=$DOTNET_ROLL_FORWARD; fi
  if command -v cygpath > /dev/null; then
    # On Windows the cmd half of dotnet-sdk.cmd runs, and it runs what follows "--" as a cmd command line.
    # The bin launcher of Git Bash, unlike usr/bin/bash.exe, sets up its PATH when started from cmd.
    exec cmd //c "$(cygpath -w "$sdk")" -- "$(cygpath -w /)bin\\bash.exe" "$(cygpath -w "$(realpath "$0")")" "$@"
  else
    # The POSIX half evals what follows "--", so every word is quoted for the shell once more.
    exec "$sdk" -- "$(printf '%q ' "$(realpath "$0")" "$@")"
  fi
fi
