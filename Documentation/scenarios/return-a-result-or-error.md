---
title: Return a result or an error
description: Return ordinary response data or a typed validation failure from a standalone Arc command, and distinguish response handling from optional Chronicle event persistence.
---

**Goal:** explicitly write through a database or service, then return a value the caller needs — or a typed rejection. Arc.Core does not append events just because a returned object has an event-like name.

## Choose a standalone result

| You want to…                           | Return                                                        |
| -------------------------------------- | ------------------------------------------------------------- |
| Complete without response data         | `void`, `Task`, or `ValueTask`                                |
| Give the caller a value                | that value, or `Task<T>` / `ValueTask<T>`                     |
| Succeed with data or reject validation | `Result<TResponse, ValidationResult>` (possibly asynchronous) |
| Return multiple ordinary values        | a tuple; review the generated response contract               |

The following **handler replacement** uses the [MongoDB tutorial's setup and domain types](/arc/backend/getting-started/your-first-command/). Add `using Cratis.Monads;` and `using Cratis.Arc.Validation;` to the command file:

```csharp
public async Task<Result<AuthorId, ValidationResult>> Handle(IMongoCollection<Author> authors)
{
    if (await authors.Find(author => author.Name == Name).AnyAsync())
    {
        return ValidationResult.Error("An author with that name is already registered.", [nameof(Name)]);
    }

    await authors.InsertOneAsync(new Author(Id, Name));
    return Id;
}
```

The insert is the persistent effect; the `AuthorId` is response data. The pre-check gives a friendly failure but is **not atomic**. Keep the database unique index from [validation](/arc/tutorial/validation/) and explicitly translate its duplicate-write failure if you need friendly errors under races. Other storage exceptions remain exception results; do not disguise them as ordinary validation.

For rules that can reject before work begins, prefer a `CommandValidator<T>`; use a typed handler failure when the write/decision itself discovers the failure. `Provide()` can also [short-circuit with typed validation](./provide-data-to-a-command.md).

## Read it on the client

After regenerating the proxy, `execute()` returns a `CommandResult` (a response-bearing result for this signature). Check `isSuccess` before using `response`; otherwise inspect `validationResults`, `isAuthorized`, and exception information. The [command-result contract](/arc/frontend/core/commands/command-result/) documents these independently — success is not synonymous with absence of validation errors.

## Optional: event results with Chronicle

Only after configuring [Arc + Chronicle](/arc/backend/chronicle/) do registered-event response handlers append events. These are **integration signatures**, not replacements for a standalone write:

| Integrated intention                  | Return shape and prerequisite                                                                                   |
| ------------------------------------- | --------------------------------------------------------------------------------------------------------------- |
| Append a registered event             | `AuthorRegistered Handle()`; Chronicle resolves the source from the command                                     |
| Select the source in the response     | `(AuthorId, AuthorRegistered)` only when `AuthorId` derives from Chronicle `EventSourceId` / `EventSourceId<T>` |
| Return caller data alongside an event | `(result, event)`; unhandled values remain response data                                                        |
| Reject or append                      | `Result<AuthorRegistered, ValidationResult>`                                                                    |
| Append several events                 | an enumerable of registered events; use `EventForEventSourceId` for explicit cross-stream targeting             |

The standalone tutorial's `AuthorId : ConceptAs<Guid>` is **not** an event-source identifier. An ordinary `Guid` in a response remains ordinary caller data; do not reinterpret it as an append target. See [Chronicle response identifiers](/arc/backend/chronicle/commands/returning-event-source-id/) for explicit-source contracts.

## See also

- [Commands](/arc/backend/commands/) — supported handlers and pipeline behavior.
- [Validate a command](./validate-a-command.md) — choose the right rejection point.
- [Test a command](./test-a-command.md) — assert the actual write, not just a successful result.
