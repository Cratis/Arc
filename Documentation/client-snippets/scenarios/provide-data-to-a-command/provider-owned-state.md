```csharp
public Task<ShippingQuote> Provide(OrderReadModel? order, IShippingRates rates) =>
    order is null
        ? Task.FromResult(ShippingQuote.None)
        : rates.Quote(order.Destination, order.TotalWeight);
```
