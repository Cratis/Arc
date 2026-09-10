---
title: Provide data to a command handler
description: Fetch the data a command's decision needs in a Provide method so Handle stays a pure, easily tested function of its arguments.
---

**Goal:** your command's `Handle()` needs data it doesn't carry — usually data from another service, such as a credit bureau, pricing service, inventory lookup, risk model, or tenant configuration — and you don't want that IO tangled into the decision logic, where it's hard to test.

## Lift the IO out of `Handle`

Add a `Provide()` method next to `Handle()` on the command. `Provide` fetches or computes the data the decision needs, and its return value is passed into `Handle()` as an argument. `Handle` can then be a pure function of its arguments. To separate the write side as well, return a [command operation](../backend/commands/operations/index.md) describing the required service work, or return events with the optional Chronicle integration. `Provide` does not make persistence automatic.

:::tip[Acquire, decide, then perform]
Use `Provide()` to acquire decision inputs, `Handle()` to decide, and returned command operations to perform inline side effects. Do not move writes into `Provide()` merely to make `Handle()` look pure: such direct writes are outside operation compensation. [Operation testing](../backend/testing/command-operations.md) covers the decision and real execution separately.
:::

`Provide` runs before `Handle`, after validation and authorization pass. Its parameters are resolved from dependency injection just like `Handle`'s, and `this` is the command, so it can read the command's own data.

The following are **alternative domain fragments**, not a complete loan system. `LoanId`, `ApplicantId`, `CreditScore`, `RiskBand`, `LoanAssessment`, and the bureau/risk/rates interfaces are application-owned types. Supply their declarations and DI implementations; import `Cratis.Arc.Commands.ModelBound`, `Cratis.Arc.Validation`, and `Cratis.Monads` for the Arc attributes and result examples. `LoanAssessment` is an ordinary response DTO, not a persisted approval or Chronicle event.

```csharp
[Command]
public record AssessLoan(LoanId LoanId, ApplicantId Applicant)
{
    public CreditScore Provide(ICreditBureau bureau) => bureau.GetScore(Applicant);

    public LoanAssessment Handle(CreditScore creditScore) => new(LoanId, creditScore);
}
```

## Why this matters: testability

Because the IO lives in `Provide`, testing the decision needs no mocking — construct the command and call `Handle` directly with the values it would have received:

```csharp
[Fact] void should_return_the_assessment() =>
    new AssessLoan(LoanId.New(), ApplicantId.New())
        .Handle(new CreditScore(800))
        .ShouldBeOfExactType<LoanAssessment>();
```

`Provide` has a single job — acquire data — so it is easy to test in isolation too, with a stubbed `ICreditBureau`.

## Return more than one value

Return a tuple to feed several `Handle` parameters; each value is matched to a parameter by type:

```csharp
public (CreditScore, RiskBand) Provide(ICreditBureau bureau, IRiskModel risk) =>
    (bureau.GetScore(Applicant), risk.Band(Applicant));

public LoanAssessment Handle(CreditScore score, RiskBand band) => new(LoanId, score, band);
```

`Provide` may be synchronous or `async` (`Task<T>` / `ValueTask<T>`).

## Use cancellation when the work can stop

`Provide` and `Handle` can both take a `CancellationToken`. Arc supplies the current command execution token. For HTTP command endpoints, that is the request-aborted token. For direct `ICommandPipeline` execution, pass the token to the pipeline call.

```csharp
[Command]
public record AssessLoan(LoanId LoanId, ApplicantId Applicant)
{
    public Task<CreditScore> Provide(ICreditBureau bureau, CancellationToken cancellationToken) =>
        bureau.GetScore(Applicant, cancellationToken);

    public Task<LoanAssessment> Handle(CreditScore creditScore, CancellationToken cancellationToken) =>
        Task.FromResult(new LoanAssessment(LoanId, creditScore));
}
```

Use the token for IO or long-running work. You do not register `CancellationToken` in the service container; Arc treats it as part of the command execution context.

## Short-circuit before `Handle` runs

`Provide` can stop the command before `Handle` is ever called:

| Return / do                                     | Result                                      |
| ----------------------------------------------- | ------------------------------------------- |
| `ValidationResult.Error("…")`                   | command fails validation (HTTP 400)         |
| an `AuthorizationResult` that is not authorized | command is unauthorized (HTTP 403)          |
| throw                                           | command fails with the exception (HTTP 500) |
| a value                                         | flows into `Handle` as an argument          |

Use `Result<,>` to reject or proceed when the data acquisition itself determines whether the command can continue — return the error to stop, or the value to continue:

```csharp
public Result<CreditScore, ValidationResult> Provide(ICreditBureau bureau)
{
    var score = bureau.GetScore(Applicant);
    return score is null
        ? ValidationResult.Error("No credit history", [nameof(Applicant)])
        : score;
}
```

A `Provide` value that no `Handle` parameter consumes is almost always a mistake, so the analyzer flags it (ARC0005).

For ordinary command validation, including read-model existence checks, prefer a `CommandValidator<>`. Use `Provide()` when `Handle()` needs acquired data as an input to the decision.

## Provider-owned state can already be available

With a configured by-key read-model provider and a usable command key, `Provide` need not fetch that model again. Arc resolves it as a parameter alongside the services it uses — handy when existing state determines _what_ to fetch. Without those prerequisites, inject your application service or context and fetch explicitly:

```csharp
public Task<ShippingQuote> Provide(OrderReadModel? order, IShippingRates rates) =>
    order is null
        ? Task.FromResult(ShippingQuote.None)
        : rates.Quote(order.Destination, order.TotalWeight);
```

See [Use current state in a command](./use-current-state-in-a-command.md) for how that resolution works and what a nullable parameter means.

## Cross-cutting values

`Provide` is per-command. For a value that many handlers need — the current tenant's settings, say — register it as a scoped service and take it as a `Handle` parameter. Arc resolves any `Handle` or `Provide` parameter it can't otherwise supply from the container.

## When not to use it

If `Handle` already has everything it needs from the command and its injected services, you don't need `Provide`. Reach for it specifically when a decision depends on data you must fetch — most often external or application-service data — where moving the fetch out buys you the testability.

## See also

- [Return a result or an error](./return-a-result-or-error.md) — the shapes `Handle()` itself can return.
- [Test a command](./test-a-command.md) — testing a slice through the real pipeline.
- [Validate a command](./validate-a-command.md) — where `ValidationResult.Error` comes from.
- [Use current state in a command](./use-current-state-in-a-command.md) — the read model Arc resolves for you, in all three positions.
