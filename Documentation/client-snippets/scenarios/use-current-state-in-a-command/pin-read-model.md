```csharp
void Establish() =>
    _scenario.Given.ForEventSource(_accountId)
        .ReadModel(new AccountBalance(150m));
```
