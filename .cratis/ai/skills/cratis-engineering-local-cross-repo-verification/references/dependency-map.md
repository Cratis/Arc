<!-- cratis-ai-managed: skills/cratis-engineering-local-cross-repo-verification/references/dependency-map.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Dependency map

`scripts/dependency-map.tsv` is a reference table of the producer-to-consumer edges between
Cratis repositories, the pack targets of each producer and the package IDs each ships.
`cratis-local-packages.sh` does not read it: use it to decide which repositories to pack,
in which order, and which IDs to expect in the feed. It holds no versions; read pins live
from the consumer manifests (`Directory.Packages.props`, `package.json`, `Dockerfile`).

## Rows

Tab-separated, `#` starts a comment.

| Row | Fields | Meaning |
| --- | --- | --- |
| `pack` | repo, eco, target, `PROP=VAL,...` | A pack target; the optional MSBuild properties are the `-p:` arguments the producer's own publish workflow passes |
| `ids` | repo, eco, comma-separated IDs | The packages the producer ships in that ecosystem |
| `uses` | consumer, producer, eco, manifest | An edge; the manifest is the pin file, or the Yarn project directory for npm |
| `image` | repo, dockerfile, tag suffix | A producer image a consumer builds on |

`eco` is `nuget`, `npm` or `image`. For npm, `.` means the repository root.

## Using it

- Find every `uses` row whose producer is the repository you changed; those consumers
  pin its packages. A consumer may also reach the producer through another repository
  (Fundamentals reaches Chronicle through Arc as well as directly).
- Pack upstream first. Overlay the earlier producer into the next repository with
  `use-nuget`, pack that, then `restore` it. Use one stamp for the whole chain: `stamp`
  takes one producer, so generate it once and reuse the string.
- Pack properties from a `pack` row are not accepted by `pack-nuget`. When a row has them,
  run `dotnet pack TARGET -c Release -p:PackageVersion=$stamp -p:Version=$stamp -p:PROP=VAL
  -o "$CRATIS_LOCAL_FEED/nuget"` yourself, with the properties as `-p:` arguments and never
  as exported environment variables (an exported `IsPackaging=true` reaches Chronicle's
  nested `dotnet run` code generator and fails with a multi-target error).
- After packing, list the feed for the stamp and compare it with the `ids` row; a missing ID
  is a silent partial overlay. Open the nuspec of each packed ID and check that every locally
  changed dependency is at the stamp.

## Facts about the chains

- Arc and Chronicle depend on each other. Arc's Chronicle-facing packages are built against
  a released Chronicle, and Chronicle's clients against a released Arc. The same holds for
  Chronicle and Orleans. Record the nuspec dependencies and do not break the cycle.
- Image and package pins are coupled. A consumer that builds `FROM` a producer image may
  derive the tag from the producer's NuGet pin; check its publish workflow before tagging a
  local image.
- A repository you did not name keeps its released version: Stage reaches Arc, Chronicle
  reaches Orleans. A binary break through that hop is a finding about the consumer's pins.
- The current Screenplay rows list only npm IDs although Stage and Studio consume its NuGet
  packages; read the IDs from the producer's publish workflow.

## Extend it

Add `pack` rows only from the producer's publish workflow on the default branch, not a
stale clone. Add `ids` for every package a consumer must resolve. Add one `uses` row per
consumer manifest that pins the producer.
