# Arc samples

These projects exercise Arc's hosting modes, generated clients, validation, authorization, observable queries, and optional Chronicle integration. They are sample applications, not framework packages.

## Embedded event-model explorer

The **AspNetCore** and **Chronicle** samples include `Screenplay.Embedded`. In Debug builds, the Roslyn/MSBuild extension generates `.play` documents and embeds them in the compiled assemblies. AspNetCore maps the explorer explicitly in Development; Chronicle uses the Cratis metapackage's automatic debug-build hosting.

When working from this repository, first build its colocated viewer:

```bash
cd Source/DotNET/Screenplay.Embedded
npm ci --workspaces=false
npm run build
```

From the repository root, run the ASP.NET Core sample using its usual local database/authentication configuration:

```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run --project Samples/AspNetCore/AspNetCore.csproj --configuration Debug
```

Open `/.cratis/event-model/` on the backend's address. The hierarchy contains the ASP.NET Core assembly and the shared sample artifacts, each with its assembly-wide and scoped documents. The shared library generates resources without taking an ASP.NET Core runtime dependency, so the Arc.Core and MAUI samples keep their existing hosting dependencies.

The Chronicle sample uses the same endpoint after its usual Chronicle setup:

```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run --project Samples/Chronicle/Chronicle.csproj --configuration Debug
```

Select a document to see the `EventModelBoard` visualization or the complete generated Screenplay source. The backend runs the Screenplay compiler with visitor hooks to produce the board model; the browser does not infer the model from source. Conversion warnings identify anything the board cannot display.

Release builds disable embedded generation in these samples. To exercise generation explicitly in a Release build, use:

```bash
dotnet build Samples/AspNetCore/AspNetCore.csproj --configuration Release -p:CratisEmbeddedScreenplayEnabled=true
```

AspNetCore's explicit endpoint remains unmapped outside Development. Chronicle's automatic endpoint depends on whether its application assembly is debug-built, not its environment name; optimized Release builds do not expose it automatically. See [Browse your embedded event model](../Documentation/backend/csharp/embedded-event-model.md) for installation in an application, namespace conventions, and authorization.

## Other examples

- `ArcCore` and `Core` demonstrate Arc's standalone host.
- `Shared` holds artifacts reused by the ASP.NET Core, Arc.Core, and MAUI applications.
- `Layered` and `Layered.Contracts` are multi-project source-analysis examples.
- [MAUI](Maui/README.md) demonstrates desktop embedding and requires the appropriate platform workload.

### Publishing ArcCore trimmed or NativeAOT

`CratisSamplePublish` selects a publish variant with reflection-based JSON disabled. From the repository root:

```bash
dotnet publish Samples/ArcCore/ArcCore.csproj -c Release -f net10.0 -r osx-arm64 -p:CratisSamplePublish=Untrimmed -o .ai-work/2859/publish/untrimmed
dotnet publish Samples/ArcCore/ArcCore.csproj -c Release -f net10.0 -r osx-arm64 -p:CratisSamplePublish=Trimmed -o .ai-work/2859/publish/trimmed
dotnet publish Samples/ArcCore/ArcCore.csproj -c Release -f net10.0 -r osx-arm64 -p:CratisSamplePublish=NativeAot -o .ai-work/2859/publish/nativeaot
```

Build the acceptance runner with `dotnet build Samples/ArcCore.Specs/ArcCore.Specs.csproj --configuration Release -p:CratisSpecsTargetFramework=net10.0`. When `CRATIS_ARC_PUBLISHED_ARCCORE` is unset, all 12 acceptance facts are skipped, including during solution-wide test runs. Setting it to a missing or invalid executable makes the facts fail rather than skip. Set it to the absolute executable path and, optionally, set `CRATIS_ARC_PUBLISHED_ARCCORE_LOG` to a file for captured server output. Run each variant separately:

```bash
CRATIS_ARC_PUBLISHED_ARCCORE="$PWD/.ai-work/2859/publish/untrimmed/ArcCore" \
CRATIS_ARC_PUBLISHED_ARCCORE_LOG="$PWD/.ai-work/2859/untrimmed-app.log" \
Samples/ArcCore.Specs/bin/Release/net10.0/Cratis.Arc.Samples.ArcCore.Specs -noLogo -noColor -result-trx "$PWD/.ai-work/2859/untrimmed.trx"
```

This is not a CI gate yet; see [#2859](https://github.com/Cratis/Arc/issues/2859). Trimmed and NativeAOT runs may expose library incompatibilities; the untrimmed variant is the control.
