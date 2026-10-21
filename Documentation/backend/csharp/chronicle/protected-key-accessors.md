---
title: Register composite protected-key accessors
description: Configure complete composite-key member coverage for strict protected-read validation without runtime property discovery.
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

Use generated or typed accessors when protected read-model keys contain custom objects or records. Accessors describe the **exact runtime type**, not its subclasses. Scalar keys, concept unwrapping, and tuple elements do not need member descriptors; custom composites nested inside them do.

## Register a composite type

The Arc model-graph generator emits a separate composite descriptor when your compilation references `Cratis.Arc.Chronicle` and the complete property set is known. It follows types reachable from model-bound commands and query arguments. It does not generate descriptors for partial types, external types or bases, experimental types, hidden or overridden members, or types generated code cannot safely name and read.

For a statically declared uncovered composite key, [ARC0030](../code-analysis/ARC0030.md) explains the registration requirement without failing the build. Register every public readable non-indexed instance property before the host starts or commands run, including inherited properties. For example, this partial type requires manual coverage:

```csharp
using Cratis.Arc.Chronicle.ReadModels;

// In startup configuration, before building the host:
ProtectedReadModelKeyAccessors.Register<WarehouseKey>(
    new(nameof(WarehouseKey.Warehouse), key => key.Warehouse),
    new(nameof(WarehouseKey.Section), key => key.Section));

public partial record WarehouseKey(string Warehouse, string Section);
```

The getters are typed delegates, not expression trees compiled at runtime. Registration is trusted application code: omitting a property weakens your validation. Do not include static properties, indexers, or properties with non-public getters.

Call `Register<EmptyKey>()` only for a type with no public readable non-indexed instance properties. An empty descriptor is distinct from an absent descriptor. Register every concrete derived runtime type separately, including inherited getters. A duplicate generated or manual registration throws `ProtectedReadModelKeyAccessorsAlreadyRegistered` instead of replacing coverage.

## Require coverage on JIT

`ProtectedReadModelKeyOptions.Strict` defaults to `false`. Configure it through the host's service collection:

```csharp
using Cratis.Arc.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;

builder.Services.Configure<ProtectedReadModelKeyOptions>(options => options.Strict = true);
```

When dynamic code is unsupported, strict coverage is mandatory regardless of this option. An uncovered custom composite throws `ProtectedReadModelKeyAccessorMissing`, naming the CLR type and registration action, never the key value. This is a configuration error, not a missing-model validation failure: command-result mapping reports an exception and an unsuccessful execution, corresponding to HTTP 500.

On a dynamic-code-capable JIT runtime with strict mode disabled, traversal prefers generated or manual descriptors, then uses the shared reflection compatibility helper. It reads public getters only. The fallback logs a warning once per exact type per process, naming the type and pointing to `ProtectedReadModelKeyAccessors.Register<T>()` and `ProtectedReadModelKeyOptions.Strict`. It never logs key values. Generated and manually registered paths do not produce this warning.

## Startup and runtime checks

Chronicle's command-aware read registration installs a startup preflight. The generator supplies declared key-type metadata for source keys and explicit lookups, plus conventional keys on statically recognized read models. Preflight checks only discovered commands whose effective read protection is `On`. It checks registry coverage using types, without rediscovering properties; in compatibility mode it reports the fallback instead of rejecting startup.

`DeclareKeyTypes` is generator infrastructure, not a manual command-key registration requirement. Raw command keys still come from the existing event-source selection sites. Neither registration nor preflight converts a key, acquires state, or changes the resolved target.

Runtime traversal remains authoritative for nested composites, object/interface declarations, runtime-derived types, and commands without generated coverage metadata. It preserves reference-identity cycle detection, checks raw parts before conversion, and checks the already-resolved wire text even when a custom wire key has an empty descriptor. Missing-model rejection and read acquisition are separate from accessor coverage.

:::caution[This does not make protected reads wholly NativeAOT-safe]
Strict mode covers composite-part traversal only. Command-property discovery still uses existing reflection paths, which trimming can remove. Dynamic-code support does not detect trimming: a trimmed JIT application can still enter unsafe compatibility fallback unless you enable strict mode. Strict mode does not repair command-key discovery or establish whole-Arc NativeAOT support.
:::
