---
title: Command operations that invoke Screenplay abilities in Cratis Arc
description: Back a Cratis Arc command operation with the abilities of a Screenplay system, run them before commit, compensate when the command does not commit, and substitute them in specifications.
---

A Screenplay model can say that a command's operation phases call abilities of an external system. This page shows how an [Arc command operation](index.md) is written so that it runs those abilities inside the command request, and how Arc reads the operation back into a model.

The model side is documented in the Screenplay documentation: [operations](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/operations.md) and [invoking an ability](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/systems.md#invoke-an-ability). How an ability reaches an extension host is documented by Stage in [ability invocations](https://github.com/Cratis/Stage/blob/main/Documentation/reference/ability-invocations.md).

## What the model says

```screenplay
produces operation ReserveOrderRevenue
  uses Ledger
  orderId OrderId = orderId
  amount Money = total
  execute
    invokes Ledger.ReserveRevenue
      orderId = orderId
      amount = amount
  compensate
    invokes Ledger.ReleaseRevenue
      orderId = orderId
```

These invocations run in the command request through `ICommandOperation`. They are not durable messages, a failed one fails the command, and their compensation follows the commit disposition described in [execution and recovery](reference.md). Their results never feed the command's events: operations run after the events are enrolled, and a decision must not depend on an answer from outside. Work that should happen after the command commits belongs in a reaction to the committed event.

## Write the operation

A system becomes one interface whose methods are its abilities. Mark it with `ISystemAbilities` and put the system's name in the interface name: `ILedger` is the system `Ledger`.

```csharp
public interface ILedger : ISystemAbilities
{
    Task ReserveRevenue(OrderId orderId, Money amount, CancellationToken cancellationToken);

    Task ReleaseRevenue(OrderId orderId, CancellationToken cancellationToken);
}
```

The operation takes the interface as a parameter of `Execute` and `Compensate`, like any other [operation dependency](implementing.md), and calls one ability per statement:

```csharp
public record ReserveOrderRevenue(OrderId OrderId, Money Amount) : ICommandOperation
{
    public Task Execute(ILedger ledger, CancellationToken cancellationToken) =>
        ledger.ReserveRevenue(OrderId, Amount, cancellationToken);

    public Task Compensate(ILedger ledger, CancellationToken cancellationToken) =>
        ledger.ReleaseRevenue(OrderId, cancellationToken);
}
```

Execution, ordering, the compensation budget and what happens when a commit is rejected are exactly those of every other operation. Nothing about abilities changes them.

### Implement the collaborator

Derive the implementation from `SystemAbilities`. It names the system and hands each call to `IAbilityInvoker`:

```csharp
public class LedgerAbilities(IAbilityInvoker invoker, ICommandContextAccessor contextAccessor)
    : SystemAbilities(invoker, contextAccessor), ILedger
{
    protected override string System => "Ledger";

    public Task ReserveRevenue(OrderId orderId, Money amount, CancellationToken cancellationToken) =>
        Invoke("ReserveRevenue", new Dictionary<string, object?> { ["orderId"] = orderId, ["amount"] = amount }, cancellationToken);

    public Task ReleaseRevenue(OrderId orderId, CancellationToken cancellationToken) =>
        Invoke("ReleaseRevenue", new Dictionary<string, object?> { ["orderId"] = orderId }, cancellationToken);
}
```

Register it as a scoped service. `Invoke` only works while an operation is executing or compensating and throws `InvalidCommandOperation` anywhere else.

### The invoker is the seam to the extension host

Arc ships no `IAbilityInvoker`. A host registers the one that hands the invocation to its extension host; Stage's runtime does this with its ability backend. Each `AbilityInvocation` carries:

| Member | Meaning |
| --- | --- |
| `System`, `Ability` | The names declared in the model. |
| `Inputs` | The typed inputs by name. |
| `IdempotencyKey` | A key that is the same for the same call in the same command execution, never includes an attempt, and differs between `Execute` and `Compensate`, so repeating a call finds the same effect once. |
| `Causation` | The `command`, `correlationId`, `operation` and `phase` (`execute` or `compensate`) that caused the call. |

The invoker returns an `AbilityInvocationResult`. An outcome of `failed`, or an exception, fails the command with `AbilityInvocationFailed` and compensation follows the commit disposition. A declared business outcome other than `failed` is returned to the operation, which decides what it means.

The key is built from the command's correlation identity. A client that retries a command as a new request gets a new correlation identity, so give the ability itself a natural idempotency of its own when the same business request can arrive twice.

## Read the operation back into a model

Arc's Screenplay generator ([Cratis/Arc#2887](https://github.com/Cratis/Arc/issues/2887)) reads an operation back into `invokes` when every statement of `Execute` and `Compensate` calls an ability of an `ISystemAbilities` parameter with values taken from the operation's own inputs. It then declares each system with the abilities that are called, typing their inputs from the method parameters.

- An operation may call more than one system. The model's `uses` names the first one and each `invokes` names its own system; the compiler accepts this.
- A phase that does anything else, such as logging, branching or a literal argument, leaves the operation in code and reports why. The model never says less than the code does.
- A trailing `Async` on a method name is dropped from the ability name.
- The effect class (`safe`, `idempotent`, ...) cannot be read from C#, so none is declared. Add it in the model.

> [!NOTE]
> Screenplay currently refuses to bind operations that invoke abilities: operations are admitted by no executable model yet (Cratis/Screenplay#301), and invocations from operations name Cratis/Screenplay#1252 in the `PLAY0268` it reports. The generated model compiles as authoring syntax, so it is reviewable and round-trips. Stage cannot render it into an application until that admission lands. The Arc runtime on this page does not depend on it.

## Specify it

The collaborator is an ordinary interface, so a specification substitutes it. `given Ledger.ReserveRevenue fails` becomes a substitute whose method throws, and the command fails with the operation's compensation running:

```csharp
ledger.ReserveRevenue(Arg.Any<OrderId>(), Arg.Any<Money>(), Arg.Any<CancellationToken>())
    .ThrowsAsync(new AbilityInvocationFailed("Ledger", "ReserveRevenue", "unavailable"));
```

To check the invocation itself, register a recording `IAbilityInvoker` and assert on the `AbilityInvocation` it received: the system, the ability, the inputs and the causation. Use `CommandScenario` with the substitute registered to check order, compensation and the command outcome together, as described in [testing command operations](../../testing/command-operations.md).
