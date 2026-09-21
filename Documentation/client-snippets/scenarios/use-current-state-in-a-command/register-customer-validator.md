```csharp
public class RegisterCustomerValidator : CommandValidator<RegisterCustomer>
{
    public RegisterCustomerValidator(Customer? customer) =>
        RuleFor(_ => customer).Null().WithMessage("Customer is already registered.");
}
```
