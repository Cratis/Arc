---
id: 0011-aot-safe-protected-read-model-key-accessors
title: Use strict composite-key accessors with a JIT compatibility fallback
status: proposed
stage: none
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Chronicle/ReadModels/**
  - Source/DotNET/Chronicle/Commands/EventSourceExtensions.cs
  - Source/DotNET/Chronicle/Commands/EventSourceValuesProvider.cs
  - Source/DotNET/Chronicle/Commands/CommandContextExtensions.cs
  - Source/DotNET/Arc.Core/Validation/**
  - Source/DotNET/Arc.Core/Arc.Core.csproj
  - Source/DotNET/Arc.Core.Generators/**
  - Source/DotNET/Arc.Core.Generators.Specs/**
  - Source/DotNET/Chronicle.Specs/**
  - Documentation/backend/csharp/chronicle/**
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

Proposed on 2026-10-18; revised after independent review. No acceptance or decider yet.

## Context

[Arc #3347](https://github.com/Cratis/Arc/issues/3347) requires invalid protected-read keys to reject before acquiring state.
[PR #3387](https://github.com/Cratis/Arc/pull/3387), at `1f478be0915611cdc6ff96975cd53fdbc4cdb183`, adds raw-key recovery
and recursive composite checking in `Source/DotNET/Chronicle/ReadModels/ProtectedReadModelKeys.cs`.
The issue reports five new IL2075 diagnostics. `.github/workflows/aot-ratchet.yml` forbids diagnostic
growth and added trim/AOT suppressions; shipping remains blocked.

Code checked on main at `f4be8d9873040983b6051fd429ae23e08abc7214`:

- `Source/DotNET/Chronicle/Commands/EventSourceExtensions.cs` already reflects in
  `GetEventSourceIdWithDeclaredType`, reads the selected raw property value, and
  exposes declared type through an out parameter. `EventSourceValuesProvider.cs`
  uses that path unless the command provides its own id. `CommandContextExtensions.cs`
  reads the resolved context/response id; `GetEventSourceIdOrDefault` does not
  itself reflect. PR #3387's `OriginalSourceKey` repeats property discovery only
  after finding a resolved id. Returning and retaining the already-read raw value
  avoids that new reflection; supplied ids need not have come from reflection.
- `Source/DotNET/Chronicle/ReadModels/CommandReadModelLookup.cs` already reads raw
  values for `[LookUpBy]` and conventional selection. These command-property
  discovery sites and `EventSourceExtensions.cs` have IL2075 entries in
  `Source/DotNET/aot-baseline.json`; no command-key generator is necessary here.
- `Source/DotNET/Arc.Core.Generators/ModelGraphWalkerGenerator.cs` defines
  `Cratis.Arc.Generators.ModelGraphWalkerGenerator`, emitting typed getters and
  module-initializer registrations. `ModelGraphWalkerTypes.cs` excludes partial
  types, external types/bases, unsafe or hidden/overridden members and unobserved
  runtime subtypes. `ReachableTypes` also skips experimental assemblies/modules.
- `Source/DotNET/Arc.Core/Validation/ModelGraphValidator.cs` has an already-baselined
  IL2070 in `GetWalkablePropertiesThroughReflection`. This internal helper is not
  callable from Chronicle today: `Source/DotNET/Arc.Core/Arc.Core.csproj` grants no
  `InternalsVisibleTo` to `Cratis.Arc.Chronicle`. Its `property.CanRead` filter is
  broader than PR #3387's public-getter filter; it is not an identical contract.

## Decision

Recommend a **minor** release combining generated/typed custom-composite accessors
with a shared, already-baselined reflection fallback for compatible JIT execution.
Require strict accessor coverage when `RuntimeFeature.IsDynamicCodeSupported` is
false, and offer strict mode as an opt-in elsewhere. Never use the fallback in
strict mode. Recover raw command keys at existing selection sites, not through a
new command-key generator or registration requirement. No suppression or baseline
growth is allowed. **The lead must confirm this minor compatibility policy versus
the major alternative of strict-by-default on every runtime.** This proposed
record does not grant that choice; universal strict-by-default is deferred to the
next major under the recommendation.

### Raw keys and composite coverage

Return the already-read raw key alongside the source id/declared type and retain it in command context.
Explicit/conventional lookups pass their raw values directly. Provider paths lacking a retained raw value
can reuse that same baselined helper for target comparison, never add another property-discovery site.
Preserve constructor-parameter `[Key]`, ambiguity behavior and provider/response overrides: inspect a raw
source only when it still denotes the resolved target; never recanonicalize that target. The ratchet
must verify that this propagation/reuse leaves diagnostic counts unchanged.

Extend `ModelGraphWalkerGenerator` with distinct custom-composite output, emitted
only when the consuming compilation references the proposed Chronicle registry.
Arc.Core gains no Chronicle dependency. Reuse conservative completeness checks,
not the assumption that every `ModelGraphWalkers` entry satisfies the key contract.
Only custom composite objects/records need member accessors: scalars, concept unwrapping and `ITuple`
element traversal need no member enumeration, except for custom composites nested inside them.

The proposed public API adds typed exact-type composite registration, a strict-mode option and a
configuration exception; names/signatures are not finalized. Descriptors contain every public readable
non-indexed instance-property getter, including applicable inherited members, read through typed delegates.
Absent and explicitly empty descriptors differ. Runtime-only, external, partial,
experimental and otherwise ungenerated custom types require manual coverage in
strict mode; base registrations never cover unknown derived runtime types.
Registrations precede commands. Conflicts fail deliberately, unlike the last-wins
assignment in `Source/DotNET/Arc.Core/Validation/ModelGraphWalkers.cs`'s `Register`.

In compatibility mode, try generated/registered descriptors first, then share
`ModelGraphValidator.GetWalkablePropertiesThroughReflection` through a new internal
friend grant to Chronicle. Add a public-getter-only selection option inside that
existing helper, preserving its default behavior for ordinary validation. Do not
add another reflective enumeration or invoke the generic model-validation walk:
key traversal retains its own scalar, concept, tuple and cycle semantics.
`Source/DotNET/Arc.Core/Validation/WalkableMember.cs` may compile expression getters;
this fallback is restricted to dynamic-code-supported compatibility execution.

Keep reference-identity cycle detection. Validate raw parts before conversion and
then the resolved wire text, including custom wire-key objects with empty member
sets. Reject null, empty/whitespace-only parts, exactly `*`, any `#`, and leading
or trailing whitespace without trimming, regardless of a stored instance.
PR #3387's proposed `ProtectedReadModelDoesNotExist` remains a `Rule` (wire `rule`)
validation rejection: `<Model> '<key>' does not exist.`, with empty composite
text. Valid missing nullable dependencies remain null; required ones reject.

### Coverage failure and safety boundary

In strict mode, an uncovered custom composite throws a proposed public `ProtectedReadModelKeyAccessorMissing`
configuration exception before read acquisition/enrollment. Identify the type and registration action, not key values.
It is not `IValidationFailure`: mapping in `Source/DotNET/Arc.Core/Commands/CommandResult.cs` yields
`HasExceptions` true, unsuccessful execution and HTTP 500, not a missing-model validation rejection.
Compatibility mode instead validates through the shared reflective fallback.

Emit informational diagnostics for statically known uncovered composites; do not
reject separately registered applications at build time. Where discovered protected
commands already supply statically declared key types, add a startup registry
preflight using type lookups only: fail uncovered custom types in strict mode,
report the fallback in compatibility mode. Do not add reflective property discovery
for this preflight. Command-time exact-type checking remains authoritative for
nested, object/interface and runtime-derived shapes that startup cannot enumerate.

This makes **strict composite-part traversal** trim-safe or explicitly refused,
not protected reads as a whole. Command-key selection in `EventSourceExtensions`
and `CommandReadModelLookup` still reflects and can lose properties under trimming.
Dynamic-code support is not a trimming detector: trimmed JIT can return true and
use an unsafe compatibility fallback unless strict mode is enabled. Even strict
mode does not repair command-key discovery; whole-Arc NativeAOT safety is unclaimed.

## Options considered

- **Universal strict generated/registered composites — major.** Cleanest long-term
  contract, with no new use of baselined reflection, but requires registrations
  for previously working JIT custom types. Prefer at the next major, not as an
  unavoidable consequence of fixing the duplicated command-key reflection.
- **Generated first, shared reflective fallback on every runtime — minor.** Keeps
  untrimmed JIT behavior and can avoid baseline growth/suppressions with the friend
  grant and filter adaptation above. It still risks losing composite parts under
  AOT/trimming. Reusing a baselined warning expands reflection's callers without
  increasing its count: this satisfies the numeric ratchet, but conflicts with its
  intent to reduce unproven reflection. It is not evidence of AOT correctness.
- **Strict opt-in or strict only when dynamic code is unsupported — minor, with
  a JIT fallback.** Opt-in alone leaves AOT callers unsafe by default; the runtime
  gate alone cannot protect trimmed JIT. **Recommend combining both with the shared
  compatibility fallback:** strict composite coverage is mandatory under AOT-like
  restricted runtimes and selectable on JIT, while ordinary JIT keeps its member
  coverage. Without the preceding fallback alternative, this still needs JIT
  reflection and merely leaves PR #3387's new diagnostics unresolved. The numeric
  ratchet tension remains and is part of the lead's minor-versus-major choice.
- **Closed set of tuples, concepts and scalars only — major.** Simpler coverage, but removes custom-record
  keys and their nested equivalents rather than offering registration. Reject this unnecessary migration.
- **ModelGraphWalkers alone, suppressions, baseline growth or skipped AOT checks —
  rejected.** An absent walker cannot mean a valid leaf. None proves full checking.
- **DynamicallyAccessedMembers alone — insufficient.** Preserving an outer command
  Type's public properties does not preserve arbitrary runtime types returned by
  object/interface/base-typed getters, recursively. An annotation cannot express
  this open recursive preservation set. It does not solve custom-composite access.

## Default if unanswered

Keep #3347/PR #3387 blocked with main unchanged. No release-intent choice, new reflection caller,
suppression or weakened AOT composite checking is implicitly approved. The cost is delayed conformance.

## Timeline and scope

Confirm the minor-versus-major policy before implementation ships #3347. The follow-up issue covers raw-value
propagation, composite generation, public registration/strict-mode/exception APIs, internal JIT helper sharing,
coverage diagnostics/preflight, specs, AOT analysis and migration documentation; it must carry the criteria below
unchanged. This record creates no issue. Out of scope: whole-Arc AOT support, changing ordinary model validation,
Off/Measure or advisory behavior, stream routing, other Chronicle refusals and query selection (decision 0009).

## Verification

- **Done when:** generated/registered and compatible JIT paths reject every invalid
  raw part before read acquisition; strict uncovered composites fail as configuration
  errors; valid/nullable/override behavior holds; migration and safety limits are documented.
- **Verify by:** run generator and Chronicle specs for raw source retention, constructor
  `[Key]`, explicit/conventional lookups, provider/response overrides, DateOnly ids,
  concepts/tuples, cycles, custom wire keys and public/private getter distinctions;
  cover external/partial/experimental/runtime-derived composites with and without
  registration in compatibility, opt-in strict and dynamic-code-unsupported paths,
  startup/command failure mapping and conflicts; run Debug/Release and repository
  release gates plus `python3 scripts/aot-ratchet.py` across configured frameworks
  with no diagnostic growth or suppressions, followed by independent review.

## Consequences

Under the recommended minor policy, untrimmed JIT applications need no accessor migration; strict-mode/restricted-runtime
applications register uncovered custom composites or accept configuration failure. No command-key registration is required.
New public APIs are additive; strict-by-default on all runtimes would instead be major and require migration.
Sharing the baselined JIT helper is a compatibility compromise, not reduced trimming risk. Manual descriptors remain
trusted application code. This documentation ships no API and proves neither implementation nor end-to-end AOT safety.
