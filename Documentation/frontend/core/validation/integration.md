---
title: Cratis Arc command and query validation
description: Cratis Arc runs attached validators before command and query requests. Handle local failures and keep server validation and authorization authoritative.
---

Cratis Arc runs an attached validator before `command.execute()` or `query.perform()` sends a request. A blocking client result returns validation feedback without contacting the server, so your UI can show what needs correcting before submission.

## Commands

This usage excerpt assumes a generated `CreateUserCommand` proxy whose validator requires an email address and a minimum age of 18, as in the [rule definition example](./rules.md#programmatic-rule-definition). Use your application's generated proxy and import path:

```typescript
const command = new CreateUserCommand();
command.email = '';
command.age = 15;

const result = await command.execute();
// result.isValid === false
```

The attached validator rejects these values. With the default Error-only client filtering, the command returns an invalid `CommandResult` and makes no request. Required-property checks also run before submission when the attached validator has not already blocked it.

`execute()` is an execution attempt: once client checks pass, it sends the command to the server, where the handler can run. If warnings need confirmation, choose a blocking severity before the first attempt; asking after a successful call is too late. Follow the [warning confirmation workflow](./severity-filtering.md#warning-confirmation-workflow) for that case.

## Queries

This excerpt assumes a generated `SearchUsersQuery` whose parameter rules reject a two-character search term and a negative minimum age:

```typescript
const query = new SearchUsersQuery();
query.parameters = { searchTerm: 'ab', minAge: -5 };

const result = await query.perform();
// result.isValid === false
```

`perform()` validates the supplied arguments, or `query.parameters` when no arguments are supplied. Any result from the attached query validator prevents the request, including a Warning or Information result. Queries do not use the command severity override arguments.

## Backend-governed validation

Define shared rules on the backend and let [proxy generation](../../../backend/csharp/proxy-generation/validation.md) extract the supported subset into TypeScript. Change the source rule and regenerate rather than patching generated validators.

The browser does not reproduce arbitrary FluentValidation execution. Custom predicates such as `Must`, conditional rules, and dependency-backed checks stay server-side. A passing local check neither proves server authorization nor guarantees that server validation will pass. Even a local failure's `isAuthorized` value is not evidence of server access: no request may have been made.

For a command preflight, use [command validation](../commands/validation.md), not `execute()`. Preflight has its own limits and is not a promise that a later write will succeed. Keep the complete rules in [Backend Command Validation](../../../backend/csharp/commands/validation.md) and [Backend Query Validation](../../../backend/csharp/queries/validation.md).

## Related

- [Rules And Fluent API](./rules.md)
- [Validation Results](./results.md)
- [Severity Filtering](./severity-filtering.md)
