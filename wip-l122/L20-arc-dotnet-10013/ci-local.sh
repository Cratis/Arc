#!/usr/bin/env bash
# Local mirror of .github/workflows/dotnet-build.yml (Tier 1). One phase per invocation so exit codes stay honest.
# Order as CI: yarn, viewer, build, then test per project.
# Usage: .ai-work/ci-local.sh build            -> dotnet build --configuration Release (whole Arc.slnx, CI warnings)
#        .ai-work/ci-local.sh test <SpecsDir>  -> dotnet test <dir> --no-build -c Release -f net10.0
#        .ai-work/ci-local.sh specs-list       -> the spec projects CI runs (excluding ProxyGenerator.Specs, run per namespace in CI)
# Tier 1 = build + every affected spec project (Chronicle.Specs, Arc.Core.Specs, Arc.Specs, Chronicle.CodeAnalysis.Specs for this lane).
# Tier 2 = Tier 1 + ProxyGenerator.Specs when proxies change, + JS `yarn ci` when Source/JavaScript changes.
set -euo pipefail
cd "$(dirname "$0")/.."
case "${1:-}" in
  yarn) exec yarn install --immutable ;;
  viewer) cd Source/DotNET/Screenplay.Embedded && npm ci --workspaces=false && npm run typecheck && npm test && npm run build && exec node --test ../../../scripts/viewer-bundle.test.mjs ;;
  build) exec dotnet build --configuration Release "${@:2}" ;;
  aot) python3 scripts/aot-ratchet.py --self-test && exec python3 scripts/aot-ratchet.py ;;
  test) exec dotnet test "$2" --no-build --configuration Release --framework net10.0 ;;
  e2e)
    # Mirrors the screenplay-end-to-end job: six generate-and-bind runs. Needs a prior Release build.
    for p in AspNetCore Chronicle Layered; do dotnet restore Samples/$p/$p.csproj >/dev/null; done
    out=.ai-work/e2e; mkdir -p $out; dll=Source/DotNET/Screenplay.EndToEnd/bin/Release/net10.0/Cratis.Arc.Screenplay.EndToEnd.dll
    dotnet $dll Samples/AspNetCore/AspNetCore.csproj $out/AspNetCore.play
    dotnet $dll Samples/AspNetCore/AspNetCore.csproj $out/AspNetCore-authoring.play --authoring-only-constructs
    dotnet $dll Samples/Chronicle/Chronicle.csproj $out/Chronicle.play
    dotnet $dll Samples/Chronicle/Chronicle.csproj $out/Chronicle-authoring.play --authoring-only-constructs
    dotnet $dll Samples/Layered/Layered.csproj $out/Layered.play Samples/Layered/Layered.expected
    exec dotnet $dll Samples/Layered/Layered.csproj $out/Layered-authoring.play --authoring-only-constructs ;;
  specs-list) find Source/DotNET -name "*.Specs.csproj" ! -name "ProxyGenerator.Specs.csproj" | sed 's|/[^/]*\.csproj$||' | sort ;;
  *) echo "usage: $0 build|test <dir>|specs-list" >&2; exit 2 ;;
esac
