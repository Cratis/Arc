---
title: Cratis Arc framework-independent commands
description: Cratis Arc core commands let TypeScript clients submit changes, validate input, and handle results through generated proxies without React hooks.
---

Cratis Arc core commands let TypeScript and JavaScript clients submit state-changing operations and inspect their results without React hooks. A generated command proxy is a client class for a backend endpoint: populate its properties, call `execute()`, and use the returned `CommandResult` to decide what to show next.

Use this section when you manage command instances and feedback yourself. The command contracts, validation, and HTTP runtime live in `@cratis/arc`; [React Commands](../../react/commands/index.mdx) adds hooks for React components.

## Capabilities

| When you need to | What to look for | Read |
| ---------------- | ---------------- | ---- |
| Work with a command instance | `ICommand`, route metadata, execution, and change tracking | [Command Contracts](./contracts.md) |
| Send requests to the right backend | Microservice routing, API base path, and global Arc configuration | [Configuration](./configuration.md) |
| Show feedback after submission | Client-side validation, `CommandResult`, and failure handling | [Validation and results](./validation-and-results.md) |
| Generate clients from backend endpoints | Controller-based and model-bound command mapping | [Backend Integration](./integration.md) |

## Related Documentation

For the returned fields and callbacks, use [Command Result](./command-result.md). To check input before submission, follow [Command validation](./validation.md); the [validation overview](../validation/index.md) explains the rules and feedback model. If you still need an endpoint to call, start with [Backend Commands](../../../backend/csharp/commands/index.md).
