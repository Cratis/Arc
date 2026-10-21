```csharp
using var scenario = new CommandScenario<AllocateCartId>();
var cartId = CartId.New();
scenario.PinGeneratedValue("cart", cartId);
var result = await scenario.Execute(new AllocateCartId());
result.ShouldBeSuccessful();
scenario.GeneratedValues.Last<CartId>("cart").ShouldEqual(cartId);
```
