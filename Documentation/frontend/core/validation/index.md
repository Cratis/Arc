---
title: Cratis Arc frontend validation
description: Cratis Arc checks generated validation rules before command and query requests, giving early feedback while the server enforces validation and access.
---

Cratis Arc frontend validation catches invalid input before a command or query request reaches the server. Generated TypeScript validators give your UI early feedback from supported backend rules; the server remains responsible for enforcing validation and authorization.

Start with rules on a discoverable backend command or query, then generate its proxy. When supported rules are extracted, the proxy has an attached validator that runs automatically. You do not need to wire it into each request by hand. Generated rules cover only part of backend validation, so a passing client check does not establish that the server will accept the request.

## Capabilities

| Capability | What It Covers | Learn More |
| ---------- | -------------- | ---------- |
| Fluent rules API | Define property rules and choose messages for invalid input | [Rules And Fluent API](./rules.md) |
| Command/query integration | Understand when local validation prevents a request | [Command And Query Integration](./integration.md) |
| Validation payloads | Read the members, messages, severities, and reasons in a result | [Validation Results](./results.md) |
| Severity-based execution gating | Decide when warnings should stop a write for confirmation | [Severity Filtering](./severity-filtering.md) |

For your first generated validator, follow [proxy generation validation](../../../backend/csharp/proxy-generation/validation.md). It explains which backend rules can be extracted and how to inspect the generated output. Validator constructors run during extraction, so keep I/O and service calls out of rule construction.

## Related Topics

- [Core Commands](../commands/index.md)
- [Core Queries](../queries/index.md)
- [Backend Command Validation](../../../backend/csharp/commands/validation.md)
- [Backend Query Validation](../../../backend/csharp/queries/validation.md)
- [Proxy Generation Validation](../../../backend/csharp/proxy-generation/validation.md)
