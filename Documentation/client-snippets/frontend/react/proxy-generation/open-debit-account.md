```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Concepts;

public record AccountName(string Value) : ConceptAs<string>(Value)
{
    public static readonly AccountName NotSet = new(string.Empty);
}

public record AccountBalance(decimal Value) : ConceptAs<decimal>(Value)
{
    public static readonly AccountBalance NotSet = new(0m);
}

public interface IAccountService
{
    Task<Guid> Open(AccountName name, AccountBalance initialBalance);
}

[Command]
public record OpenDebitAccount(AccountName Name, AccountBalance InitialBalance)
{
    public Task<Guid> Handle(IAccountService accounts) =>
        accounts.Open(Name, InitialBalance);
}
```
