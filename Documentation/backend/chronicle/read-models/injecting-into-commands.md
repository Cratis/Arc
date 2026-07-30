---
title: Read models in commands
description: Take a Chronicle read model as a dependency in a CommandValidator, a Provide method, or a Handle method — and declare what a missing instance means.
---

A command can take the read model Arc resolved for its key in three places: the constructor of a `CommandValidator<TCommand>`, a `Provide()` method, and a `Handle()` method. All three resolve from the same command scope, so all three see the same instance.

Which one to use is a question about *what the state is for*.

| Position | Use it when | The state is… |
|---|---|---|
| `CommandValidator<TCommand>` | The command should be rejected with a message | a gate |
| `Handle()` | The event you produce is computed from the state | an input |
| `Provide()` | The state has to be combined with fetched data before the decision | an input to acquisition |

## In a validator

Validators run before the handler, which makes them the natural place for state-based rejection. The read model is an ordinary constructor dependency:

```csharp
[Command]
public record SettleLedger(EventSourceId LedgerId)
{
    public LedgerSettled Handle(LedgerBalance balance) => new(balance.Balance);
}

public class SettleLedgerValidator : CommandValidator<SettleLedger>
{
    public SettleLedgerValidator(LedgerBalance balance) =>
        RuleFor(command => command.LedgerId)
            .Must(_ => balance.Balance > 0)
            .WithMessage("Ledger has no funds to settle.");
}
```

Validators are discovered by convention — there is nothing to register. Their messages reach the client through `CommandResult` like any other validation error.

:::note[Validators need the Arc command pipeline]
Read-model injection into a validator works for commands that run through the Arc command pipeline — minimal-API command endpoints (the default) and `ICommandPipeline` directly. It does **not** work through MVC controllers, because MVC model validation runs during model binding, before the command context exists and therefore before there is an event source id to resolve by. The request fails with [`ReadModelValidatorRequiresCommandPipeline`](./failures.md#readmodelvalidatorrequirescommandpipeline). Expose the command through a minimal-API endpoint, or move the check into `Handle()`.
:::

## In `Handle()`

When the state is an input to the event rather than a gate on it, take it in the handler:

```csharp
[Command]
public record UseReducerReadModelInHandle(EventSourceId AccountId)
{
    public BalanceRecorded Handle(ReducerAccountSummary summary) => new(summary.Balance);
}
```

This works identically whether `ReducerAccountSummary` is materialized by a reducer, a fluent projection, or a model-bound projection.

## In `Provide()`

`Provide()` acquires the data `Handle()` needs, and its return value is passed to `Handle()` as an argument. A read model can be one of `Provide`'s own inputs:

```csharp
[Command]
public record ProvideReadModelDependencyCommand(EventSourceId AccountId)
{
    public ProvidedAccountBalance Provide(AccountBalanceReadModel readModel) =>
        new(readModel.Balance);

    public ReadModelDependencyProvided Handle(ProvidedAccountBalance balance) =>
        new(balance.Value);
}
```

Use this shape when the projected state has to be combined with something fetched — a rate, a policy, an external lookup — before `Handle()` can decide. For plain validation, prefer a validator; `Provide()` exists to keep IO out of the decision, not to host rules. See [Provide data to a command handler](../../../scenarios/provide-data-to-a-command.md).

## Nullable means you handle absence

The key on a command identifies *which* read model instance to resolve. It does not prove that instance exists. Nullability is how you declare what absence means, and Arc behaves differently for each choice.

### Nullable — absence is a business condition

Declare the parameter nullable when "does not exist" is a state your rule is written around. Arc injects `null` and your code decides:

```csharp
[Command]
public record RegisterCustomer([Key] Guid CustomerId, string Name);

public class RegisterCustomerValidator : CommandValidator<RegisterCustomer>
{
    public RegisterCustomerValidator(Customer? customer) =>
        RuleFor(_ => customer)
            .Null()
            .WithMessage("Customer is already registered");
}
```

The mirror image — reject when the entity is *missing* — is the same shape with the rule inverted, and `When` guards the rules that dereference it:

```csharp
public class AssignPersonToRoleValidator : CommandValidator<AssignPersonToRole>
{
    public AssignPersonToRoleValidator(RoleReadModel? role)
    {
        RuleFor(_ => role)
            .NotNull()
            .WithMessage("Role does not exist");

        When(_ => role is not null, () =>
        {
            RuleFor(command => command.PersonId)
                .Must(personId => !role!.AssignedPersonIds.Contains(personId))
                .WithMessage("Person is already assigned to this role");

            RuleFor(command => command)
                .Must(_ => role!.Status == RoleStatus.Active)
                .WithMessage("Cannot assign people to inactive roles");
        });
    }
}
```

### Non-nullable — the projection is required

Keep the parameter non-nullable when the command genuinely requires the projection and its absence is a fault, not an outcome. Arc then fails the command with [`ReadModelDoesNotExistForCommand`](./failures.md#readmodeldoesnotexistforcommand) before your code runs, and you write rules against the state directly:

```csharp
[Command]
public record SubmitOrder([Key] Guid OrderId);

public class SubmitOrderValidator : CommandValidator<SubmitOrder>
{
    public SubmitOrderValidator(OrderReadModel order)
    {
        RuleFor(_ => order.Status)
            .Equal(OrderStatus.ReadyForSubmission)
            .WithMessage("Only orders that are ready for submission can be submitted");

        RuleFor(_ => order.Lines)
            .NotEmpty()
            .WithMessage("Order must have at least one line");
    }
}
```

A missing `OrderReadModel` here is not a validation outcome — it is a rejected command, because the validator declared the projection required.

### The analyzer makes the choice explicit

[ARC0006](../../code-analysis/ARC0006.md) reports a warning on every non-nullable command-scoped read model parameter, in a validator, `Provide()`, or `Handle()`. It is not saying non-nullable is wrong — it is making sure the required-state choice was made deliberately rather than by default.

The same nullability rules apply in all three positions:

```csharp
[Command]
public record UseNullableReducerReadModelInHandle(EventSourceId AccountId)
{
    public ReadModelAbsenceRecorded Handle(ReducerAccountSummary? summary) => new(summary is null);
}
```

## Combining with an aggregate root

A command can take both — projected state as context, and the aggregate as the thing that changes:

```csharp
[Command]
public record AddItemToCart([Key] Guid CartId, Guid ProductId, int Quantity)
{
    public ItemAddedToCart Handle(
        ShoppingCart cart,                  // aggregate root — emits the events
        ShoppingCartSummary? summary,       // read model — projected context
        ILogger<AddItemToCart> logger)
    {
        logger.LogAddingItem(summary?.TotalItems ?? 0);
        cart.AddItem(ProductId, Quantity);

        return new ItemAddedToCart(ProductId, Quantity);
    }
}
```

Read models never emit events. If the decision must hold under concurrency, drive it from the aggregate or from a Chronicle [constraint](/chronicle/constraints/) rather than from projected state — read models are eventually consistent.

## Read models from other providers

Injection is not Chronicle-only. A read model backed by Entity Framework Core or MongoDB is injected into a command exactly the same way, and everything on this page — the three positions, and what nullability means — applies unchanged.

What differs is where the read model is loaded from and what key loads it, including how a command declares its key when there is no Chronicle to resolve one. See [Read models from other providers](./other-providers.md).

## Testing

Seed the state the command should see with the `Given` builder — either the events behind it or a pinned instance — and execute through the real pipeline. See [Testing with Chronicle](../../testing/chronicle.md#testing-commands-that-take-read-model-dependencies).

## See also

- [Use current state in a command](../../../scenarios/use-current-state-in-a-command.md) — the short recipe.
- [When resolution fails](./failures.md) — every error and what it means.
- [Command validation](../../commands/validation.md) — the rest of Arc's validation model.
