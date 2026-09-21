```csharp
void Establish() =>
    _scenario.Given.ForEventSource(_accountId)
        .Events(new MoneyDeposited(100m), new MoneyDeposited(50m));
```
