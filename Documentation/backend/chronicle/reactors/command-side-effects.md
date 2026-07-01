---
title: Returning Commands as Side Effects
description: Automatically execute commands when a reactor processes an event.
---

**Goal:** when a reactor processes an event and needs to change state, it can return commands that will be automatically executed as side effects of the event. This provides a clean, transactional way to trigger follow-up actions without manually executing commands through `ICommandPipeline`.

## Overview

Instead of injecting `ICommandPipeline` and calling `commands.Execute()` manually, a reactor can return a command (or multiple commands) from its handler method. Arc automatically detects the return value, executes the command through the pipeline, and handles any failures.

### How it works

1. A reactor handler method returns an object that is a command (decorated with `[Command]` attribute)
2. Arc's `CommandResultHandler` detects the return value and executes it
3. The command runs through validation and its `Handle()` method just like any other command
4. If the command succeeds, execution continues
5. If the command fails, `CommandExecutionFailedException` is thrown to fail the reactor

## Returning a Single Command

When a reactor needs to trigger a single follow-up command, return it directly from the handler method:

```csharp
[Command]
public record CreateSearchIndex(string BookId, string Title);

public class CatalogIndexer : IReactor
{
    public object OnNext(BookAddedToCatalog @event) =>
        new CreateSearchIndex(@event.BookId, @event.Title);
}
```

The returned command is executed in its own service scope, ensuring transactional consistency. If the command validation fails or the handler throws an exception, the reactor fails with `CommandExecutionFailedException`.

## Returning Multiple Commands

When a reactor needs to execute multiple commands as a single atomic operation, return them as an array or collection:

```csharp
public class BookArchiver : IReactor
{
    public object OnNext(BookRemovedFromCatalog @event) =>
        new object[]
        {
            new ArchiveBookMetadata(@event.BookId),
            new RemoveFromSearchIndex(@event.BookId),
            new NotifySubscribers(@event.BookId)
        };
}
```

Multiple commands execute sequentially within the same service scope, ensuring transactional consistency. If any command fails, the reactor fails immediately, and the remaining commands are not executed.

## Transactional Behavior

Commands returned from a reactor execute within a dedicated `IServiceScope`. This scope isolation ensures:

- **Consistency**: All returned commands execute within the same dependency injection scope
- **Atomicity**: Commands execute sequentially in order; if any fails, the reactor fails
- **Isolation**: The scope is separate from other command executions

This makes returning commands ideal for multi-step operations that should succeed or fail as a unit.

## Error Handling

### Command Validation Failures

If a returned command fails validation or authorization, `CommandExecutionFailedException` is thrown:

```csharp
public class CommandExecutionFailedException : Exception
{
    public Type CommandType { get; }
    public CommandResult Result { get; }
}
```

The exception contains:
- **CommandType**: The type of the command that failed
- **Result**: The full `CommandResult` with validation errors, authorization failures, and exceptions

When a reactor throws `CommandExecutionFailedException`, the reactor fails and the event processing is rolled back. Use this to decide whether a failed command should cause the entire reactor to fail or should be handled gracefully.

### Example: Graceful Degradation

If you want a reactor to continue even if a command fails, catch the exception:

```csharp
public class ResilientIndexer : IReactor
{
    private readonly ILogger<ResilientIndexer> _logger;

    public ResilientIndexer(ILogger<ResilientIndexer> logger) => _logger = logger;

    public async Task OnNext(BookAddedToCatalog @event)
    {
        try
        {
            return new IndexBookForSearch(@event.BookId, @event.Title);
        }
        catch (CommandExecutionFailedException ex)
        {
            _logger.LogWarning("Failed to index book {BookId}: {Result}", 
                @event.BookId, ex.Result);
        }
    }
}
```

## Combining with Manual Execution

You can mix both approaches. Some reactions might return commands, while others use `ICommandPipeline` directly:

```csharp
public class HybridReactor(ICommandPipeline commands) : IReactor
{
    public async Task OnNext(BookAddedToCatalog @event)
    {
        // Side effect 1: Return a command (auto-executed)
        if (@event.IsPopular)
        {
            return new FlagAsPopular(@event.BookId);
        }

        // Side effect 2: Execute manually for complex logic
        if (@event.RequiresApproval)
        {
            await commands.Execute(new RequestApproval(@event.BookId));
        }
    }
}
```

When a reactor method returns a command, it cannot also execute additional commands manually in the same invocation. Choose one approach per handler method.

## See also

- [React to an event](../react-to-an-event.md) — the fundamentals of reactors and when to use them
- [Commands](../commands/toc.yml) — Arc commands and command execution
