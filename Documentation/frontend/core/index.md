# Core

The core is Arc's frontend layer **beneath React** — the framework-agnostic TypeScript that the React
hooks build on. Most apps consume it indirectly through [React](../react/index.md); reach for it
directly when you're working outside React (a service layer, a worker, a vanilla-TS integration) or you
need lower-level control over how commands and queries execute.

| Topic | What it covers |
| ------- | ----------- |
| [Commands](./commands/index.md) | Instantiating and executing commands without React — the runtime behind the hooks. |
| [Queries](./queries/index.md) | Retrieving data and subscribing to observable queries at the core level. |
| [Validation](./validation/index.md) | The validation rules and results that flow through commands and queries. |
| [Identity](./identity.md) | The user-context primitives the higher layers expose. |
| [Messaging](./messaging.md) | The messaging and communication patterns Arc uses under the hood. |

Building UI? You almost certainly want [React](../react/index.md) instead — it wraps everything here in
hooks.
