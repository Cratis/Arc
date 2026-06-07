# React

This is the heart of Arc on the frontend: a small set of hooks and helpers that turn your
generated command and query proxies into idiomatic React. You import a proxy, call its `.use()` hook,
and get a typed, reactive instance — no API client, no DTOs, no manual loading state. Start with
[Commands](./commands/index.md) and [Queries](./queries/index.md); the rest of the pages here refine
those two with validation, scopes, and identity.

## Working with commands

| Topic | What it covers |
| ------- | ----------- |
| [Commands](./commands/index.md) | The `.use()` hook — instantiate, bind, validate, and execute a command from a component. |
| [Command Validation](./commands/validation.md) | Pre-flight validation: catch invalid input before the request leaves the browser. |
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
| [Proxy Generation](../../backend/proxy-generation/index.md) | How the typed proxies you import here are generated from C#. |
| [Storybook](./storybook.md) | The Storybook for the components Arc exposes. |
| [Story Components](./stories) | Building good-looking, consistent stories. |

Prefer a structured, testable approach for complex screens? See [MVVM with React](../react.mvvm/index.md).
