---
title: 'ARCCHR0017: [EventStreamId] template is invalid'
description: An [EventStreamId] value with {Property} placeholders names a property the command does not expose, a type that does not convert to a string, or has unbalanced braces.
---

## Rule

On a command, `[EventStreamId("{ReportingScopeId}:{Period}")]` is a template. Arc resolves each `{Name}` from the command's public instance properties. `{{` and `}}` write literal braces, and a value without placeholders is a constant stream id that this rule does not inspect.

The rule reports, on the attribute value, each of:

- a placeholder that is not a public instance property of the command (unknown, private, internal, static or an indexer);
- a placeholder whose type does not convert to a string part: the supported types are `string`, primitives, `decimal`, `Guid`, enums, `DateOnly`, `DateTime`, `DateTimeOffset`, `TimeOnly`, `TimeSpan`, `ConceptAs<T>` over one of these, and `EventSourceId` derived types, each optionally nullable;
- malformed braces: an unclosed `{`, a stray `}`, or an empty `{}`.

## Severity

Error

## Example

```csharp
[Command]
[EventStreamId("{Scope}:{Period}")] // ARCCHR0017: 'Period' is not a public instance property
public record CloseBooks(string Scope)
{
    ...
}
```

Add a public `Period` property of a supported type, or write `{{Period}}` if you meant literal braces.

## Not resolved outside commands

Only Arc commands resolve templates. A reactor reads the same attribute as a literal constant, so a template there is reported by [ARCCHR0019](./ARCCHR0019.md).

## See also

- [Concurrency](../commands/concurrency.md)
- [Stream decisions](../commands/stream-decisions.md)
