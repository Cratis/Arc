```csharp
[Command]
public record SettleLedger(LedgerId LedgerId)
{
    public LedgerSettled? Handle(LedgerBalance? balance) =>
        balance is null ? null : new(balance.Balance);
}

[Command]
public record WithdrawFunds(AccountId AccountId, decimal Amount)
{
    public FundsWithdrawn? Handle(AccountBalance? balance) =>
        balance is null ? null : new(Amount, balance.Balance - Amount);
}
```
