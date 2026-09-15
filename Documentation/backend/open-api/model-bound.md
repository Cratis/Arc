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
2. Attempts to infer a `CommandResult` / `CommandResult<T>` 200 response schema from the registered handler adapter's `Handle` method.
3. Adds 400, 403, and 500 error response schemas.

> [!WARNING]
> The current transformer inspects the adapter type, not the command record's actual `Handle()` return contract. The standard `ModelBoundCommandHandler` exposes `ValueTask<object?>`; inference does not recover your concrete response payload from that adapter. Do not assume the generated schema describes a returned DTO, `Guid`, or typed result correctly. Runtime command response behavior is unchanged; verify the schema against an actual response before client generation.

Command type illustration (application persistence is deliberately outside this example):

```csharp
using Cratis.Arc.Commands.ModelBound;

[Command]
public record EchoInvoiceId(Guid Id)
{
    public Guid Handle() => Id;
}
```

## Query operations

The `ModelBound.QueryOperationTransformer` attempts to match operations whose `operationId` starts with `Execute` against registered `IQueryPerformerProviders`.

Current limitation: it compares the suffix to `performer.Name`, while the runtime GET mapper uses `performer.FullyQualifiedName` in endpoint names. A normal model-bound method name and its fully qualified name differ, so this transformer can miss the generated operation. The additional HTTP `QUERY` endpoint is excluded from API description by its request reader. Inspect the generated document rather than assuming all query arguments and result schemas below were applied.

For matched operations it:

1. Adds each query parameter from the performer's parameter list as a query string parameter, currently with `Required = false` even when runtime binding requires it.
2. Adds paging and sorting parameters when `IQueryPerformer.SupportsPaging` is `true`.
3. Sets the 200 response schema to `QueryResult`.
4. Adds 400, 403, and 500 error response schemas.

## Pagination and sorting parameters

For query performers that support paging, the following query parameters are added:

| Parameter | Type | Description |
| ----------- | ------ | ------------- |
| `sortby` | `string` | Field name to sort by |
| `sortDirection` | `string` (`asc` \| `desc`) | Sort direction |
| `pageSize` | `integer` | Number of items per page |
| `page` | `integer` | Page number (0-based) |
