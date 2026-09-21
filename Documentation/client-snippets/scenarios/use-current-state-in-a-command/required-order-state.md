```csharp
public class SubmitOrderValidator : CommandValidator<SubmitOrder>
{
    public SubmitOrderValidator(OrderReadModel order) =>
        RuleFor(_ => order.Status)
            .Equal(OrderStatus.ReadyForSubmission)
            .WithMessage("Only orders that are ready for submission can be submitted.");
}
```
