---
title: Set up a standalone Arc backend
description: Create an ASP.NET Core project with Arc, start with SQLite through EF Core or choose MongoDB, and configure Debug proxy generation without Chronicle.
---

Set up an ASP.NET Core backend for a library app and configure generated TypeScript clients before adding a UI. This is standalone Arc: no Chronicle package, event store, or Cratis template is required. Use **SQLite through EF Core** for a first run without a database server; MongoDB is an alternative below. Choose one storage branch and keep it through the next lesson. You'll define the author command and query there, then check that a write can be read back.

Use this page for ASP.NET Core. The [lightweight Arc.Core host](../core/getting-started.md) has a separate setup; do not mix its bootstrap with this one.

## Create the project

Install **.NET SDK 10.0.400 or newer in the .NET 10 line**. The target framework alone is not enough: Arc's analyzers require that SDK feature band. This walkthrough uses .NET SDK **10.0.401** and released Arc packages **22.156.2**. Keep these versions for the first run rather than mixing releases.

```bash
dotnet new web -n Library --framework net10.0
cd Library
dotnet add package Cratis.Arc --version 22.156.2
dotnet add package Cratis.Arc.ProxyGenerator.Build --version 22.156.2
```

Keep `Library.csproj` and `Program.cs` at the project root. Backend feature files will live under `src/Authors/`, beside the frontend files added later. This co-location is a tutorial convention, not an Arc routing requirement. Keep the resolved package versions in the project and retain the frontend lockfile when you add React.

Add this property group and item group inside `Library.csproj`:

```xml
<PropertyGroup>
    <CratisProxiesOutputPath>$(MSBuildThisFileDirectory)src</CratisProxiesOutputPath>
    <CratisProxiesUseSourceFileAsOutputFile>true</CratisProxiesUseSourceFileAsOutputFile>
</PropertyGroup>
<ItemGroup>
    <NamespaceRoot Include="library" Namespace="Library" Folder="" />
</ItemGroup>
```

This configuration keeps generated clients beside the feature code. The namespace root removes `Library` from **output folders only**, placing `Library.Authors` under `src/Authors`; API routes still retain the default `/api/library/...` prefix. Source-file mode groups query proxies into their read model's file (`Author.ts`) instead of separate query files (`AllAuthors.ts`). When following a frontend example using separate query files, import `AllAuthors` from `./Authors/Author` for this setup.

A proxy is the generated TypeScript class that calls a backend command or query. The generator runs after compilation, reading the assembly and its PDB debugging information to locate source files. Setting an output path enables it; installing Arc alone does not. Use Debug builds for the following checkpoints, and regenerate after backend contract changes before type-checking the frontend. See [proxy configuration](../proxy-generation/Configuration/index.md) before changing routes or output layout.

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

These imports are explicit because this project does not use the `Cratis` integration meta-package's global usings. The `Library.Authors` namespace is introduced in the next lesson; **finish that lesson before building**.

## Start with EF Core and SQLite

This is the default branch for the walkthrough. It needs no database container:

```bash
dotnet add package Cratis.Arc.EntityFrameworkCore --version 22.156.2
```

Add these imports to `GlobalUsings.cs`:

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

The nonempty connection string enables automatic discovery of public `BaseDbContext` subclasses. Arc recognizes SQLite from `Data Source=library.db`, configures the provider and observation interceptor, and registers the context. **Do not add a redundant manual `AddDbContext` registration.** The database file is relative to the project directory from which you run the host.

`EnsureCreatedAsync` creates an initial schema; it does **not** upgrade an existing one. Later chapters change the schema. For this disposable lesson, stop the host, remove only your tutorial `library.db`, and restart after those changes. For retained data, use [EF migrations](../entity-framework/migrations-add-columns.md) instead; do not mix `EnsureCreated` with a migration-managed database.

SQLite observation reports writes made through the configured context in this host. It does not detect arbitrary external-process writes. Other providers have additional [database notification prerequisites](../entity-framework/observing.md).

**Continue to [Your first command and query](./your-first-command.mdx)** and choose its EF Core tab. Skip the MongoDB branch below.

## Or choose MongoDB instead

Use this branch **instead of** the EF Core package, imports, and `Program.cs` above. It requires Docker or an existing MongoDB replica set.

```bash
dotnet add package Cratis.Arc.MongoDB --version 22.156.2
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

Wait until Mongo accepts connections before running `rs.initiate`; then repeat the final check until it prints `true`. If the container exits immediately, run `docker logs arc-library-mongo`. MongoDB 8 refuses to start on Linux kernels 6.19 through 7.0.13, which includes some Docker Desktop virtual machines; the log then names the kernel incompatibility. Update Docker Desktop, or remove the container and start it from `mongo:7.0` instead, which runs this tutorial unchanged. See MongoDB's [production notes](https://www.mongodb.com/docs/upcoming/administration/production-notes) for the affected kernels. This unauthenticated, loopback-only database is **development-only**. Use your deployment's access controls and connection secrets in production. Arc discovers collections and its serializers; no per-collection registration is required.

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

## Next checkpoint

You have configured the host, storage, and proxy output. The project still needs the author types referenced by the setup, so continue to [Your first command and query](/arc/backend/csharp/getting-started/your-first-command/) before building. Keep the same database branch. That lesson supplies the missing types and a curl write/read check, so you can verify the backend before adding React.

The [full-stack tutorial](/arc/tutorial/) continues from that backend. The `dotnet new cratis` template is an optional **Arc + Chronicle** scaffold, not a prerequisite for this standalone path; use [Chronicle integration](../chronicle/index.md) when you intentionally choose event sourcing.
