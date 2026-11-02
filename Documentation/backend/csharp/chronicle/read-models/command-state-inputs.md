---
title: Read models as command state inputs
description: Declare command state as read-model parameters, preserve read protection, and recognize acquisition gaps that need explicit readers.
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

A command's `Handle()` may take any service dependencies. It reads state by declaring the read models it needs as parameters. Direct `IDecisionReads` or `DecisionRead<T>` parameters are almost never needed for the supported shapes below. `IDecisionReads` acquires state; `DecisionRead<T>` wraps an issued version token and its model instance. Neither is forbidden.

## Existing acquisition shapes

| Shape | Existing declaration | Behavior |
| --- | --- | --- |
| Default key | `Handle(AccountState account)` | Uses the command's event-source key. |
| Different model types | `Handle(AccountState account, CustomerState customer)` | Acquires each recognized model independently. |
| Same type, different command keys | `Handle([LookUpBy(nameof(From))] AccountState from, [LookUpBy(nameof(To))] AccountState to)` | Uses each named public command property. |
| Nullable state | `Handle(AccountState? account)` | A valid key with no model produces null; it does not excuse an invalid key. |
| Required protected state | `Handle(AccountState account)` with protection On | Missing state is a validation failure, not success with default state. |
| Provisioned decision | `Provide(AccountState account)` then `Handle(Prepared prepared, IService service)` | Derived data does not itself issue a token. Protection comes from the acquired input. |
| Declared same-stream state | `Handle(MonthlyTotals? month)` | Uses the model's declared stream and the command route, including its stream-id template. Cross-route reads are not supported by this binding. |

These signatures describe existing declarations, not new overloads or a collection-binding API. [Injecting read models](injecting-into-commands.md) explains the lookup attributes and optional state; [stream decisions](../commands/stream-decisions.md) explains stream routing.

## Compiled example: two keys of the same model

This command excerpt assumes an `AccountId : ConceptAs<string>` concept and a `Balance(AccountId Id)` read model registered with Chronicle. The model's projection is omitted here; see [What makes a read model injectable](index.md#what-makes-a-read-model-injectable). It reads each account through an explicit public command property and records the identities returned by acquisition, rather than calling a reader inside `Handle()`.

```csharp
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;

[Command]
[ReadProtection(ReadProtectionMode.On)]
public record RecordTransfer(AccountId From, AccountId To)
{
    public TransferRecorded Handle(
        [LookUpBy(nameof(From))] Balance from,
        [LookUpBy(nameof(To))] Balance to) => new(from.Id, to.Id);
}

[EventType("1e1ae8e5-2e30-4baa-a55f-8ecc54189fd9")]
public record TransferRecorded(AccountId From, AccountId To);
```

The command is compiled in `Source/DotNET/Chronicle.Specs/ReadModels/for_ChronicleParameterDependencyResolver/given/RecordTransfer.cs`, and its event in `given/TransferRecorded.cs` next to it. The specification `when_a_handler_records_keyed_state.cs` in the same folder calls `Handle()` with resolved state and checks its event values, two protected reads for different keys, and one cached read for equal keys. Run it from the repository root:

```shell
dotnet test Source/DotNET/Chronicle.Specs -c Release --filter FullyQualifiedName~when_a_handler_records_keyed_state
```

Both cases pass: different keys issue two tokens, while equal keys reuse the same model instance and issue one token. The default-key specification (`when_handlers_use_plain_state.cs` in the same folder) also checks the issued version boundary and the Off/Measure/On differences; the same-stream specification (`Source/DotNET/Chronicle.Specs/ReadModels/for_per_stream_models/when_plain_stream_state_reaches_a_handler.cs`) checks the routed month and handler output.

## Protection is explicit

`ReadProtectionMode.Off` (the default) and `Measure` do not enroll a plain model as a guarded read. Measure observes behavior without turning it into concurrency protection. With `On`, admitted model reads are enrolled at the boundary that produced their folded state. A conflicting append rejects the command instead of appending its events.

Do not replace a protected `DecisionRead<T>` with a plain model and assume identical protection: enable the appropriate read-protection policy and preserve its scope. ARCCHR0011 and ARCCHR0012 still apply. Combining whole-source and per-stream reads can widen the guard; see ARCCHR0038.

## Unresolved acquisition shapes

Plain model parameters do not currently express every decision read. Caller-derived keys, optional or conditional keys, dynamic keyed collections, keys derived from other state, and cross-route reads still need separate design. Do not treat proposed binding APIs for these shapes as shipped capabilities, and do not move a reader into `Provide()` merely to hide a limitation.

## Informational guidance and suppression

[ARCCHR0040](../code-analysis/ARCCHR0040.md) suggests the model-parameter alternative for direct `IDecisionReads` and `DecisionRead<T>` parameters in recognized command `Handle()` methods. It is Info only, has no code fix, and changes no runtime behavior. Arbitrary services are not restricted; `Provide()` is not policed by this diagnostic.

When an actual acquisition gap requires the dependency, suppress it locally with a concrete reason. This state-returning command excerpt assumes a registered `State` model, the `Cratis.Arc.Commands.ModelBound` and `Cratis.Chronicle.ReadModels` namespaces, and an application-provided `ICallerContext` service. Its account key comes from trusted caller context, not a bindable command property:

```csharp
public interface ICallerContext { ReadModelKey AccountKey { get; } }
[Command] public record InspectCaller
{
#pragma warning disable ARCCHR0040 // Key comes from trusted caller identity, not a command property.
    public async System.Threading.Tasks.Task<State> Handle(IDecisionReads reads, ICallerContext caller)
    {
        var state = await reads.Get<State>(caller.AccountKey);
        return state.Instance;
    }
#pragma warning restore ARCCHR0040
}
```

The ARCCHR0040 specification (`Source/DotNET/Chronicle.CodeAnalysis.Specs/for_CommandReadModelParameterAnalyzer/when_a_handler_declares_dependencies.cs`) compiles this declaration and verifies the reasoned suppression. `ICallerContext` is an illustrative application interface, not a new Arc API.

Keep the reason beside the declaration. Suppression does not waive concurrency, authorization, or acquisition-failure handling.
