---
title: Register Arc protected read-model key accessors
description: Register accessors for composite keys in Arc's Chronicle integration so protected reads can validate every key part without discovering properties.
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

Protected read-model keys that contain custom objects or records need accessors so Arc's Chronicle integration can validate their parts without discovering properties at runtime. Use generated accessors when available, or register typed getters for each composite type. Each registration covers the exact runtime type, not its subclasses.

Scalar keys, concept unwrapping, and tuple elements do not need member descriptors. A custom composite nested inside a concept or tuple still needs coverage. For the command identity these checks protect, see [Resolving EventSourceId](resolving-event-source-id.md).

## Register a composite type

The Arc model-graph generator emits a composite descriptor when your compilation references `Cratis.Arc.Chronicle` and it knows the complete property set. It follows types reachable from model-bound commands and query arguments. Manual registration is needed for partial types, external types or bases, types marked with `System.Diagnostics.CodeAnalysis.ExperimentalAttribute`, hidden or overridden members, and types generated code cannot safely name and read.

[ARC0030](../code-analysis/ARC0030.md) reports statically declared composite keys without coverage; it does not fail the build. Before the host starts or commands run, register every public readable non-indexed instance property, including inherited properties. This partial record needs manual coverage because another generator could add members:

```csharp
using Cratis.Arc.Chronicle.ReadModels;

// In startup configuration, before building the host:
ProtectedReadModelKeyAccessors.Register<WarehouseKey>(
    new(nameof(WarehouseKey.Warehouse), key => key.Warehouse),
    new(nameof(WarehouseKey.Section), key => key.Section));

public partial record WarehouseKey(string Warehouse, string Section);
```

Both getters above are typed delegates, so registration does not compile expression trees at runtime. Coverage depends on your registration being complete: omitting a property weakens validation. Leave out static properties, indexers, and properties with non-public getters.

Call `Register<EmptyKey>()` only for a type with no public readable non-indexed instance properties. An empty descriptor is distinct from an absent descriptor. Register every concrete derived runtime type separately, including inherited getters. A duplicate generated or manual registration throws `ProtectedReadModelKeyAccessorsAlreadyRegistered` instead of replacing coverage.

## Require coverage on JIT

Set `ProtectedReadModelKeyOptions.Strict` to `true` when missing composite coverage should fail instead of falling back to reflection on a just-in-time (JIT) compiled runtime. The default is `false`. Configure the option through the host's service collection:

```csharp
using Cratis.Arc.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;

builder.Services.Configure<ProtectedReadModelKeyOptions>(options => options.Strict = true);
```

When dynamic code is unsupported, strict coverage is mandatory regardless of this option. An uncovered custom composite throws `ProtectedReadModelKeyAccessorMissing`, naming the CLR type and registration action, never the key value. This is a configuration error, not a missing-model validation failure: command-result mapping reports an exception and an unsuccessful execution, corresponding to HTTP 500.

On a dynamic-code-capable JIT runtime with strict mode disabled, traversal prefers generated or manual descriptors, then uses the shared reflection compatibility helper. It reads public getters only. The fallback logs a warning once per exact type per process, naming the type and pointing to `ProtectedReadModelKeyAccessors.Register<T>()` and `ProtectedReadModelKeyOptions.Strict`. It never logs key values. Generated and manually registered paths do not produce this warning.

## Startup and runtime checks

Chronicle's command-aware read registration checks coverage at startup. The generator supplies declared key-type metadata for source keys and explicit lookups, plus conventional keys on statically recognized read models. This preflight checks only discovered commands whose effective read protection is `On`. It uses type metadata without rediscovering properties. In compatibility mode, it reports reflection fallback instead of rejecting startup.

`DeclareKeyTypes` is generator infrastructure, not a manual command-key registration requirement. Raw command keys still come from the existing event-source selection sites. Neither registration nor preflight converts a key, acquires state, or changes the resolved target.

Runtime traversal remains authoritative for nested composites, object/interface declarations, runtime-derived types, and commands without generated coverage metadata. It preserves reference-identity cycle detection, checks raw parts before conversion, and checks the already-resolved wire text even when a custom wire key has an empty descriptor. Missing-model rejection and read acquisition are separate from accessor coverage.

:::caution[This does not make protected reads wholly NativeAOT-safe]
Strict mode covers composite-part traversal only. Command-property discovery still uses existing reflection paths, which trimming can remove. Dynamic-code support does not detect trimming: a trimmed JIT application can still enter unsafe compatibility fallback unless you enable strict mode. Strict mode does not repair command-key discovery or establish whole-Arc NativeAOT support.
:::
