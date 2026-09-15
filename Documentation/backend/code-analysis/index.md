---
title: Code analysis rules
description: Core Arc diagnostic inventory, default severities, analyzer packaging, and available editor fixes.
---

Arc's Core Roslyn analyzers check command, query, validation, and concept declarations at compile time. They do not require Chronicle. The optional [Chronicle integration rules](../chronicle/code-analysis/index.md) have a separate `ARCCHR` prefix.

## Rules overview

These are the active `ARC` descriptors in the current source, all enabled by default in category `Arc`. Severity is the default before your project's analyzer configuration. The release-tracking file currently lists ARC0001–ARC0018 under **Unshipped**; its Shipped table has no rule entries. This inventory describes the current source, not a claim that every older NuGet version contains every rule.

| Rule ID | Descriptor title | Severity | Analyzer |
| --- | --- | --- | --- |
| [ARC0001](ARC0001.md) | Incorrect Query method signature on ReadModel | Error | `ReadModelAnalyzer` |
| [ARC0002](ARC0002.md) | Missing [Command] attribute on command-like type | Warning | `CommandAnalyzer` |
| [ARC0003](ARC0003.md) | Handle() must be on [Command] type | Error | `CommandAnalyzer` |
| [ARC0004](ARC0004.md) | [Command] type must have public Handle() method | Error | `CommandAnalyzer` |
| [ARC0005](ARC0005.md) | Value produced by Provide is not consumed by Handle | Warning | `CommandProvideAnalyzer` |
| [ARC0006](ARC0006.md) | Command-scoped read model can be missing | Warning | `InjectedReadModelAnalyzer` |
| [ARC0007](#arc0007-command-records) | Command should be declared as a record | Warning | `ModelBoundRecordAnalyzer` |
| [ARC0008](#arc0008-read-model-records) | ReadModel should be declared as a record | Warning | `ModelBoundRecordAnalyzer` |
| [ARC0009](#arc0009-concept-records) | Concept should be declared as a record | Warning | `ConceptRecordAnalyzer` |
| [ARC0010](#arc0010-synchronous-handlers) | Command Handle() wraps a synchronous result in a Task | Warning | `CommandHandleTaskWrappingAnalyzer` |
| [ARC0011](#arc0011-role-names) | [Roles] argument should use nameof instead of a string literal | Warning | `RolesLiteralAnalyzer` |
| [ARC0012](#arc0012-domain-named-exceptions) | Arc artifact throws a built-in exception type | Warning | `ArcArtifactBuiltInExceptionAnalyzer` |
| [ARC0013](#arc0013-concept-dereferences-in-validators) | Validator rule dereferences a possibly-null concept member | Warning | `ValidatorConceptDereferenceAnalyzer` |
| [ARC0014](#arc0014-generic-query-methods) | Generic query method on ReadModel | Error | `ReadModelAnalyzer` |
| [ARC0015](#arc0015-concept-query-parameters) | Query parameter converted to a concept in the method body | Warning | `QueryParameterConceptTypeAnalyzer` |
| [ARC0016](#arc0016-command-operation-methods) | Invalid command operation method | Error | `CommandOperationAnalyzer` |
| [ARC0017](#arc0017-command-operation-batches) | Use CommandOperations for operation batches | Error | `CommandOperationAnalyzer` |
| [ARC0018](#arc0018-command-operation-visibility) | Operation cannot have a generated invoker | Error | `CommandOperationAnalyzer` |

The individual rule pages contain deliberately invalid **diagnostic examples**, not runnable application checkpoints. Compile each alternative separately; duplicate domain type names are intentional.

## ARC0007: Command records

Reports a non-record type marked `[Command]`. Declare the command as a record and retain its public instance `Handle()` method. This is a modeling warning, not a claim that the runtime rejects every class-based command.

## ARC0008: Read model records

Reports a non-record type marked `[ReadModel]`. Prefer a record to represent returned state with value equality. A standalone Arc read model need not be an event-sourced projection.

## ARC0009: Concept records

Reports a non-record class deriving from `Cratis.Concepts.ConceptAs<T>`, including indirect inheritance. Use a record. Since `ConceptAs<T>` is itself a record, an ordinary derived class also violates the C# record inheritance rules; this diagnostic can accompany a compiler error.

## ARC0010: Synchronous handlers

Reports a public instance `Handle()` on a `[Command]` that returns `Task` or `Task<T>` and either:

- is `async` but has no own `await`, or
- returns only `Task.FromResult(...)` / `Task.CompletedTask` wrappers.

Return the synchronous value (or `void`) directly. Forwarding a genuine asynchronous operation is not the same as wrapping a synchronous value. Ordinary response DTOs do not need Chronicle. The editor offers **Unwrap to synchronous Handle()**; an `async` example without `await` may also produce compiler warning CS1998.

## ARC0011: Role names

Reports direct string-literal arguments to Arc's `[Roles]` attribute. Prefer `nameof(ApplicationRole.Administrator)` when that enum member defines the role's actual wire name. Do not change an externally assigned role string merely to silence the analyzer.

The editor offers **Use nameof for role** only when it finds a matching enum member. The current fix searches by member name and inserts an unqualified enum type name; review the selected enum and its namespace, especially when several enums share a member name. It does not prove that your identity provider issues that role.

## ARC0012: Domain-named exceptions

Reports explicit `throw new ...` of an exception in `System` or a `System.*` namespace from:

- a `[Command]` type's `Handle()` method,
- a `CommandValidator<T>` or `ConceptValidator<T>` type,
- a Chronicle `IReactor` implementation, if the optional integration is present.

Use validation rejection for expected invalid input. For genuinely exceptional failures, use a domain-named exception. Simply renaming an exception does **not** turn it into a validation result. For expected invalid input, return an explicit validation result rather than throwing an ordinary domain exception.

## ARC0013: Concept dereferences in validators

Reports a FluentValidation `RuleFor` selector that dereferences a member of a concept property, such as `RuleFor(command => command.Name.Value)`. Input can contain a null concept even when its declaration is non-nullable. Prefer validating the concept itself and placing its invariant in a `ConceptValidator<T>`; use a null guard when checking an optional concept's member.

**Current limitation:** detection is syntactic after identifying the concept type. It does not analyze a surrounding `.When(...)`, rule ordering, or cascade settings. A correctly guarded member selector can still be reported. Review the guard before applying a narrowly scoped suppression; do not remove required validation to obtain a clean build.

## ARC0014: Generic query methods

Reports public or internal static generic methods on a `[ReadModel]` whose return type has an accepted query shape. Query invocation cannot close the method's type parameters. Make the query non-generic, or move a generic composition helper off the read model. A valid return type alone does not make a generic method invocable.

## ARC0015: Concept query parameters

Reports `string`, `Guid`, or nullable `Guid` parameters converted to a `ConceptAs<T>`-derived type inside a public or internal static non-void method on a `[ReadModel]`. The analyzer recognizes conversion operations (including implicit conversions), not every possible manual construction or data-flow pattern.

Declare the parameter as the concept when you want Arc's query pipeline to validate that concept before invoking the method. Preserve null-aware handling for omitted input. For free-text search whose accepted values intentionally differ from an identifier concept's rules, review the model or suppress this warning locally with a reason. Direct static calls still bypass the query pipeline.

## ARC0016: Command operation methods

Reports an `ICommandOperation` implementation without exactly one valid public instance `Execute()` or with an invalid optional `Compensate()`. Both methods must be nongeneric and return `void`, `Task`, or `ValueTask`. Unsupported shapes include `async void`, value-returning tasks, static or overloaded methods, by-reference arguments, optional service parameters, and service locators. `CommandOperationFailure` can be requested only by `Compensate()`.

Use the [operation declaration contract](../commands/operations/reference.md#declaration-contract). Forward service calls directly; do not add a separate executor type or application `try/catch` merely to satisfy the convention.

## ARC0017: Command operation batches

Reports a command return shape containing a bare collection of operation values, including arrays and typed enumerables inside supported wrappers. Return `CommandOperations` instead. The explicit immutable batch distinguishes server execution from an ordinary collection response and supports collection expressions such as `[]`.

See [zero-to-many operations](../commands/operations/implementing.md#return-zero-or-many-operations). Do not work around the diagnostic by erasing operation types to `object`; use a meaningful declared return contract.

## ARC0018: Command operation visibility

Reports a concrete operation whose type cannot be referenced by its generated invoker. Use a public or internal nongeneric operation in accessible nongeneric containing types. File-local, private nested, or generic declarations do not provide the supported generated invocation shape. A file-local type cannot be referenced from the separate generated source file.

Runtime validation remains necessary when declarations are loaded without the source generator. Generated operation invokers do not establish NativeAOT support for every other Arc execution path.

## Quick fixes

Only these Core diagnostics currently have code-fix providers. The other rules require manual changes.

| Rule | Editor action | Provider |
| --- | --- | --- |
| ARC0010 | Unwrap to synchronous Handle() | `UnwrapCommandHandleTaskCodeFixProvider` |
| ARC0011 | Use nameof for role | `UseNameofForRolesCodeFixProvider` |

Both providers expose Roslyn's batch Fix All support. Review and compile the result, particularly the enum lookup for ARC0011.

## Installation

The `Cratis.Arc.Core` NuGet package depends on `Cratis.Arc.Core.CodeAnalysis`. Install Core normally; no Chronicle package is needed:

```bash
dotnet add package Cratis.Arc.Core
```

The analyzer package places `Cratis.Arc.Core.CodeAnalysis.dll` and `Cratis.Arc.Core.CodeAnalysis.CodeFixes.dll` under `analyzers/dotnet/cs`. Compiler analysis and workspace-based editor fixes live in separate assemblies. Arc Core's source generators are separate assets, not additional `ARC` rules. Editor support and the installed package version determine which fixes you see.

For source contributors, the inventory is defined by `Source/DotNET/Arc.Core.CodeAnalysis/DiagnosticDescriptors.cs`, each analyzer's `SupportedDiagnostics`, `AnalyzerReleases.*.md`, and `Arc.Core.CodeAnalysis.Package/Arc.Core.CodeAnalysis.Package.csproj`. Check all four when adding a rule; a descriptor alone does not demonstrate that a diagnostic runs or is packaged.
