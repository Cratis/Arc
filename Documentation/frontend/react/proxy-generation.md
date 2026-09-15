---
title: Consume generated proxies
description: Use backend-generated command and query contracts from React without a hand-written API client.
---

A backend change should produce a compiler error where your frontend needs updating, not a surprise in production. Arc generates TypeScript command, query, and model types from discovered endpoints. Rebuild the backend, then update your consumers; never edit generated files.

You get completion for command fields and query arguments, typed responses, and compiler-guided repairs after a backend rename. The generated modules also give you concrete contracts to navigate instead of hand-maintained request shapes.

## Set up the frontend

Current generated command and query modules import both `@cratis/arc` and `@cratis/arc.react`, even when you call them imperatively. Install their dependencies in your React app:

```bash
npm install @cratis/fundamentals @cratis/arc @cratis/arc.react react react-dom
```

Mount [Arc](./arc.md) above hook consumers. For build configuration, namespace mapping, source-file grouping, and output ownership, use the canonical [backend proxy-generation reference](../../backend/proxy-generation/index.md).

## Commands use your application services

Arc is a standalone CQRS framework. A command can call an ordinary service and return a response; it does not need events or `IEventLog`.

This backend fragment assumes your application implements and registers `IAccountService`. The interface is application-owned, not an Arc API:

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Concepts;

public record AccountName(string Value) : ConceptAs<string>(Value)
{
    public static readonly AccountName NotSet = new(string.Empty);
}

public record AccountBalance(decimal Value) : ConceptAs<decimal>(Value)
{
    public static readonly AccountBalance NotSet = new(0m);
}

public interface IAccountService
{
    Task<Guid> Open(AccountName name, AccountBalance initialBalance);
}

[Command]
public record OpenDebitAccount(AccountName Name, AccountBalance InitialBalance)
{
    public Task<Guid> Handle(IAccountService accounts) =>
        accounts.Open(Name, InitialBalance);
}
```

The named concepts keep account names and balances distinct in backend signatures. Their generated client properties use `string` and `number`, so the form does not need to construct concept objects. Keep each concept in its own file when organizing your application.

After generation, use the proxy from a React component. This consumer fragment assumes its generated import path is `./generated/OpenDebitAccount`:

```tsx
import { OpenDebitAccount } from './generated/OpenDebitAccount';

export function OpenAccountButton() {
    const [command] = OpenDebitAccount.use({
        name: 'Primary account',
        initialBalance: 500
    });

    async function open() {
        const result = await command.execute();
        if (result.isSuccess) {
            console.log(String(result.response));
        }
    }

    return <button onClick={() => void open()}>Open account</button>;
}
```

An ordinary C# `Guid` response remains a supported response and maps to Fundamentals `Guid`. The [Chronicle integration](../../backend/chronicle/commands/index.md) adds optional event-sourcing behavior; it does not redefine generic Arc responses.

The command hook returns `[command, setValues, clearValues]`. Set required content explicitly. The setter edits properties; it does not reset the change-tracking baseline. See [data binding](./commands/data-binding.md).

## Queries follow the backend result shape

Both [controller-based](../../backend/queries/controller-based/index.md) and [model-bound](../../backend/queries/model-bound/index.md) endpoints generate proxies. Parameterized queries emit a `NameParameters` interface. Query hooks return tuples, not model objects; read the first tuple element's `.data`.

- Ordinary `.use()` returns `[result, perform, setSorting]`.
- Observable enumerable `.use()` returns `[result, setSorting]`.
- Observable single-model `.use()` returns `[result]`.
- Only enumerable proxies expose `.useWithPaging()`, `.useSuspenseWithPaging()`, and, for observables, `.useChangeStream()`.

See [query usage](./queries/usage.md) for argument positions and the complete tuple matrix. Results expose `isPerforming`, `isReady`, `isAuthorized`, `isValid`, `hasExceptions`, and `exceptionMessages`; they do not expose an `error` or `isLoading` property.

## Live and Suspense consumers

Observable proxies subscribe to an emitting backend source. MongoDB observation is one option; Chronicle is another optional integration. A database write does not become a live notification without that source wiring. Both [SSE and WebSocket](./queries/observable-query-multiplexing.md) support direct and shared-hub modes.

All generated queries expose `.useSuspense()`. Suspense handles initial waiting and selected failures, not every unsuccessful result: keep validation/readiness handling in the component. Read [Suspense limitations](./queries/suspense-queries.md) before adding an error boundary or retry control.
