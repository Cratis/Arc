# Model-Bound Operations

Arc supports minimal API-style endpoints for commands and queries, called *model-bound operations*. These endpoints are registered automatically by the Arc infrastructure and follow a convention-based naming scheme (`Execute<TypeName>`).

Because model-bound endpoints do not use traditional controller actions, the standard `CommandResultOperationTransformer` and `QueryResultOperationTransformer` transformers do not apply to them. The `ModelBound.CommandOperationTransformer` and `ModelBound.QueryOperationTransformer` fill this gap.

## Registration

Model-bound transformers are included automatically when you call `AddConcepts()`:

```csharp
builder.Services.AddOpenApi(options => options.AddConcepts());
```

They can also be registered independently:

```csharp
builder.Services.AddOpenApi(options => options.AddModelBoundOperationTransformers());
```

## Command operations

The `ModelBound.CommandOperationTransformer` matches operations whose `operationId` starts with `Execute` and resolves the command type from the registered `ICommandHandlerProviders`.

For matched operations it:

1. Sets the `requestBody` to a schema of the command type.
2. Sets the 200 response schema to `CommandResult` or `CommandResult<T>` (depending on the handler return type).
3. Adds 400, 403, and 500 error response schemas.

```csharp
// The handler is automatically discovered and the endpoint is documented
public class CreateInvoiceCommandHandler : ICommandHandler<CreateInvoice>
{
    public Task Handle(CreateInvoice command, CommandContext context) { ... }
}
```

## Query operations

The `ModelBound.QueryOperationTransformer` matches operations whose `operationId` starts with `Execute` and resolves the query performer from the registered `IQueryPerformerProviders`.

For matched operations it:

1. Adds each query parameter from the performer's parameter list as a query string parameter.
2. Adds paging and sorting parameters when `IQueryPerformer.SupportsPaging` is `true`.
3. Sets the 200 response schema to `QueryResult`.
4. Adds 400, 403, and 500 error response schemas.

## Pagination and sorting parameters

For query performers that support paging, the following query parameters are added:

| Parameter | Type | Description |
|-----------|------|-------------|
| `sortBy` | `string` | Field name to sort by |
| `sortDirection` | `string` (`asc` \| `desc`) | Sort direction |
| `pageSize` | `integer` | Number of items per page |
| `page` | `integer` | Page number (0-based) |

