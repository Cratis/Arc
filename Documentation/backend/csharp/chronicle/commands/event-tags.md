---
title: Event tags
description: Attach structured named tags to every event a command returns, from properties, handler results, or application-wide conventions.
---

When events from several streams belong to the same project or import, their event source ids alone do not capture that relationship. Add named tags to the command to carry that grouping on every event it returns, without changing the events' payloads or routing.

These examples require [Arc's Chronicle integration](../index.md). The tag APIs below live in `Cratis.Arc.Chronicle.Commands`; `NamedTag` lives in `Cratis.Chronicle`.

## Tag from command properties

Use `[EventTag]` for a property value or a constant:

```csharp
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;

[Command]
[EventTag("projectId", nameof(ProjectId))]
[EventTag("origin", Value = "import")]
public record ImportTask(EventSourceId TaskId, string ProjectId, string Title)
{
    public TaskImported Handle() => new(Title);
}

[EventType]
public record TaskImported(string Title);
```

For `ProjectId = "project-42"`, `TaskImported` carries the named tags `projectId=project-42` and `origin=import`. You can declare multiple attributes. A concept property contributes its underlying value; other values use `ToString()`, with invariant culture for `IFormattable` values.

Each attribute must specify exactly one source: a property name or `Value`. Both or neither cause `AmbiguousEventTagValue`. A property must be a readable public instance property without index parameters; an invalid name or unreadable property causes `UnknownEventTagProperty`. A null property value, or a value that renders as null, causes `EventTagValueMissing` rather than silently omitting a tag. Empty strings are valid tag values.

## Compute tags on the command

Implement `ICanProvideEventTags` when tags need logic. This alternative uses the `TaskImported` event from the previous example:

```csharp
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;

[Command]
public record ImportTaskWithCategory(EventSourceId TaskId, string Title, bool Urgent)
    : ICanProvideEventTags
{
    public IEnumerable<NamedTag> GetEventTags() =>
        [new("priority", Urgent ? "urgent" : "normal")];

    public TaskImported Handle() => new(Title);
}
```

Arc combines these tags with attribute tags, so you can use both approaches on the same command.

## Return tags with events

If the value is known only after the handler's decision, return `EventTags` alongside the event. This alternative also uses `TaskImported`:

```csharp
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;

[Command]
public record ImportTaskWithBatch(EventSourceId TaskId, string Title)
{
    public (TaskImported, EventTags) Handle()
    {
        var batchId = Guid.NewGuid().ToString();
        return (new(Title), new EventTags(new NamedTag("batchId", batchId)));
    }
}
```

Arc merges returned tags before appending any events in the tuple, regardless of the tuple order. `EventTags` is metadata, not an event collection or a client response. Returning it alone appends nothing.

## Apply an application-wide convention

Implement `ICanProvideCommandEventTags` in an application service to add shared grouping tags. Arc discovers every implementation by convention; no manual registration is needed. For example, this provider tags commands by their type name:

```csharp
using Cratis.Arc.Chronicle.Commands;
using Cratis.Chronicle;

public class CommandKindTags : ICanProvideCommandEventTags
{
    public IEnumerable<NamedTag> GetEventTags(object command) =>
        [new("commandKind", command.GetType().Name)];
}
```

Arc resolves providers per command from the command's service scope, so providers may take scoped dependencies such as tenant services. Providers run only after the command passes authorization and validation and returns events to append; validation-only calls and commands without returned events do not evaluate tags. Each provider receives the command instance and can return an empty collection for commands it does not apply to. Keep providers transient or scoped when they depend on request, user, or tenant services; do not capture those values in a singleton.

Arc evaluates attributes, the command interface and application providers once, at the first returned-event append. Tags returned from `Handle()` join that union. `commandContext.GetEventTags()` reads only returned tags without evaluating any providers; `commandContext.ResolveEventTags()` resolves the union at append time. For a manually constructed context without a service provider or Arc's type discovery, resolution includes attribute, command-interface and returned tags, but no application-wide providers.

## Combine command and per-event tags

All command sources form a union, distinct by the pair `(Name.Value, Value)`. Equal pairs occur once; the same name with different values keeps both values. A returned `EventForEventSourceId` preserves its own `NamedTags` and adds the command tags, with wrapper tags first. For an individual event, set its tags on the wrapper (handler excerpt using `TaskImported`):

```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.EventSequences;

public EventForEventSourceId Handle() =>
    new(TaskId, new TaskImported(Title))
    {
        NamedTags = [new NamedTag("sourceFile", "tasks.csv")]
    };
```

The same merge applies to single events, plain batches, mixed batches, and `EventsWithConcurrencyScopes`, both in a command transaction and in an immediate append. Legacy string `Tags`, routing, and per-event occurrence times are unchanged. When there are no named tags, Arc keeps its existing append calls. An alternate Chronicle event sequence or unit-of-work implementation must support named tags to accept tagged events; unsupported implementations fail rather than discard the tags.

Command tags apply to returned events, not to aggregate commits or events you append manually. Continue with [Events](./events.md) for the supported handler return shapes and [Transactional commands](./transactional-commands.md) for the commit boundary.
