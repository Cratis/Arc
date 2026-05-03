# Validation

Core validation in Arc provides shared TypeScript/JavaScript primitives and generated rules that run before command and query requests.

This page is an overview of validation capabilities. Use the pages below for specific implementation details.

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
- [Backend Command Validation](../../../backend/commands/validation.md)
- [Backend Query Validation](../../../backend/queries/validation.md)
- [Proxy Generation Validation](../../../backend/proxy-generation/validation.md)
