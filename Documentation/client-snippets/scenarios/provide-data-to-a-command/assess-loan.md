```csharp
[Command]
public record AssessLoan(LoanId LoanId, ApplicantId Applicant)
{
    public CreditScore Provide(ICreditBureau bureau) => bureau.GetScore(Applicant);

    public LoanAssessment Handle(CreditScore creditScore) => new(LoanId, creditScore);
}
```
