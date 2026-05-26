# Implementation Example

```csharp
public class IdentityDetailsProvider : IProvideIdentityDetails
{
    public Task<IdentityDetails> Provide(IdentityProviderContext context)
    {
        var result = new IdentityDetails(true, new { Hello = "World" });
        return Task.FromResult(result);
    }
}
```
