# ARCCHR0039: Prefer plain read-model parameters in Handle

## What it means

A public instance `Handle` method on a `[Command]` takes `IDecisionReads` (or an implementation) or `DecisionRead<T>`. Both APIs remain supported. This **Info** diagnostic recommends separating acquisition from the decision; it applies in `Off`, `Measure` and `On`, including async handlers. It is signature advice, not a runtime guard change.

## Recommended shape

Take the model as a plain parameter. Given an admitted `LedgerBalance` read model, replace this advanced handler:

```csharp
public LedgerSettled Handle(DecisionRead<LedgerBalance> read) =>
    new(read.Instance!.Balance);
```

with plain state under the command's read protection policy:

```csharp
[Command, ReadProtection(ReadProtectionMode.On)]
public record SettleLedger(LedgerId Ledger)
{
    public LedgerSettled Handle(LedgerBalance balance) => new(balance.Balance);
}
```

Use these shapes in order:

1. A plain read-model parameter for the command's event source.
2. `[LookUpBy(nameof(Command.Property))]` when a command property supplies another key.
3. A per-stream model when the state belongs to the command route.
4. `IDecisionReads.Get<T>` in `Provide()` only when no parameter can express the key, returning plain values to `Handle()`.

For example, when identity-derived or ambient data supplies the key:

```csharp
public async Task<LedgerBalance?> Provide(IDecisionReads reads, ICurrentLedger current) =>
    (await reads.Get<LedgerBalance>(current.Key)).Instance;

public LedgerSettled Handle(LedgerBalance? balance) => new(balance?.Balance ?? 0m);
```

These fragments assume application-owned ledger types and `ICurrentLedger.Key` returning a `ReadModelKey`. A real protected read still issues its token and enrolls before `Provide()` returns plain state. The handler does not need the token to retain protection. No diagnostic is reported for acquisition in `Provide()`, validator constructors, ordinary services, unrelated types with the same short names, or generated code.

## Specify the decision

Application specs give state and assert outcomes. Call `Handle(readModel)` directly, or use `CommandScenario<T>` and `Given.ForEventSource(id).ReadModel(state)`, including for a protected plain parameter. Specs do not configure decision reads or simulate competing appends: concurrency handling is proved by Arc and Chronicle's framework specs. Arc does not add application test support for non-recommended handler shapes.

## Related

- [Injecting read models into commands](../read-models/injecting-into-commands.md)
- [Less idiomatic forms](../consistency/scenarios/less-idiomatic.md)
- [Testing Chronicle commands](../../testing/chronicle.md)
