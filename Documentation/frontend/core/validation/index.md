---
title: Validation
description: Overview of Arc's TypeScript validation primitives and generated rules that run before command and query requests.
---

Arc can check input in the browser before sending a command or query request. If you own the client-side checks, define them with the fluent rules API. If the rules already live on the backend, proxy generation can extract supported rules into typed validators so you do not have to maintain those checks twice.

Generated validation is a projection of supported backend rules, not a guarantee that the browser can enforce every backend condition. Local checks give early feedback; the server remains responsible for authoritative validation and authorization.

Start with rule definition and its supported mappings, then follow how execution uses an attached validator. The result reference explains the feedback you receive. Severity filtering covers the later decision: whether a warning should block execution until the user confirms.

## Capabilities

| Capability | What It Covers | Learn More |
| ---------- | -------------- | ---------- |
| Fluent rules API | Programmatic rule definition, built-in rules, and custom messages | [Rules And Fluent API](./rules.md) |
| Command/query integration | Automatic pre-flight validation behavior for commands and queries | [Command And Query Integration](./integration.md) |
| Validation payloads | Result shape, severities, and diagnostics | [Validation Results](./results.md) |
| Severity-based execution gating | Warning/information filtering and confirmation flows | [Severity Filtering](./severity-filtering.md) |

## Related Topics

- [Core Commands](../commands/index.md)
- [Core Queries](../queries/index.md)
- [Backend Command Validation](../../../backend/csharp/commands/validation.md)
- [Backend Query Validation](../../../backend/csharp/queries/validation.md)
- [Proxy Generation Validation](../../../backend/csharp/proxy-generation/validation.md)
