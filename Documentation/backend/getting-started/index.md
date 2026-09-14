---
title: Set up a standalone Arc backend
description: Create an ASP.NET Core project with Arc, choose MongoDB or SQLite through EF Core, and configure Debug proxy generation without Chronicle.
---

Start with a backend you can understand before adding a UI. This path uses **ASP.NET Core and standalone Arc**: no Chronicle package, event store, or Cratis template is required. The [lightweight Arc.Core host](../core/getting-started.md) is a separate hosting option; do not mix its bootstrap with this one.

## Create the project

Install a .NET SDK compatible with the Arc release you select, plus Node.js for the later React step. **This tutorial targets .NET 10.** The application's target framework and the SDK required to build Arc's analyzers are separate choices; see the [SDK prerequisites](https://github.com/Cratis/Arc#contributing). Use matching released versions of the Arc packages; retain the resolved versions in your project and lockfiles.

```bash
dotnet new web -n Library --framework net10.0
cd Library
dotnet add package Cratis.Arc
dotnet add package Cratis.Arc.ProxyGenerator.Build
```

Keep `Library.csproj` and `Program.cs` at the project root. Backend feature files will live under `src/Authors/`, beside the frontend files added later. This co-location is a tutorial convention, not an Arc routing requirement.

Add this property group inside `Library.csproj`:

```xml
<PropertyGroup>
    <CratisProxiesOutputPath>$(MSBuildThisFileDirectory)src</CratisProxiesOutputPath>
    <CratisProxiesUseSourceFileAsOutputFile>true</CratisProxiesUseSourceFileAsOutputFile>
</PropertyGroup>
<ItemGroup>
    <NamespaceRoot Include="library" Namespace="Library" Folder="" />
</ItemGroup>
```

The namespace root removes `Library` from **output folders only**, placing `Library.Authors` under `src/Authors`; API routes still retain the default `/api/library/...` prefix. Source-file mode groups query proxies into their read model's file (`Author.ts`) instead of separate query files (`AllAuthors.ts`). When following a frontend example using separate query files, import `AllAuthors` from `./Authors/Author` for this setup.

The generator runs **after compilation** and uses the assembly and PDB source paths. An output path enables it; installing Arc alone does not. Use Debug builds for the following checkpoints. See [proxy configuration](../proxy-generation/configuration.md) before changing routes or output layout.

Create `GlobalUsings.cs` at the project root:

```csharp
global using System.Reactive.Subjects;
global using Cratis.Arc;
global using Cratis.Arc.Commands;
global using Cratis.Arc.Commands.ModelBound;
global using Cratis.Arc.Queries;
global using Cratis.Arc.Queries.ModelBound;
global using Cratis.Arc.Validation;
global using Cratis.Concepts;
global using FluentValidation;
global using Library.Authors;
```

These imports are explicit because this project does not use the `Cratis` integration meta-package's global usings. The `Library.Authors` namespace is introduced in the next lesson; finish that lesson before building.

## Choose MongoDB

Use this branch **or** the EF Core branch below, not both for the same tutorial models.

```bash
dotnet add package Cratis.Arc.MongoDB
```

Add `global using MongoDB.Driver;` to `GlobalUsings.cs`. Supply these local-development settings in `appsettings.Development.json`:

```json
{
    "Cratis": {
        "MongoDB": {
            "Server": "mongodb://localhost:27017/?replicaSet=rs0&directConnection=true",
            "Database": "arc-library"
        }
    }
}
```

Live queries need a replica set (or a supported sharded deployment), not a standalone `mongod`. For an isolated local Docker instance:

```bash
docker run --name arc-library-mongo -d -p 127.0.0.1:27017:27017 mongo:8 --replSet rs0 --bind_ip_all
docker exec arc-library-mongo mongosh --eval 'rs.initiate({_id:"rs0",members:[{_id:0,host:"localhost:27017"}]})'
docker exec arc-library-mongo mongosh --eval 'db.hello().isWritablePrimary'
```

Wait until Mongo accepts connections before running `rs.initiate`; then repeat the final check until it prints `true`. This unauthenticated, loopback-only database is **development-only**. Use your deployment's access controls and connection secrets in production. Arc discovers collections and its serializers; no per-collection registration is required.

Replace `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddCratisArc();
builder.UseCratisMongoDB();

var app = builder.Build();
app.UseCratisArc();
await app.RunAsync();
```

Database/collection creation happens on the first write. The validation chapter adds a unique index explicitly; the framework does not create that business constraint for you.

## Or choose EF Core with SQLite

This local alternative needs no database container:

```bash
dotnet add package Cratis.Arc.EntityFrameworkCore
```

Add these imports to `GlobalUsings.cs` instead of the MongoDB import:

```csharp
global using Cratis.Arc.EntityFrameworkCore;
global using Microsoft.EntityFrameworkCore;
```

Replace `Program.cs` with this version. The next lesson supplies `LibraryDbContext`:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddCratisArc(configureBuilder: arc => arc.WithEntityFrameworkCore(
    options => options.ConnectionString = "Data Source=library.db"));

var app = builder.Build();
app.UseCratisArc();

// Tutorial bootstrap for a new, disposable database only.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
    await db.Database.EnsureCreatedAsync();
}

await app.RunAsync();
```

The nonempty connection string enables automatic discovery of public `BaseDbContext` subclasses. Arc recognizes SQLite from `Data Source=library.db`, configures the provider and observation interceptor, and registers the context. **Do not add a redundant manual `AddDbContext` registration.**

`EnsureCreatedAsync` creates an initial schema; it does **not** upgrade an existing one. Later chapters change the schema. For this disposable lesson, stop the host, remove only your tutorial `library.db`, and restart after those changes. For retained data, use [EF migrations](../entity-framework/migrations-add-columns.md) instead; do not mix `EnsureCreated` with a migration-managed database.

SQLite observation reports writes made through the configured context in this host. It does not detect arbitrary external-process writes. Other providers have additional [database notification prerequisites](../entity-framework/observing.md).

## Next checkpoint

The project is configured but its domain types are not defined yet. Continue to [Your first command and query](/arc/backend/getting-started/your-first-command/), choose the same database branch, then build and run. That page gives the observable success checkpoint before you add React.

The [full-stack tutorial](/arc/tutorial/) continues from that backend. The `dotnet new cratis` template is an optional **Arc + Chronicle** scaffold, not a prerequisite for this standalone path; use [Chronicle integration](../chronicle/index.md) when you intentionally choose event sourcing.
