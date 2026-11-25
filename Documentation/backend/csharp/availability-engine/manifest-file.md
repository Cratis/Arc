---
title: The availability manifest file
description: The one file format that describes audiences, blocks, gates, context values, parts and suspendable parts for every Arc implementation, the shared test vectors any engine runs, and how to write, read and evaluate it from .NET.
---

The availability manifest file describes what an application declares about availability, as data. Where the
[availability manifest](manifest.md) is the in-memory description of a model, the file is the interchange format: every Arc
implementation (.NET, and the others that follow) writes and reads the same JSON, so tools, the operator and the contract
tests of every engine agree on what is declared.

The format, its JSON schema and the test vectors live in `Specifications/availability` of the Arc repository. That folder is the
contract; this page describes it and shows the .NET API.

## The format

The file is a JSON object. `format` is always `cratis-availability-manifest` and `version` is `1`. Every collection is present, empty when there is nothing to list.

| Property | Holds |
| --- | --- |
| `origin` | What wrote the file; empty when unknown. |
| `audiences` | `name` and `description` of each audience. |
| `blocks` | Named gates that `with` reuses: `name`, `description` and `gate`. |
| `conditions` | The conditions that are code in the engine that wrote the file: `name` and `description`. They are listed so a reader can show them; they cannot be evaluated from the file. |
| `contextValues` | The values a gate can read: `name`, `scope` (module, feature and slice joined with `/`), `required` and `origin`. |
| `scopes` | The namespaces (modules, features and slices) that carry a gate: `namespace` and `gate`. |
| `parts` | Every command and read model: `name` (full name), `kind` (`command` or `readModel`), `module`, `feature`, `slice` and `gates`, the gates down its path, outermost first. Every gate must hold. |
| `suspendable` | What an operator can suspend: `kind` (`module`, `feature`, `slice` or `part`), `path` and `name`. |

A gate is an expression tree. Each node has a `kind`:

| `kind` | Properties | Holds when |
| --- | --- | --- |
| `for` | `audience` | the audience is held |
| `not` | `audience` | the audience is not held |
| `when` | `condition` | the condition holds |
| `with` | `block` | the named block holds |
| `all` | `items` | every item holds |
| `any` | `items` | any item holds |

A condition has a `kind` too. `compare` reads a context path and compares it with literals: `path`, `operator` (`isTrue`, `equal`,
`notEqual`, `in`, `notIn`, `greaterThan`, `greaterThanOrEqual`, `lessThan`, `lessThanOrEqual`) and `operands`. `all` and `any` combine
`items`. `named` is a condition that is code (`name`, `description`).

```json
{
  "name": "Fleet.Vessels.Dock.DockVessel",
  "kind": "command",
  "module": "Fleet",
  "feature": "Vessels",
  "slice": "Dock",
  "gates": [
    { "kind": "for", "audience": "Plus" },
    { "kind": "when", "condition": { "kind": "compare", "path": "arguments.vesselId", "operator": "in", "operands": ["v-17", "v-18"] } }
  ]
}
```

## Deciding from a manifest

An engine decides in this order. It is the contract, and the vectors check it.

1. **Suspension first.** The part path is the module, feature and slice that are not empty, then the full name, joined with `/`. A suspended path covers a part when it is that path, a whole-segment prefix of it, or the full name of the part. A covered part is `Suspended` whatever its gates say.
2. **Gates down the path.** Every gate in `gates` must hold. No gates means available.
3. **Unknown denies.** A value that is absent is unknown for every operator, including `notEqual` and `notIn`. Only available opens a gate, so unknown is `NotAvailable`.
4. **The server's context wins.** `tenant` and `identity.user` come from the server and are never replaced by `arguments`.

## The shared test vectors

`Specifications/availability/vectors` holds data-driven cases. Each file has a manifest and a list of cases. A case names a `part`, a `context` (`tenant`, `user`, `audiences`, `suspended`, `values`), optional `arguments` and what to `expect`: `available`, and for a refusal the `reason`. Any engine runs every file and fails when it finds none; the README in that folder lists the steps.

The .NET engine runs them in `Arc.Availability.Specs`, through `ManifestFileEvaluator`, which decides from a manifest file with the same engine that decides in a running application:

```csharp
var document = ManifestFileDocument.Parse(File.ReadAllText("cratis.availability.manifest.json"));
var evaluator = new ManifestFileEvaluator(document);

var decision = await evaluator.Decide(
    "Fleet.Vessels.Dock.DockVessel",
    ManifestContext.Empty with
    {
        Audiences = ["Plus"],
        Arguments = new Dictionary<string, object?> { ["vesselId"] = "v-17" }
    });
```

A condition that is `named` cannot be decided from the file and throws `InvalidDataException`.

## Writing the manifest

`ManifestFileBuilder.Build` describes a running engine, including gates registered in code, and `ToJson()` writes it:

```csharp
var document = ManifestFileBuilder.Build(gate, discoveredParts, catalog, manifest);
File.WriteAllText(ManifestFileDocument.FileName, document.ToJson());
```

The proxy generator also writes the part of the manifest the build can see without running the application, which is
every part with its location, the gates declared with attributes, and what an operator can suspend. See
[Writing the manifest at build](#writing-the-manifest-at-build).

## Writing the manifest at build

Set `CratisAvailabilityManifest` in the project to a file path and the proxy generator writes the manifest there each time it runs:

```xml
<PropertyGroup>
    <CratisAvailabilityManifest>$(MSBuildProjectDirectory)/obj/cratis.availability.manifest.json</CratisAvailabilityManifest>
</PropertyGroup>
```

Because the generator reads the assemblies without running them, the build file lists what attributes declare. A gate registered in
code at startup, or a typed condition that is code, appears only in the file `ManifestFileBuilder` writes from the running application.
