---
title: Cratis Arc command validation and result handling
description: Cratis Arc commands validate input before sending requests and return structured results for handling success, field errors, and other failures.
---

When you call `execute()` on a Cratis Arc command, the core runtime checks input before sending a request and returns a `CommandResult<TResponse>` with the outcome. Inspect the result to show field errors, handle an authorization failure, or continue after success. This behavior does not require React.

## Client-side validation

Before `execute()` sends a request, it runs any validator attached to the command, then checks required non-route properties for `null` or `undefined`. A blocking validation failure returns a result with `isValid` and `isSuccess` set to `false`, without contacting the backend.

The proxy generator extracts supported rules from backend FluentValidation validators and DataAnnotations. This gives the client early feedback, but does not replace backend validation. An error blocks by default; a model-bound command can declare a stricter severity threshold.

The following example assumes the backend validator rejects an empty email and an age below 18. Presence checks alone would not reject these values.

```typescript
const command = new CreateUserCommand();
command.email = '';
command.age = 15;

const result = await command.execute();
// result.isValid === false
// result.validationResults contains validation errors
```

For the rule model, read the [validation overview](../validation/index.md). To check input without executing the handler, use [command preflight validation](./validation.md).

## Result shape

Use `result.isSuccess` to decide whether to continue the successful workflow. `isValid` answers a narrower question: a valid command can still fail authorization or encounter an exception. If the backend returns a response value, it is available through `result.response`.

The result also exposes `isAuthorized`, `hasExceptions`, `validationResults`, and `exceptionMessages` so you can choose the appropriate feedback. [Command Result](./command-result.md) documents the full shape and callback helpers.

## Error categories

A validation failure means the input did not pass the applicable rules. An authorization failure means the operation was not allowed; show that separately from a field error. Exception messages describe backend failures or failures caught by the client runtime.

The core runtime turns a missing endpoint, a rejected fetch, or an unreadable JSON response into a failed `CommandResult` with exception messages. It does not set a request timeout or map every HTTP error status to a distinct result category. Check the result even when the call does not throw.

## See also

Choose fields or callbacks for feedback in [Command Result](./command-result.md), or add checks before submission with [command validation](./validation.md). For hook-managed state in a React component, continue with [React Commands](../../react/commands/index.mdx).
