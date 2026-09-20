```csharp
public Result<CreditScore, ValidationResult> Provide(ICreditBureau bureau)
{
    var score = bureau.GetScore(Applicant);
    return score is null
        ? ValidationResult.Error("No credit history", [nameof(Applicant)])
        : score;
}
```
