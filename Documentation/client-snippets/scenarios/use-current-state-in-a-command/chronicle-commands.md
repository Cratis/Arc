```csharp
[Command]
public record SettleLedger(LedgerId LedgerId)
{
    public LedgerSettled Handle(LedgerBalance balance) => new(balance.Balance);
}

[Command]
public record WithdrawFunds(AccountId AccountId, decimal Amount)
{
    public FundsWithdrawn Handle(AccountBalance balance) =>
        new(Amount, balance.Balance - Amount);
}
```
