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
