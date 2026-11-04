---
title: Command and query integration
description: Understand how generated frontend commands and queries apply backend validation metadata.
---

Commands and queries run their attached client validators before sending a request. If local checks block the operation, the returned result can describe that failure without contacting the server. Passing those checks does not establish server authorization or guarantee that the backend will accept the operation.

The examples below assume you have already generated and imported the endpoint proxies, with their routes, required inputs and supported validators. They do not define the endpoints or supply package and routing setup. A manually constructed core command also does not read configuration from a React provider.

## Commands

This fragment assumes `CreateUserCommand` has attached rules that reject the shown values. Defining the validator class on the rules page alone does not configure this command. Supply any other required command values before execution; the invalid-result comment depends on the command's actual rules and inputs.

```typescript
const command = new CreateUserCommand();
command.email = '';
command.age = 15;

const result = await command.execute();
// result.isValid === false
```

## Queries

Here, `SearchUsersQuery` must already have a generated parameter shape containing the shown search and age fields, with attached rules that reject these values. Assigning the parameters supplies input to those checks; it does not define the rules.

```typescript
const query = new SearchUsersQuery();
query.parameters = { searchTerm: 'ab', minAge: -5 };

const result = await query.perform();
// result.isValid === false
```

## Backend-governed validation

Proxy generation reads validation metadata from discoverable backend endpoints and emits typed validators for the supported rules. This lets the browser reuse simple backend checks without a separately maintained client definition. Custom predicates, conditional rules and checks that depend on backend services must not be assumed to have equivalent client behavior.

The backend remains responsible for its full validation and authorization when the request reaches it. Inspect the returned command or query result even when local validation passed.

See [Backend Command Validation](../../../backend/csharp/commands/validation.md) and [Backend Query Validation](../../../backend/csharp/queries/validation.md) for the backend pattern. Those pages use different examples; they do not define the user endpoints assumed by the fragments above.

## Related

- [Rules And Fluent API](./rules.md)
- [Validation Results](./results.md)
- [Severity Filtering](./severity-filtering.md)
