```csharp
[Command]
public record OpenAccount(AccountId Id, AccountHolder Owner)
{
    public Task Handle(IMongoCollection<Account> accounts) =>
        accounts.InsertOneAsync(new Account(Id, Owner));
}
```
