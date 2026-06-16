---
title: Provide data to a command handler
description: Fetch the data a command's decision needs in a Provide method so Handle stays a pure, easily tested function of its arguments.
---

**Goal:** your command's `Handle()` needs data it doesn't carry — a lookup, an external score, some current state — and you don't want that IO tangled into the decision logic, where it's hard to test.

## Lift the IO out of `Handle`

Add a `Provide()` method next to `Handle()` on the command. `Provide` fetches or computes what the decision needs, and its return value is passed into `Handle()` as an argument. `Handle` is left as a pure function of its arguments — given its inputs it returns events or results, with no IO to set up.

`Provide` runs before `Handle`, after validation and authorization pass. Its parameters are resolved from dependency injection just like `Handle`'s, and `this` is the command, so it can read the command's own data.

```csharp
[Command]
public record ApproveLoan(LoanId LoanId, ApplicantId Applicant)
{
    public CreditScore Provide(ICreditBureau bureau) => bureau.GetScore(Applicant);

    public LoanApproved Handle(CreditScore creditScore) => new(LoanId, creditScore);
}
```

## Why this matters: testability

Because the IO lives in `Provide`, testing the decision needs no mocking — construct the command and call `Handle` directly with the values it would have received:

```csharp
[Fact] void should_produce_the_event() =>
    new ApproveLoan(LoanId.New(), ApplicantId.New())
        .Handle(new CreditScore(800))
        .ShouldBeOfExactType<LoanApproved>();
```

`Provide` has a single job — acquire data — so it is easy to test in isolation too, with a stubbed `ICreditBureau`.

## Return more than one value

Return a tuple to feed several `Handle` parameters; each value is matched to a parameter by type:

```csharp
public (CreditScore, RiskBand) Provide(ICreditBureau bureau, IRiskModel risk) =>
    (bureau.GetScore(Applicant), risk.Band(Applicant));

public LoanApproved Handle(CreditScore score, RiskBand band) => new(LoanId, score, band);
```

`Provide` may be synchronous or `async` (`Task<T>` / `ValueTask<T>`).

## Short-circuit before `Handle` runs

`Provide` can stop the command before `Handle` is ever called:

| Return / do | Result |
|---|---|
| `ValidationResult.Error("…")` | command fails validation (HTTP 400) |
| an `AuthorizationResult` that is not authorized | command is unauthorized (HTTP 403) |
| throw | command fails with the exception (HTTP 500) |
| a value | flows into `Handle` as an argument |

Use `Result<,>` to reject or proceed in one method — return the error to stop, or the value to continue:

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

## Cross-cutting values

`Provide` is per-command. For a value that many handlers need — the current tenant's settings, say — register it as a scoped service and take it as a `Handle` parameter. Arc resolves any `Handle` or `Provide` parameter it can't otherwise supply from the container.

## When not to use it

If `Handle` already has everything it needs from the command and its injected services, you don't need `Provide`. Reach for it specifically when a decision depends on data you must fetch — that's where moving the fetch out buys you the testability.

## See also

- [Return a result or an error](./return-a-result-or-error.md) — the shapes `Handle()` itself can return.
- [Test a command](./test-a-command.md) — testing a slice through the real pipeline.
- [Validate a command](./validate-a-command.md) — where `ValidationResult.Error` comes from.
