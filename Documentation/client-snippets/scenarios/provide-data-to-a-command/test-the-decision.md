```csharp
[Fact] void should_return_the_assessment() =>
    new AssessLoan(LoanId.New(), ApplicantId.New())
        .Handle(new CreditScore(800))
        .ShouldBeOfExactType<LoanAssessment>();
```

For a Chronicle-backed decision, keep `Handle` about plain state as well. A pipeline spec uses `CommandScenario<TCommand>` and `Given.ForEventSource(id).ReadModel(state)`, including for a protected plain read-model parameter. Application specs give state and assert outcomes; they do not configure decision reads or simulate concurrent writers.
