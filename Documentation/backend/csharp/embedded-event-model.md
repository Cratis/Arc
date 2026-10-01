---
title: Browse your embedded event model
description: Generate Screenplay documents during compilation and expose a read-only event-model explorer in an ASP.NET Core application.
---

To inspect the model behind a running application without keeping a separate diagram up to date, embed its generated Screenplay documents during the build. `Cratis.Arc.Screenplay.Embedded` adds a read-only explorer at `/.cratis/event-model` with project, module, and feature navigation.

This is source-derived documentation, not a view of stored events or live application data. Use it to review a feature's command/event/read-model flow, orient yourself in an unfamiliar codebase, or compare a scoped feature with the whole application without maintaining a second diagram. It uses the same analysis as [Screenplay generation](generating-a-screenplay.md); its limitations and diagnostics still apply.

Arc does not require event sourcing. An ordinary command that returns a response has no event card. Commands that produce recognized Chronicle events show those events, including events declared in another slice; state-view slices show the events they consume.

## Open the explorer in a Cratis application

The `Cratis` metapackage includes the embedded-generation package. In an existing ASP.NET Core host configured with `AddCratis` and activated with `UseCratis`, build and run in Debug, then open `/.cratis/event-model/`. The build creates and embeds the documents automatically; `UseCratis` maps the viewer automatically for a debug-built application.

The automatic check uses the application's `DebuggableAttribute` and whether optimizations are disabled, not the ASP.NET environment name. Release-built applications do not expose the viewer automatically, even in the Development environment. Through the metapackage, Release builds also disable generation by default.

:::caution[Debug detection is not authorization]
A debug-built application can expose its structure even when its environment is named Production. Disable the viewer or require authorization if the application is reachable beyond your development machine.
:::

To opt out, configure services before building the host:

```csharp
builder.Services.AddCratisEventModelViewer(options => options.Enabled = false);
```

For Arc-only hosts, or when you want to choose the exposed assemblies and policy yourself, use the explicit setup below.

## Map the explorer explicitly

In an ASP.NET Core application that already declares Arc commands or queries, add the package:

```bash
dotnet add package Cratis.Arc.Screenplay.Embedded
```

Map the explorer in your existing `Program.cs` after building the application. This is a complete minimal host; retain your application's existing Arc setup when adding the mapping:

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapCratisEventModel();
}

app.Run();
```

Build and start your application, then open `/.cratis/event-model/`. Select a document in the hierarchy to view its event-model board or its generated `.play` source. The viewer is bundled in the package; your application's users do not need to install Node.js.

Outside automatic metapackage hosting, nothing is mapped until you call the extension. By default it reads the entry assembly. For an application whose model lives in other assemblies, select them explicitly:

```csharp
app.MapCratisEventModel(typeof(MyApplicationMarker).Assembly);
```

`MyApplicationMarker` is a type in the assembly containing your application artifacts. Each selected assembly must have been built with the embedded-generation package enabled. Documents remain assembly-scoped; this does not merge multiple projects into one model.

## Keep it out of release builds

Endpoint mapping and resource generation are separate switches. An unmapped explorer cannot be requested, but generated resources still reside in the assembly unless you disable generation.

To generate documents only in Debug, add this to your application project:

```xml
<PropertyGroup>
    <CratisEmbeddedScreenplayEnabled Condition="'$(Configuration)' != 'Debug'">false</CratisEmbeddedScreenplayEnabled>
</PropertyGroup>
```

A direct reference to `Cratis.Arc.Screenplay.Embedded` enables generation by default; the `Cratis` metapackage disables it in Release unless you explicitly set `CratisEmbeddedScreenplayEnabled` to `true`. Generation does not run during design-time builds. A real build regenerates the resources from the current source; disabling generation prevents previously generated files from being included.

## Understand the hierarchy

Namespace placement is relative to your project's `RootNamespace`. Set it explicitly when your assembly name and application namespace differ:

```xml
<PropertyGroup>
    <RootNamespace>Company.Library</RootNamespace>
</PropertyGroup>
```

For example:

| Namespace | Placement |
| --- | --- |
| `Company.Library.Catalog.Authors.Registration` | Module `Catalog`, feature `Authors`, slice `Registration` |
| `Company.Library.Catalog.Books.Listing` | Module `Catalog`, feature `Books`, slice `Listing` |
| `Company.Library.Accounts.SignIn` | Rooted feature `Accounts`, slice `SignIn` |

The first segment after the root is a module when it contains feature namespaces beneath it. If its children are only namespaces identified as slices, it is a rooted feature rather than a module. Nested features remain nested. The assembly document includes everything; scoped documents describe the selected module or feature and its descendants.

Screenplay's current grammar requires a module container around features. Rooted features use the application's root container in the `.play` text; they remain direct children of the assembly in the explorer rather than being promoted to modules.

Slice identification comes from recognized application artifacts, not a fixed list of folder names. Shared supporting types do not turn an arbitrary namespace into a slice. Cross-scope event references remain explicit dependencies in scoped documents.

## Protect the explorer

The documents expose your application's structure. Development-only mapping is the simplest default. If you expose the explorer elsewhere, apply your application's authorization policy to the returned route group:

```csharp
app.MapCratisEventModel().RequireAuthorization("Developers");
```

This assumes your host has already registered authentication and the `Developers` policy. The group protects the viewer, static assets, and every document query together. Explicit and automatic mapping reuse the same group, so adding authorization does not create a second unprotected copy.

For automatic hosting, configure the same policy during service registration:

```csharp
builder.Services.AddCratisEventModelViewer(options =>
{
    options.RequireAuthorization = true;
    options.AuthorizationPolicy = "Developers";
});
```

`Enabled = true` explicitly opts into automatic hosting for an optimized build; that does not generate documents. Set `CratisEmbeddedScreenplayEnabled` to `true` as well if you want resources in Release. `Assemblies` lets you name additional application assemblies; automatic selection otherwise uses the entry assembly and its already-loaded, directly referenced application assemblies, not framework assemblies or arbitrary loaded libraries.

## Explore the model

The solid sidebar lists each assembly and its module/feature documents. Selecting a document opens a read-only board to the right; its initial camera centers and fits the model. The lower-right rounded toolbar controls zoom and the minimap. Pan and zoom freely after opening a document: the initial fit does not keep overriding your camera.

![Read-only event-model board with command and event cards, the solid project sidebar, and lower-right zoom controls](images/embedded-event-model-board.png)

Open **View** in the upper right to choose:

- **Full** or **Overview** detail level.
- **Properties** to show or hide card properties.
- **Arrows** or **Lines** visualization.

These choices are stored in your browser's local storage and restored on the next visit. If the browser blocks persistence, the viewer applies the choice but displays a warning that it could not remember it.

![Upper-right View menu showing detail level, property visibility, and visualization choices](images/embedded-event-model-view-options.png)

Choose **Source** to inspect the generated Screenplay for the selected document. This is useful when a conversion warning says a declaration or mapping cannot be represented on the board.

![Source tab showing the generated Screenplay for the selected Authors feature](images/embedded-event-model-source.png)

## Query the documents

All paths below are relative to `/.cratis/event-model/`:

| GET path | Response |
| --- | --- |
| `hierarchy` | Selected projects and their document hierarchy |
| `documents/{projectId}/{documentId}/source` | Generated `.play` text |
| `documents/{projectId}/{documentId}/model` | Read-only canvas model with conversion warnings |

Use the IDs returned by `hierarchy` and URL-encode each path segment. Unknown projects, documents, and assets return HTTP 404. These queries read embedded resources only; they do not accept filesystem paths or fetch remote documents.

The board is a visualization, not another inference engine. Where its card model cannot show everything a Screenplay document expresses, conversion warnings make that limitation visible. Use the source view for the full generated document. Generation errors fail the build instead of embedding a document that the Screenplay compiler rejects.
