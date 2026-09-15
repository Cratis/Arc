# Commands

Arc wraps participating controller commands in a `CommandResult` or `CommandResult<T>` envelope. This is not a guarantee about every arbitrary mutation endpoint in an ASP.NET application. The `CommandResultOperationTransformer` automatically updates the generated operation documentation to reflect this.

## What the transformer does

For every endpoint whose controller action or method is identified as a command (not marked with `[AspNetResult]`), the transformer:

1. Replaces the 200 response schema with `CommandResult` (for `void`/`Task` returns) or `CommandResult<T>` (for typed returns).
2. Adds standard error response schemas for 400, 403, and 500 status codes — all using the same `CommandResult`/`CommandResult<T>` schema so clients only need to handle one type.

## Response status codes

| Status code | Meaning |
| ------------- | --------- |
| 200 | Command executed successfully |
| 400 | Validation error or malformed payload |
| 403 | Forbidden — insufficient permissions |
| 500 | Unexpected server error |

## Concept return types

If the command returns a concept (a type inheriting from `ConceptAs<T>`), the transformer unwraps the concept to its underlying primitive type before generating the `CommandResult<T>` schema.

The following **type/action fragments** illustrate a response contract; place the action inside an existing MVC controller and supply the application service:

```csharp
public record InvoiceId(Guid Value) : Cratis.Concepts.ConceptAs<Guid>(Value);
```

```csharp
[HttpPost]
public Task<InvoiceId> CreateInvoice(CreateInvoice command, [FromServices] IInvoiceService invoices) =>
    invoices.Create(command);
```

The transformer constructs `CommandResult<Guid>`; its response member is represented in JSON Schema as a string with UUID format. It does not change a runtime `Guid` response into a different command contract.

## Opting out

Decorate the action with `[AspNetResult]` to bypass the transformer and expose the raw return type directly:

```csharp
[HttpPost]
[AspNetResult]
public Task<InvoiceId> CreateInvoice(CreateInvoice command, [FromServices] IInvoiceService invoices) =>
    invoices.Create(command);
```
