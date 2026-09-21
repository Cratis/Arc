```csharp
[Fact] void should_return_the_assessment() =>
    new AssessLoan(LoanId.New(), ApplicantId.New())
        .Handle(new CreditScore(800))
        .ShouldBeOfExactType<LoanAssessment>();
```
