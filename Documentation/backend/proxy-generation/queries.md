---
title: Query proxy generation
description: Query discovery, parameter names, hook tuples, and paging eligibility.
---

## Discovery and output

The generator supports [controller-based queries](../queries/controller-based/index.md) with `[HttpGet]` and eligible static query methods on model-bound `[ReadModel]` types. Not every static helper becomes a query: special-name and open generic methods are excluded, and the return shape must qualify. Injected dependencies are not client parameters. See [model-bound queries](../queries/model-bound/index.md) for backend definitions.

One-shot queries return a scalar model or collection. Observable return shapes such as `IObservable<T>` and `ISubject<T>` generate observable clients. Their transport can be SSE or WebSocket; the backend source must actually emit updates. No Chronicle integration is required.

For a query method named `AuthorsByName` with client parameters, the generator emits:

- `AuthorsByNameParameters` (**no `I` prefix**).
- `AuthorsByName`, extending `QueryFor<TResult, AuthorsByNameParameters>` or `ObservableQueryFor<TResult, AuthorsByNameParameters>`.
- A validator when extractable rules exist, plus route and parameter metadata.

A parameterless query has no parameters interface and uses the base class's default parameter type. Collection results use `TModel[]`. The default output is a file named after the query method; [source-file grouping](Configuration/basic.md#source-file-as-output-file) can change that filename.

Controller methods marked `[AspNetResult]` are excluded from normal proxy discovery.

## React return tuples

These are API shapes, **not complete application examples**. `result` is a `QueryResultWithState<TResult>` in every row.

| Generated hook                                                      | Return tuple                                          |
| ------------------------------------------------------------------- | ----------------------------------------------------- |
| One-shot `use()` / `useSuspense()`                                  | `[result, perform, setSorting]`                       |
| One-shot enumerable `useWithPaging()` / `useSuspenseWithPaging()`   | `[result, perform, setSorting, setPage, setPageSize]` |
| Observable scalar `use()` / `useSuspense()`                         | `[result]`                                            |
| Observable enumerable `use()` / `useSuspense()`                     | `[result, setSorting]`                                |
| Observable enumerable `useWithPaging()` / `useSuspenseWithPaging()` | `[result, setSorting, setPage, setPageSize]`          |

Destructure the tuple before reading `result.data`. State includes `isPerforming`, `isReady`, `isSuccess`, `isAuthorized`, `isValid`, `validationResults`, `hasExceptions`, `exceptionMessages`, and `paging`. There is no generated `isLoading` or `error` property, nor a connection-state property on this result.

For parameterized proxies, `use(args)` takes the generated parameters object; enumerable queries may also accept sorting after it. For parameterless enumerable proxies, sorting is the first argument. `useWithPaging(pageSize, …)` inserts page size before those arguments.

## Paging and change streams

Only **enumerable** proxies receive `useWithPaging()`, `useSuspenseWithPaging()`, and generated sort helpers. This is a client API eligibility rule, not proof that your backend applies paging. Ordinary automatic query-pipeline paging uses `IQueryable<T>`; an observable provider such as Arc's MongoDB `Observe()` can apply paging through `QueryContext` instead. See [backend paging](../queries/model-bound/paging.md).

Only observable enumerable proxies receive `useChangeStream()`. Its argument positions differ:

| Proxy shape   | Call shape                                  |
| ------------- | ------------------------------------------- |
| Parameterized | `useChangeStream(args?, getKey?, sorting?)` |
| Parameterless | `useChangeStream(getKey?, sorting?)`        |

Thus `useChangeStream(undefined, getKey)` fits a parameterized proxy, not a parameterless one. This hook returns a `ChangeSet<TModel>`, not a query-result tuple. See [change streams](../../frontend/react/queries/change-stream.md).

## Routes and HTTP methods

[Routing options](Configuration/routing.md) govern conventional model-bound routes. Controller routes and explicit query `[Path]` values follow their own declarations. Name-skipping uses the **query method name**, not the read-model type name, and conflict fallback can restore it.

For **model-bound queries**, `[QueryHttpMethod]` metadata can select `Get`, `Query`, or `Auto` in generated clients. The method attribute takes precedence over the read-model attribute. Controller query discovery does not extract this metadata, so the attribute does not configure generated controller clients. See [using the HTTP QUERY method](../queries/using-the-http-query-method.md) for runtime and infrastructure requirements.

Continue with [React query usage](../../frontend/react/queries/usage.md) and [validation extraction](validation.md).
