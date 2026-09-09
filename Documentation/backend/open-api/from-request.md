# FromRequest Attribute

The `[FromRequest]` attribute lets you combine data from multiple parts of an HTTP request — body, route, and query string — into a single model object. See [FromRequest Attribute](../asp-net-core/from-request.md) for full details on how model binding works.

## OpenAPI documentation

Without special handling, parameters decorated with `[FromRequest]` would appear as individual query/route parameters in the API documentation even though they are bound as a single body object at runtime. The `FromRequestOperationTransformer` and `FromRequestSchemaTransformer` correct this.

### What the transformers do

1. **`FromRequestOperationTransformer`** — For each parameter marked `[FromRequest]`:
   - Removes the parameter from the `parameters` list of the operation.
   - Creates a `requestBody` entry using the parameter's type schema.

2. **`FromRequestSchemaTransformer`** — Ensures the schema for the request body accurately reflects the model's properties, excluding any properties that come from route or query binding.

The schema transformation does not change the runtime merge rule: body values win unless they equal CLR `default(T)`. Removing a property from a schema does not enforce that the value came from the URL. See [binding defaults and precedence](../asp-net-core/from-request.md).

## Example

These are illustrative request/action fragments for an existing MVC controller with application-owned customer types and service:

```csharp
public record UpdateCustomerRequest(
    [property: FromRoute] CustomerId CustomerId,
    string Name,
    string Email);

[HttpPut("{customerId}")]
public Task UpdateCustomer([FromRequest] UpdateCustomerRequest request, [FromServices] ICustomerService customers) =>
    customers.Update(request);
```

The intended operation contract is:

- **Path parameter**: `customerId` (from the route)
- **Request body**: a JSON schema with `name` and `email` properties

The operation transformer replaces the matching request parameter with a body; the schema transformer removes route/query-decorated properties. It does not itself create missing path-parameter declarations, so verify the final document supplied by ASP.NET API Explorer rather than assuming these transformers guarantee the complete operation shape.
