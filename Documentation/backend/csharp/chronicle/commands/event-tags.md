---
title: Cratis Arc command event tags
description: Cratis Arc adds named tags to returned events without changing payloads or routing. Supply tags from command properties, results or shared providers.
---

Cratis Arc command event tags attach named values to returned events without changing their payloads or routing. Use them when events from several streams share a project, import batch or other grouping that their event source ids do not express.

Choose the tag source by when the value is known: a command property or constant, command logic, a handler result, or an application-wide provider. These examples require [Arc's Chronicle integration](../index.md). The command tag APIs live in `Cratis.Arc.Chronicle.Commands`; `NamedTag` lives in `Cratis.Chronicle`.

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

Give each attribute either a property name or `Value`, not both. Arc rejects invalid declarations rather than dropping tags:

| Declaration or value | Result |
| --- | --- |
| Both sources, or neither | `AmbiguousEventTagValue` |
| A property that is not public and readable, has index parameters, or does not exist | `UnknownEventTagProperty` |
| A null property value, or a value that renders as null | `EventTagValueMissing` |
| An empty string | Accepted as a tag value |

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

Each provider receives the command instance and can return an empty collection when it does not apply. Arc resolves providers from the command's service scope, so they can depend on scoped tenant or user services. Keep such providers transient or scoped; a singleton must not capture request-specific values.

Providers run at the first returned-event append, after authorization and validation, and are evaluated once per command. Validation-only calls and commands with no returned events do not evaluate them. Attribute and command-interface tags are resolved at the same point, then combined with tags returned from `Handle()`.

For pipeline code, `commandContext.GetEventTags()` reads only returned tags without invoking providers. `commandContext.ResolveEventTags()` resolves the combined set at append time. A manually constructed context without a service provider or Arc's type discovery includes attribute, command-interface and returned tags, but no application-wide providers.

## Combine command and per-event tags

Arc combines tags by the pair `(Name.Value, Value)`. Two sources supplying `projectId=project-42` produce one tag; the same name with a different value keeps both. A returned `EventForEventSourceId` keeps its own `NamedTags` first and adds the command tags.

Set tags on that wrapper when they belong to only one event. This handler excerpt uses `TaskImported` from above:

```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.EventSequences;

public EventForEventSourceId Handle() =>
    new(TaskId, new TaskImported(Title))
    {
        NamedTags = [new NamedTag("sourceFile", "tasks.csv")]
    };
```

The same merge applies to single events, plain or mixed batches, and `EventsWithConcurrencyScopes`, whether Arc enrolls them in a command transaction or appends them immediately. Legacy string `Tags`, routing and per-event occurrence times are unchanged. With no named tags, Arc uses its existing append calls.

If you supply an alternate Chronicle event sequence or unit-of-work implementation, it must support named tags. An unsupported implementation fails the tagged append rather than discarding the tags.

Command tags apply to returned events, not to aggregate commits or events you append manually. Continue with [Events](./events.md) for the supported handler return shapes and [Transactional commands](./transactional-commands.md) for the commit boundary.
