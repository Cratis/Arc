# Commands

Arc commands follow a request/response pattern where every HTTP handler that performs a mutation returns a `CommandResult` or `CommandResult<T>` envelope. The `CommandResultOperationTransformer` automatically updates the generated operation documentation to reflect this.

## What the transformer does

For every endpoint whose controller action or method is identified as a command (not marked with `[AspNetResult]`), the transformer:

1. Replaces the 200 response schema with `CommandResult` (for `void`/`Task` returns) or `CommandResult<T>` (for typed returns).
2. Adds standard error response schemas for 400, 403, and 500 status codes — all using the same `CommandResult`/`CommandResult<T>` schema so clients only need to handle one type.

## Response status codes

| Status code | Meaning |
|-------------|---------|
| 200 | Command executed successfully |
| 400 | Validation error or malformed payload |
| 403 | Forbidden — insufficient permissions |
| 500 | Unexpected server error |

## Concept return types

If the command returns a concept (a type inheriting from `ConceptAs<T>`), the transformer unwraps the concept to its underlying primitive type before generating the `CommandResult<T>` schema.

```csharp
public record InvoiceId(Guid Value) : ConceptAs<Guid>(Value);

// Controller action — return type is InvoiceId
[HttpPost]
public Task<InvoiceId> CreateInvoice(CreateInvoice command) { ... }
```

The documented 200 response will use `CommandResult<string>` (uuid format) rather than `CommandResult<InvoiceId>`.

## Opting out

Decorate the action with `[AspNetResult]` to bypass the transformer and expose the raw return type directly:

```csharp
[HttpPost]
[AspNetResult]
public Task<InvoiceId> CreateInvoice(CreateInvoice command) { ... }
```

