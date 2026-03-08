# FromRequest Attribute

The `[FromRequest]` attribute lets you combine data from multiple parts of an HTTP request — body, route, and query string — into a single model object. See [FromRequest Attribute](../asp-net-core/from-request.md) for full details on how model binding works.

## OpenAPI documentation

Without special handling, parameters decorated with `[FromRequest]` would appear as individual query/route parameters in the API documentation even though they are bound as a single body object at runtime. The `FromRequestOperationTransformer` and `FromRequestSchemaTransformer` correct this.

### What the transformers do

1. **`FromRequestOperationTransformer`** — For each parameter marked `[FromRequest]`:
   - Removes the parameter from the `parameters` list of the operation.
   - Creates a `requestBody` entry using the parameter's type schema.

2. **`FromRequestSchemaTransformer`** — Ensures the schema for the request body accurately reflects the model's properties, excluding any properties that come from route or query binding.

## Example

```csharp
public record UpdateCustomerRequest(
    [property: FromRoute] CustomerId CustomerId,
    string Name,
    string Email);

[HttpPut("{customerId}")]
public Task UpdateCustomer([FromRequest] UpdateCustomerRequest request) { ... }
```

The generated OpenAPI operation will show:

- **Path parameter**: `customerId` (from the route)
- **Request body**: a JSON schema with `name` and `email` properties

Rather than listing all three as individual query/route parameters.

