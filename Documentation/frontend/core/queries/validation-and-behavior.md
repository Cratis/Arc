---
title: Cratis Arc query validation, cancellation, and readiness
description: Handle Cratis Arc query validation, missing arguments, cancellation, and readiness so your TypeScript client distinguishes pending results from failures.
---

Cratis Arc core queries check arguments before sending a request and return results that distinguish validation, authorization, and exceptions. When calling them directly, check readiness too: an observable query with no first result yet is pending, not failed. Cancellation follows a separate path and can reject the request promise instead of returning a result.

## Client-side validation

The proxy generator translates supported backend FluentValidation rules and DataAnnotations into client-side validation. When those rules reject the arguments, `perform()` returns a ready result with `isSuccess: false`, `isValid: false`, and the errors in `validationResults`. No HTTP request is sent.

For a query with generated rules for `searchTerm` and `minAge`, the call looks like this:

```typescript
const query = new SearchUsersQuery();
query.parameters = { searchTerm: 'ab', minAge: -5 };

const result = await query.perform();
// result.isValid === false
// result.validationResults contains validation errors
```

Observable queries run generated validation too. Their `perform()` returns the same kind of invalid result; `subscribe()` delivers it to your callback instead of opening a connection. For example, with a generated rule that rejects a negative `minAge`:

```typescript
const query = new ObserveUsers();

query.subscribe(result => {
    // result.isValid === false when the arguments were rejected,
    // which is distinct from a valid result that simply has no data yet
}, { minAge: -5 });
```

### Required arguments are a separate check

After generated validation passes, the runtime checks `requiredRequestParameters`. An absent property, `undefined`, `null`, or an empty string counts as missing; `0` and `false` do not. If a required argument is missing, the runtime logs a warning and sends no request.

The result depends on how you called the query. `perform()` returns the query's default data with `isSuccess: false`, `isValid: true`, and no validation errors. `subscribe()` delivers the default data as an empty result with both `isSuccess` and `isValid` set to `true`. Both results are ready. These are not validation failures unless a generated rule, such as `notEmpty()`, rejected the arguments first.

In a service or view model, wait until the required arguments exist before calling `subscribe()`. Its empty fallback result cannot tell you whether the caller supplied the arguments. In React, gate automatic subscription with the generated `.when()` wrapper (illustrative fragment):

```tsx
const [books] = BooksForAuthor.when(!!authorId).use({ authorId });
```

`ObservableQueryWhen` is a class returned by `.when()`, not a JSX component. Supply a correctly typed argument/default even while disabled; the condition suppresses automatic subscription, not all query/cache creation. See [conditional queries](../../react/queries/conditional-queries.md).

### Server validation

Client-side rules give feedback before a request; they do not replace server validation. The generator translates supported rules, not arbitrary backend logic. Server rejections use camelCased member names. A concept, a domain type that wraps a value, reports errors against the field carrying it rather than its inner `value` member.

For an HTTP query request, a server validation rejection returns HTTP 400 with a result whose `isValid` and `isSuccess` are `false`. `perform()` preserves the member names and messages in `validationResults`; React query hooks expose those same fields.

Observable subscriptions to model-bound queries deliver one invalid result through the callback and then end that subscription, over WebSocket or SSE in direct or hub mode. Controller-based direct WebSocket and SSE queries also send a terminal validation result rather than treating the rejection as a failed connection. For background on the controller-based direct WebSocket case, see [#3230](https://github.com/Cratis/Arc/issues/3230). Correct the arguments and create a new subscription to try again.

For general validation docs, see [Validation](../validation/index.md).

## Sorting and paging

Set a query instance's `sorting` and `paging` properties with `Sorting` and `Paging` before calling `perform()` or `subscribe()`. For an observable query, subscribe again to apply changed settings to the connection.

For React usage patterns and generated hooks, see [Paging](../../react/queries/paging.md).

## Request cancellation

On a `QueryFor` instance, a new `perform()` call that passes validation and required-argument checks aborts the previous HTTP request before starting its own. Calls on separate instances do not cancel each other. You can also cancel the current request explicitly with `query.abortController?.abort()`.

An `AbortError` from the HTTP request rejects its `perform()` promise rather than resolving to an unsuccessful `QueryResult`. Catch and discard that superseded or explicitly cancelled attempt so it cannot replace a newer result. A call rejected by client-side checks does not abort the previous request, because those checks run first.

`ObservableQueryFor.perform()` does not use this abort-controller behavior. For an active subscription, call the returned subscription's `unsubscribe()` or the query's `dispose()` to disconnect. Calling `subscribe()` again on the same instance disconnects the previous connection, even if the new arguments fail validation.

## Readiness is not success

`isReady: false` means an observable query has not produced a result yet. In that state, `isSuccess` is also `false`, but readiness alone is not a validation or exception failure. When the backend has no current value and is not configured to wait for the first result, an HTTP request returns this state with HTTP 202. A one-shot `perform()` resolves with that result; it does not keep waiting for later updates.

Check readiness before presenting an unsuccessful result as an error. A ready result can still be invalid, unauthorized, or exceptional, and a ready successful result can contain an empty collection. Core query results default `isReady` to `true` when an older server omits the field.

## Error categories

Use the result fields to choose the next action, rather than treating every `isSuccess: false` as the same error:

- `isReady: false`: keep a pending state; it is not evidence of an empty dataset.
- `isValid: false`: use `validationResults` to explain which arguments need correcting.
- `isAuthorized: false`: handle denied access rather than asking the user to change query arguments.
- `hasExceptions: true`: inspect `exceptionMessages` and send available diagnostic details to your logger.

For `QueryFor.perform()`, a non-abort network failure resolves to a ready result with default data, `isSuccess: false`, and `hasExceptions: true`. A response that cannot be parsed as JSON instead returns the generic unsuccessful result with no exception messages. Neither should be displayed as a successful empty dataset.

`ObservableQueryFor.perform()` can reject on a network failure, so handle promise rejection as well as result fields when using that one-shot path. A request's `AbortError` is also a promise rejection for `QueryFor`, as described above, not a validation error.

## See also

- [Query Contracts](./contracts.md)
- [React Queries](../../react/queries/index.md)
