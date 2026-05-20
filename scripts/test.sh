#!/usr/bin/env bash
# Reliable local test run on macOS / CI: avoids MSBuild named-pipe worker issues in some environments.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

export MSBUILDDISABLENODEREUSE="${MSBUILDDISABLENODEREUSE:-1}"
export DOTNET_CLI_TELEMETRY_OPTOUT="${DOTNET_CLI_TELEMETRY_OPTOUT:-1}"

FILTER="${1:-}"

if [[ -n "$FILTER" ]]; then
  dotnet test Cobryx.Application.Tests/Cobryx.Application.Tests.csproj \
    --filter "$FILTER" --verbosity minimal
  dotnet test Cobryx.Domain.Tests/Cobryx.Domain.Tests.csproj \
    --filter "$FILTER" --verbosity minimal
  dotnet test Cobryx.Integration.Tests/Cobryx.Integration.Tests.csproj \
    --filter "$FILTER" --verbosity minimal
else
  dotnet test Cobryx.Application.Tests/Cobryx.Application.Tests.csproj --verbosity minimal
  dotnet test Cobryx.Domain.Tests/Cobryx.Domain.Tests.csproj --verbosity minimal
  dotnet test Cobryx.Integration.Tests/Cobryx.Integration.Tests.csproj --verbosity minimal
fi
