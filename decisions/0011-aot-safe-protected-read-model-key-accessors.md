---
id: 0011-aot-safe-protected-read-model-key-accessors
title: Validate protected read-model keys through generated or registered accessors
status: proposed
stage: none
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Chronicle/ReadModels/**
  - Source/DotNET/Chronicle/Commands/EventSourceExtensions.cs
  - Source/DotNET/Arc.Core.Generators/**
  - Source/DotNET/Arc.Core.Generators.Specs/**
  - Source/DotNET/Chronicle.Specs/**
  - Documentation/backend/csharp/chronicle/**
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

Proposed on 2026-10-18. Acceptance and a named human decider remain pending.

## Context

[Arc #3347](https://github.com/Cratis/Arc/issues/3347) requires protected command
reads with invalid keys to reject as missing models before acquiring state.
[PR #3387](https://github.com/Cratis/Arc/pull/3387), at `1f478be0915611cdc6ff96975cd53fdbc4cdb183`,
checks raw keys and recursive composite parts in
`Source/DotNET/Chronicle/ReadModels/ProtectedReadModelKeys.cs`. Its
`OriginalSourceKey` reflects over `context.Type.GetProperties()`, and `IsRefused`
reflects over `value.GetType().GetProperties(...)`. The issue reports five new
IL2075 diagnostics; `.github/workflows/aot-ratchet.yml` forbids diagnostic growth
and added trim/AOT suppressions. Leaving this unresolved blocks that delivery.

The following existing facilities were checked on `origin/main` at
`f4be8d9873040983b6051fd429ae23e08abc7214`:

- `Source/DotNET/Arc.Core.Generators/ModelGraphWalkerGenerator.cs` defines
  `Cratis.Arc.Generators.ModelGraphWalkerGenerator`, an incremental generator
  rooted in model-bound commands and query arguments. It emits typed member
  getters and module-initializer registrations into `ModelGraphWalkers.g.cs`.
- `Source/DotNET/Arc.Core.Generators/ModelGraphWalkerTypes.cs` deliberately
  excludes partial types, external types or base types, hidden/overridden
  properties, and other shapes whose complete reflected member set it cannot
  safely reproduce. Object/interface slots do not enumerate runtime subtypes.
- `Source/DotNET/Arc.Core/Validation/ModelGraphWalkers.cs` registers exact runtime
  types; `ModelGraphValidator.cs` falls back to reflection when none is registered.
  Neither facility describes Chronicle command-key selection. The exclusions are
  exercised in `Source/DotNET/Arc.Core.Generators.Specs/for_ModelGraphWalkerGenerator/`.
- `Source/DotNET/Chronicle/Commands/EventSourceExtensions.cs` selects source keys
  by event-source-id type or `[Key]`, including matching constructor parameters.
  `EventSourceValuesProvider.cs` supports a provider-supplied resolved id.
  `Source/DotNET/Chronicle/ReadModels/CommandReadModelLookup.cs` also supports
  `[LookUpBy]` and unambiguous property-type selection.

## Decision

Use generated command-key and composite-member accessors, with public typed
registrations for shapes generation cannot cover, and fail closed when required
accessors are absent. Extend `ModelGraphWalkerGenerator` in
`Source/DotNET/Arc.Core.Generators` with a distinct protected-key output rather
than treating its existing validation walkers as complete key metadata. Emit
Chronicle registrations only when the consuming compilation references the new
Chronicle registry; Arc.Core gains no Chronicle dependency. Preserve strict key
validation on both JIT and AOT: no `UnconditionalSuppressMessage`, baseline growth,
reflection fallback for this traversal, or skipped validation.

### Generated and registered coverage

The following are proposed API responsibilities, not shipped APIs or finalized
signatures:

- A public typed command registration supplies raw-key getters and selection
  metadata for an exact command type. Generated descriptors preserve `[LookUpBy]`,
  conventional selection, source-key selection and ambiguity behavior, including
  constructor-parameter `[Key]`. A complete descriptor can explicitly report no
  source-key property; a missing descriptor cannot mean the same thing.
- A public typed composite registration supplies the complete set of public,
  readable, non-indexed instance-property getters for an exact key type, including
  applicable inherited members. Delegates read members directly through their
  declared types, not reflection, expression compilation or dynamic code.
  An explicitly empty member set differs from an absent registration.
- Generation uses the existing conservative completeness checks where applicable.
  It must not emit a partial descriptor for a partial declaration or assume another
  generator's output is visible. Applications register runtime-only, external,
  partial, otherwise inaccessible-to-generation, and ungenerated derived shapes
  through typed code that can access their complete final member set.
- Registrations are installed before protected commands execute. Base-type
  registrations do not cover unknown derived types; each non-built-in runtime
  type encountered recursively needs exact coverage. Conflicting descriptors
  fail as configuration errors, rather than depending on registration order.

Keep built-in scalar canonicalization, concept unwrapping, tuple element traversal
and reference-identity cycle detection. The scalar set follows
`Source/DotNET/Chronicle/Commands/GeneratedIdentifierCanonicalizer.cs`, not an
assumption that every value type is scalar. Validate every raw composite part
before conversion, then validate the resolved wire key. A custom wire-key object
with no public readable properties still needs an explicit complete descriptor;
its converted text is checked, not silently accepted as an opaque object.
Reject null, empty or whitespace-only parts, exactly `*`, any `#`, and leading or
trailing whitespace; do not trim input. A stored instance at such a key does not
make the key valid.

Preserve PR #3387's provider/response overrides: inspect a raw source only when it
still denotes the resolved target, and do not replace the resolved id with a new
canonicalization. Invalid input remains a `ProtectedReadModelDoesNotExist`
validation rejection with reason `Rule` (wire `rule`) and message
`<Model> '<key>' does not exist.`; composite diagnostic text is empty. Valid
missing nullable dependencies remain null; required ones reject. These types and
behaviors are proposed in PR #3387, not present on the inspected main commit.

### Missing-accessor outcome

The authoritative check is at command time, before protected read acquisition or
state enrollment. A missing required command/composite descriptor throws a new
public configuration exception (proposed name `ProtectedReadModelKeyAccessorMissing`)
identifying the uncovered CLR type and registration action, without key values.
It does not implement `IValidationFailure`: the pipeline returns an unsuccessful
`CommandResult` with `HasExceptions` true and HTTP 500, not a missing-model/rule
rejection or successful read. This follows the existing mapping in
`Source/DotNET/Arc.Core/Commands/CommandResult.cs` and distinguishes application
misconfiguration from invalid client input.

For statically identified uncovered shapes, generation emits an informational
build diagnostic explaining the typed registration requirement; it does not
reject a valid separately registered application at compile time. No startup
reflection scan or claim of complete startup coverage is required. Runtime-only
subtypes cannot be proven covered at build time, so command-time checking is
mandatory even after a clean build. Failure has the same meaning on JIT and AOT.

## Options considered

- **Generated accessors plus typed registrations (chosen).** Statically references
  members where possible while retaining strict checking for open runtime graphs.
- **Suppress IL2075 or increase the baseline (rejected).** Hides unproven member
  preservation and violates the ratchet instead of making the access safe.
- **ModelGraphWalkers alone (rejected).** Its exact-type coverage is deliberately
  incomplete and its normal fallback reflects. Treating an absent walker as a
  leaf silently omits composite parts; sharing getters is safe only after proving
  the protected-key member contract is complete.
- **Reflection with DynamicallyAccessedMembers annotations (rejected).** Annotating
  a command `Type` can preserve that type's properties, but does not recursively
  preserve the runtime types returned by those getters. A property declared as
  object, an interface or a base type may return an arbitrary subtype whose own
  recursively returned members are unknown. Annotating the outer Type or generic
  parameter does not propagate that requirement through boxed property values and
  their `GetType()` calls. It cannot express this open recursive preservation set.
- **Skip composite validation under AOT (rejected).** Makes invalid-key behavior
  depend on deployment mode and can admit keys the protected read must refuse.

## Default if unanswered

Keep #3347/PR #3387 blocked and retain main's existing behavior. Do not merge the
new reflective traversal, suppress its diagnostics, or weaken validation to make
CI pass. The cost is delayed Screenplay invalid-key conformance, not an implicit
approval of a different key contract.

## Timeline and scope

Acceptance is required before the separate accessor implementation is treated as
the settled contract. Implement and verify the accessors before shipping #3347;
the contract then holds for future protected-key shapes until superseded.

The follow-up implementation issue must cover generator output and diagnostics,
public typed registration and exception APIs, integration with raw-key selection
and recursive checking, specs, AOT analysis, and migration documentation. It must
carry the verification criteria below unchanged. This record creates no issue.
Out of scope: making all Arc NativeAOT-compatible, changing ordinary model graph
validation, Off/Measure or advisory-read behavior, stream routing, other Chronicle
refusal reasons, and query selection policy (decision 0009 remains unchanged).

## Verification

- **Done when:** generated and manually registered paths reject every invalid raw
  scalar/composite part before reader acquisition, preserve valid/nullable and
  override behavior, and uncovered exact runtime types fail as configuration
  errors without acquiring or enrolling a read; public migration guidance exists.
- **Verify by:** run generator and Chronicle specs for generated source keys,
  constructor `[Key]`, explicit/conventional lookups, nested concepts/tuples,
  cycles, custom wire keys, provider/response overrides and DateOnly resolved ids;
  exercise runtime-only, external, partial and derived types with and without
  typed registration, plus informational diagnostic and HTTP failure mapping;
  run Debug/Release and repository release gates, and `python3 scripts/aot-ratchet.py`
  across its configured target frameworks with no diagnostic growth or new
  suppressions, followed by independent review. Documentation alone proves none
  of these implementation criteria.

## Consequences

Generated applications with fully covered key graphs require no hand-written
registration. Applications using shapes the generator cannot cover must register
complete typed descriptors before executing protected commands, even on JIT.
This adds public registration and configuration-exception APIs and replaces
previously available reflection-backed execution for uncovered shapes with a
failure. The APIs alone are additive, but the combined mandatory-registration
contract is a **major** semver change and needs an explicit migration note; it is
not a patch-only fix to #3347. This documentation-only record ships no API.
Applications own the completeness of their manual descriptors as trusted code;
tests must cover members added by other generators. Exact coverage costs setup
and maintenance, but prevents trimming from silently weakening protected reads.
