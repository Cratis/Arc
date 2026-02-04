# Controller Based Commands

You can represent commands as regular ASP.NET Core Controller actions.

```csharp
public record AddItemToCart(string sku, int quantity);

[Route("api/carts")]
public class Carts : ControllerBase
{
    [HttpPost("add")]
    public Task AddItemToCart([FromBody] AddItemToCart command)
    {
        // Logic for handling...
    }
}
```

> **Note**: If you're using Cratis Arc [proxy generator](../proxy-generation/index.md), the method name
> will become the command name for the generated TypeScript file and class.

## Bypassing Command Result Wrappers

By default, controller-based commands return results wrapped in a `CommandResult` structure. If you need to return the raw result from your controller action without this wrapper, you can use the `[AspNetResult]` attribute. For more details, see [Without wrappers](../without-wrappers.md).

## Automatic Validation Endpoints

Controller-based commands automatically get validation endpoints created at application startup. For any POST action with a `[FromBody]` parameter, a corresponding `/validate` endpoint is automatically registered.

**Example:**

```csharp
[Route("api/carts")]
public class Carts : ControllerBase
{
    [HttpPost("add")]
    public Task AddItemToCart([FromBody] AddItemToCart command)
    {
        // Execute the command
    }
}
```

**Automatically Created Endpoints:**

- Execute: `POST /api/carts/add`
- Validate: `POST /api/carts/add/validate` _(created automatically)_

The validation endpoint accepts the same payload as the execute endpoint but only runs validation and authorization filters without executing the command logic.

For comprehensive documentation on command validation, see [Command Validation](command-validation.md).

