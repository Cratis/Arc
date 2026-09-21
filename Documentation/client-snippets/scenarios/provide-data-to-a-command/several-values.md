```csharp
public (CreditScore, RiskBand) Provide(ICreditBureau bureau, IRiskModel risk) =>
    (bureau.GetScore(Applicant), risk.Band(Applicant));

public LoanAssessment Handle(CreditScore score, RiskBand band) => new(LoanId, score, band);
```
