---
title: React
description: Use generated Arc TypeScript proxies in React to submit commands, read query results and observe live updates through typed hooks and result state.
---

Once your backend has a command to submit or a query to read, use its generated proxy in a React component. Arc's `.use()` hooks connect those typed contracts to rendering, so the component does not need its own HTTP client or duplicate request types.

First [generate the proxies](./proxy-generation.mdx) and [configure Arc](./arc.md) above the components that use them. For a live author list, the [observable query example](../../understanding-the-proxy-boundary.mdx#the-transport-disappears-too) uses this hook inside a component:

```tsx
const [authors] = AllAuthors.use();
```

`AllAuthors` is the generated query proxy; `authors` is its result state, with the current models in `.data`. The hook subscribes to the configured backend source and re-renders when it emits. Use an ordinary query instead when the screen only needs a one-off read.

Query results expose `isPerforming` for loading state. For commands, manage pending state in the component or use a form component; the command hook does not disable a submit button while a request is pending. Start with [commands](./commands/index.mdx) to submit a change or [queries](./queries/index.md) to display data.

## Working with commands

| Topic | What it covers |
| ------- | ----------- |
| [Commands](./commands/index.mdx) | The `.use()` hook — instantiate, bind, validate, and execute a command from a component. |
| [Command Validation](./commands/validation.md) | Pre-flight feedback from client checks and the backend validation endpoint, without executing the handler. |
| [Command Scopes](./commands/scope.md) | A scope that captures changes, validation, and errors across several commands in one UI. |

## Working with queries

| Topic | What it covers |
| ------- | ----------- |
| [Queries](./queries/index.md) | Reading data with `.use()` — including live, observable queries that re-render on change. |

## Around the edges

| Topic | What it covers |
| ------- | ----------- |
| [Configure Arc](./arc.md) | Point the React app at your backend and set transport, headers, and identity. |
| [Identity](./identity.md) | Who the user is, and what they're allowed to see and do. |
| [Dialogs](./dialogs.md) | Consistent dialog handling for command and data-entry flows. |
| [Proxy generation (C#)](../../backend/csharp/proxy-generation/index.md) | Generate the typed clients used in these C# examples. For other backends, see [JVM proxies](/arc/backend/kotlin/guides/typescript-proxies/) or the [TypeScript backend's generator](/arc/backend/typescript/proxy-generation/). |
| [Storybook](/arc/frontend/react/storybook/) | The Storybook for the components Arc exposes. |
| [Story Components](./stories) | Building good-looking, consistent stories. |

When a screen's behavior grows beyond its component, [Model–View–ViewModel (MVVM) with React](../react/mvvm/index.md) shows how to move that behavior into a testable view model.
