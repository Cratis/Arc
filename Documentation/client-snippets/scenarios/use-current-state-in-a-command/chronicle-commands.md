```csharp
[Command]
public record SettleLedger(LedgerId LedgerId)
{
    public Result<LedgerSettled, ValidationResult> Handle(LedgerBalance? balance) =>
        balance is null
            ? ValidationResult.Error("The ledger does not exist", [nameof(LedgerId)])
            : new LedgerSettled(balance.Balance);
}

[Command]
public record WithdrawFunds(AccountId AccountId, decimal Amount)
{
    public Result<FundsWithdrawn, ValidationResult> Handle(AccountBalance? balance) =>
        balance is null
            ? ValidationResult.Error("The account does not exist", [nameof(AccountId)])
            : new FundsWithdrawn(Amount, balance.Balance - Amount);
}
```
