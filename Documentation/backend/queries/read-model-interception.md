# Read Model Interception

Read model interception lets you apply cross-cutting operations to every read model instance before it is served to a client. Common uses include decryption, field masking, localization, and audit enrichment. Interceptors run automatically for all query types — controller-based, model-bound, and observable (WebSocket and SSE).

## How It Works

When a query returns, the framework passes each read model instance through every registered interceptor for that type before serializing the response. For collections every item is intercepted individually. For observable queries the interception happens on every emission.

```mermaid
sequenceDiagram
    participant Client
    participant Framework
    participant Interceptor

    Client->>Framework: GET /api/accounts
    Framework->>Framework: Execute query
    loop For each item
        Framework->>Interceptor: Intercept(item)
        Interceptor-->>Framework: (mutated in place)
    end
    Framework-->>Client: QueryResult with intercepted data
```

## Implementing an Interceptor

Implement `IInterceptReadModel<TReadModel>` and register it with the DI container. The framework discovers all implementations automatically.

```csharp
public class DecryptAccountNumbers : IInterceptReadModel<AccountSummary>
{
    readonly IEncryptionService _encryption;

    public DecryptAccountNumbers(IEncryptionService encryption)
    {
        _encryption = encryption;
    }

    public Task Intercept(AccountSummary readModel)
    {
        readModel.AccountNumber = _encryption.Decrypt(readModel.AccountNumber);
        return Task.CompletedTask;
    }
}
```

> **Note:** The `Intercept` method mutates the read model in place — it does not return a value. Arc captures and applies your changes automatically.

### Registration

Register your interceptor in the service collection. Any lifetime (singleton, scoped, or transient) works:

```csharp
services.AddTransient<DecryptAccountNumbers>();
```

If you use `AddSelfBindings()` (the default for Arc applications), no registration is needed — the framework discovers and wires up all `IInterceptReadModel<T>` implementations from your assemblies automatically.

## Multiple Interceptors

You can register any number of interceptors for the same read model type. They run in the order they are discovered.

```csharp
public class MaskSensitiveFields : IInterceptReadModel<AccountSummary>
{
    public Task Intercept(AccountSummary readModel)
    {
        readModel.AccountNumber = $"****{readModel.AccountNumber[^4..]}";
        return Task.CompletedTask;
    }
}

public class EnrichWithLocale : IInterceptReadModel<AccountSummary>
{
    readonly ILocalizationService _locale;

    public EnrichWithLocale(ILocalizationService locale)
    {
        _locale = locale;
    }

    public Task Intercept(AccountSummary readModel)
    {
        readModel.FormattedBalance = _locale.FormatCurrency(readModel.Balance);
        return Task.CompletedTask;
    }
}
```

Both interceptors run for every `AccountSummary` returned by any query.

## Observable Queries

Interceptors apply equally to observable (real-time) queries. Each time the observable emits new data, every item passes through the registered interceptors before the payload is sent to the client.

```csharp
// No changes needed in your observable query — interception is automatic.
[HttpGet("observable")]
public ISubject<IEnumerable<AccountSummary>> GetAccountSummaries()
{
    return _collection.Observe();
}
```

## Type Safety

An interceptor is bound to exactly one read model type through the generic parameter. An interceptor for `AccountSummary` never runs for `TransactionHistory`, even if both are returned by different queries in the same request.
