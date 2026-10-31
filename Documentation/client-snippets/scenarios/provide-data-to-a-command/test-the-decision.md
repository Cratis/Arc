```csharp
[Fact] void should_return_the_assessment() =>
    new AssessLoan(LoanId.New(), ApplicantId.New())
        .Handle(new CreditScore(800))
        .ShouldBeOfExactType<LoanAssessment>();
```

For a Chronicle-backed decision, the pipeline spec gives the model its state as well. This illustrative fragment assumes an application-owned `AssessExistingLoan` command with a plain `LoanApplication` parameter, including under read protection `On`:

```csharp
await using var scenario = new CommandScenario<AssessExistingLoan>();
scenario.Given.ForEventSource(loanId).ReadModel(application);
var result = await scenario.Execute(new AssessExistingLoan(loanId));
result.ShouldBeSuccessful();
```

Application specs give state and assert outcomes; they do not configure decision reads or simulate concurrent writers.
