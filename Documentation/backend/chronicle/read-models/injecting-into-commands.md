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

Injection is not Chronicle-only. Any provider that owns a read model's storage can make its `[ReadModel]` types injectable into a command, resolved by the same key, so a validator, `Provide()`, or `Handle()` takes the read model exactly as it would a Chronicle-backed one.

### Entity Framework Core

A `[ReadModel]` entity carried by a `ReadOnlyDbContext` becomes injectable once the context is registered — there is nothing extra to wire up:

```csharp
[ReadModel]
public class Customer
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CustomerDbContext(DbContextOptions<CustomerDbContext> options) : ReadOnlyDbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
}
```

```csharp
[Command]
public record RenameCustomer([Key] Guid CustomerId, string NewName)
{
    public CustomerRenamed Handle(Customer customer) => new(customer.Id, NewName);
}
```

`WithEntityFrameworkCore()` discovers the `ReadOnlyDbContext`, and the command's resolved key (here the `[Key]` on `CustomerId`) loads the entity by its primary key. The primary key may be a `Guid`, `int`, `long`, `string`, or a `ConceptAs<T>` wrapping one of those.

The nullable rules are identical: a nullable `Customer?` receives `null` when no row exists, and a non-nullable `Customer` fails the command with [`ReadModelDoesNotExistForCommand`](./failures.md#readmodeldoesnotexistforcommand).

### MongoDB

`WithMongoDB()` does the same for the read models MongoDB holds. There is nothing to declare — a `[ReadModel]` becomes injectable, resolved by the document `_id`:

```csharp
[ReadModel]
public record Customer(Guid Id, string Name)
{
    public static IEnumerable<Customer> AllCustomers(IMongoCollection<Customer> collection) =>
        collection.Find(_ => true).ToList();
}
```

```csharp
[Command]
public record RenameCustomer([Key] Guid CustomerId, string NewName)
{
    public CustomerRenamed Handle(Customer customer) => new(customer.Id, NewName);
}
```

The id member is whichever one MongoDB maps to `_id` — a member named `Id` by convention, or the one marked `[BsonId]`. Like the EF primary key it may be a `Guid`, `int`, `long`, `string`, or a `ConceptAs<T>` wrapping one of those. A read model with no member mapped to `_id` cannot be resolved by key, and injecting it fails with `MissingIdMapping`.

### Which provider resolves a read model

More than one provider can be able to load the same read model, and the order an application registers them in should not decide the outcome. What decides it is whether an artifact in the application says the provider *owns* the read model:

| Provider | Owns a read model when | Claims it as |
|---|---|---|
| Chronicle | a projection, model-bound projection, or reducer targets it | declared |
| Entity Framework Core | a `DbSet` on a `ReadOnlyDbContext` carries it | declared |
| MongoDB | — a collection is served for any read model | fallback |

A declaring provider always wins, in either registration order. MongoDB claims only what nothing else resolves, and it also leaves your own registration of a read model type alone. This matters beyond tidiness: Chronicle is the provider that releases a read model's compliance-protected values, so a read model Chronicle projects has to be resolved by Chronicle.

To contribute a provider of your own, implement `ICanResolveReadModelForCommand` — reporting the types it resolves, the `ReadModelForCommandOwnership` it claims them with, and how to load one by key — and register it with `services.AddReadModelsForCommand(...)`.

### Declaring the key without Chronicle

Every provider loads a read model by the command's key, and Chronicle is what resolves that key — from `ICanProvideEventSourceId`, from a property assignable to `EventSourceId`, or from one carrying `Cratis.Chronicle.Keys.KeyAttribute`.

An application without Chronicle has none of those, so Arc reads the key from the command itself. Mark the property holding it with the data annotations `[Key]`:

```csharp
using System.ComponentModel.DataAnnotations;

[Command]
public record RenameCustomer([property: Key] Guid CustomerId, string NewName)
{
    public CustomerRenamed Handle(Customer customer) => new(customer.Id, NewName);
}
```

The key may be a `Guid`, `int`, `long`, `string`, or a `ConceptAs<T>` wrapping one of those — a concept resolves to the value it wraps rather than to its own `ToString()`.

When the key is not one property — a composite of two, or a value derived from them — the command declares it:

```csharp
[Command]
public record MoveItem(Guid CartId, Guid ItemId) : ICanProvideKeyForCommand
{
    public string GetKey() => $"{CartId}/{ItemId}";
}
```

Nothing is inferred from the shape of a command. One carrying two identifiers and marking neither resolves no key, and injection fails as a validation error rather than silently picking one of them.

To key commands your own way across an application, implement `ICanResolveKeyForCommand`. It is discovered automatically and asked before the rule Arc ships, whichever order the two happen to be discovered in.

:::warning[Two attributes are spelled `[Key]`]
In an application **with** Chronicle, the data annotations `[Key]` does nothing. Chronicle resolves keys from `Cratis.Chronicle.Keys.KeyAttribute`, invents a fresh event source id when it finds no key property, and every read model keyed by that command then resolves to nothing. `ARCCHR0008` reports it, so this is a build warning rather than a puzzling "the entity does not exist" at runtime.
:::

## Testing

Seed the state the command should see with the `Given` builder — either the events behind it or a pinned instance — and execute through the real pipeline. See [Testing with Chronicle](../../testing/chronicle.md#testing-commands-that-take-read-model-dependencies).

## See also

- [Use current state in a command](../../../scenarios/use-current-state-in-a-command.md) — the short recipe.
- [When resolution fails](./failures.md) — every error and what it means.
- [Command validation](../../commands/validation.md) — the rest of Arc's validation model.
