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
