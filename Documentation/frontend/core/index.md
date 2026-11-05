---
title: Cratis Arc core TypeScript runtime
description: Use Cratis Arc's core TypeScript runtime to execute commands and queries outside React, read identity, and coordinate UI messages with explicit cleanup.
---

Cratis Arc's core frontend runtime lets TypeScript code execute commands, retrieve query results, read the current identity, and exchange in-memory messages without React hooks. Use it directly for a browser service layer or a non-React UI where your code needs to control execution and cleanup.

For React components, start with the [React integration](../react/index.md). Its hooks build on the same core APIs and connect their lifetimes to components. The core pages are useful when you need to understand the behavior behind a hook or take ownership of that lifecycle yourself.

| Task | Start here |
| ------- | ----------- |
| Send a state-changing request and inspect its result | [Commands](./commands/index.md) |
| Fetch data or receive updates from an observable query | [Queries](./queries/index.md) |
| Check input and interpret validation results | [Validation](./validation/index.md) |
| Display the current user and refresh identity after a change | [Identity](./identity.md) |
| Notify another part of the UI without a direct reference to it | [Messaging](./messaging.md) |

Commands and queries communicate with backend endpoints. Messaging stays in memory within the frontend; it does not send a command or record a domain event. Identity also comes from the backend, but the browser keeps the resolved response until you refresh or clear it. Follow the identity and messaging pages when you need to decide when to reload user context or release subscriptions.
