---
title: Cratis Arc command state inputs
description: Declare read-model parameters in Cratis Arc commands, choose lookup keys or stream scope, and identify when state needs an explicit acquisition step.
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

Declare a read-model parameter when a Cratis Arc command needs current state to decide which event to produce. Arc acquires the model before calling `Handle()`, so you can specify the decision by calling `Handle(state)` directly.

Choose which instance the command needs before choosing a reader API. The bindings below cover command keys, caller claims, and the command's own stream. Use `IDecisionReads`, which acquires state, or `DecisionRead<T>`, which carries an issued version token and its model instance, directly only when a parameter cannot express the acquisition.

## Existing acquisition shapes

| Shape | Existing declaration | Behavior |
| --- | --- | --- |
| Default key | `Handle(AccountState account)` | Uses the command's event-source key. |
| Different model types | `Handle(AccountState account, CustomerState customer)` | Acquires each recognized model independently. |
| Same type, different command keys | `Handle([LookUpBy(nameof(From))] AccountState from, [LookUpBy(nameof(To))] AccountState to)` | Uses each named public command property. |
| Caller-derived key | `Handle([LookUpBy<CallerSubject>] AccountState account)` | Uses the selected caller's exact `sub` claim. An application `CallerClaim` provider can select another claim. |
| Nullable state | `Handle(AccountState? account)` | A valid key with no model produces null; it does not excuse an invalid key. |
| Required protected state | `Handle(AccountState account)` with protection On | Missing state is a validation failure, not success with default state. |
| Provisioned decision | `Provide(AccountState account)` then `Handle(Prepared prepared, IService service)` | Derived data does not itself issue a token. Protection comes from the acquired input. |
| Declared same-stream state | `Handle(MonthlyTotals? month)` | Uses the model's declared stream and the command route, including its stream-id template. Cross-route reads are not supported by this binding. |

These are individual model dependencies, not a collection-binding API. First check [what makes a model injectable](index.md#what-makes-a-read-model-injectable): `[ReadModel]` alone does not give Chronicle a way to load it. Then use [Injecting read models](injecting-into-commands.md) to choose lookup attributes and define what missing state means. For a stream-scoped decision, continue to [stream decisions](../commands/stream-decisions.md).

## Compiled example: two keys of the same model

A transfer needs both accounts, so a single command key cannot identify all its inputs. This excerpt assumes an `AccountId : ConceptAs<string>` concept and a `Balance(AccountId Id)` read model registered with Chronicle. Its projection is omitted; see [What makes a read model injectable](index.md#what-makes-a-read-model-injectable). Each lookup names a public command property, leaving `Handle()` to record the identities from the acquired state.

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

`From` selects the first `Balance`; `To` selects the second. With protection `On`, different keys issue two protected reads, while equal keys reuse one model instance and one token. The handler receives those models without acquiring either itself.

The compiled example lives in `Source/DotNET/Chronicle.Specs/ReadModels/for_ChronicleParameterDependencyResolver/given/RecordTransfer.cs`, with its event in `given/TransferRecorded.cs`. To check the binding and handler output, run the specification from the repository root:

```shell
dotnet test Source/DotNET/Chronicle.Specs -c Release --filter FullyQualifiedName~when_a_handler_records_keyed_state
```

The specification checks the event values as well as the read count. Related specifications cover the default key (`when_handlers_use_plain_state.cs` in the same folder) and a routed month (`Source/DotNET/Chronicle.Specs/ReadModels/for_per_stream_models/when_plain_stream_state_reaches_a_handler.cs`).

## Protection is explicit

Choosing a key does not enable concurrency protection. `ReadProtectionMode.Off` returns a snapshot and guards no later append. `Measure` observes that path without enforcing protection. With `On`, the default from Arc 23, Arc enrolls admitted model reads at the boundary that produced their folded state. A conflicting append rejects the command instead of appending its events.

Do not replace a protected `DecisionRead<T>` with a plain model and assume identical protection: enable the appropriate read-protection policy and preserve its scope. ARCCHR0011 and ARCCHR0012 still apply. Combining whole-source and per-stream reads can widen the guard; see ARCCHR0038.

## Unresolved acquisition shapes

Use [caller-key lookup](injecting-into-commands.md#key-a-read-by-the-caller) when a selected caller claim supplies the key; that binding is supported. Optional or conditional keys, dynamic keyed collections, keys derived from other state, and cross-route reads still need a separate acquisition design. A nullable model parameter handles absence after a valid lookup, not an optional key.

When no binding fits, identify what must be acquired before the decision can run. Keep that limitation visible rather than moving a reader into `Provide()` merely to hide it.

## Informational guidance and suppression

[ARCCHR0040](../code-analysis/ARCCHR0040.md) suggests the model-parameter alternative for direct `IDecisionReads` and `DecisionRead<T>` parameters in recognized command `Handle()` methods. It is Info only, has no code fix, and changes no runtime behavior. Arbitrary services are not restricted; `Provide()` is not policed by this diagnostic.

When a binding cannot express the acquisition, suppress the diagnostic locally with a concrete reason and a tracking-feature link. Keep `IDecisionReads` or `DecisionRead<T>` directly in `Handle()` rather than moving it to `Provide()`.

The following state-returning excerpt illustrates an application-owned key service, not the usual caller-claim lookup. It assumes a registered `State` model, the `Cratis.Arc.Commands.ModelBound` and `Cratis.Chronicle.ReadModels` namespaces, and an application-provided `ICallerContext` service. If that service only exposes a selected caller claim, use `CallerSubject` or an application `CallerClaim` provider instead. The advanced reader is warranted only when obtaining its key needs something those stateless providers cannot access:

```csharp
public interface ICallerContext { ReadModelKey AccountKey { get; } }
[Command] public record InspectCaller
{
#pragma warning disable ARCCHR0040 // Key comes from trusted caller identity; https://github.com/Cratis/Arc/issues/3620 tracks acquisition gaps.
    public async System.Threading.Tasks.Task<State> Handle(IDecisionReads reads, ICallerContext caller)
    {
        var state = await reads.Get<State>(caller.AccountKey);
        return state.Instance;
    }
#pragma warning restore ARCCHR0040
}
```

The ARCCHR0040 specification (`Source/DotNET/Chronicle.CodeAnalysis.Specs/for_CommandReadModelParameterAnalyzer/when_a_handler_declares_dependencies.cs`) compiles this declaration and verifies the reasoned suppression. `ICallerContext` is an illustrative application interface, not a new Arc API.

Keep the reason and tracking-feature link beside the declaration. Suppression does not waive concurrency, authorization, or acquisition-failure handling.
