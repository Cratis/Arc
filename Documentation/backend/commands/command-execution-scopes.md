# Command Execution Scopes

Command execution scopes let you bracket the execution of a command in the **model-bound pipeline** with a lifetime concern that must see the command's final outcome — begin something before the command runs, and complete it afterwards knowing whether the command succeeded. This is the extension point behind [transactional commands](./transactional-commands.md), and you can use it for your own concerns such as database transactions, metrics that need the final result, or outbox-style coordination.

> **Note**: A [command filter](./command-filters.md) runs *before* the handler and can stop the command. An execution scope runs *around* the whole execution — it always completes, with the final `CommandResult`, whether the command succeeded, failed validation, or threw.

## How It Works

Implementations of `ICommandExecutionScope` are discovered automatically — no registration needed. For every command the pipeline:

1. Calls `Begin(context)` on every scope after the `CommandContext` is established, before filters and the handler run.
2. Executes the command — filters, handler, and response value handlers.
3. Calls `Complete(context, result)` on every scope with the final, mutable `CommandResult` — **exactly once**, on every outcome, including validation failures and exceptions. An exception thrown from `Complete` is folded into the command's result as an exception outcome rather than propagating to the caller.

`Begin` is deliberately synchronous so ambient state a scope establishes — such as an `AsyncLocal`-based unit of work — flows into the command's execution. `Complete` is asynchronous and may mutate the `CommandResult` to reflect the outcome of completing the scope.

Scopes nest: they complete in the reverse of the order they began, like `using` blocks. The relative order between different scope implementations is unspecified — design scopes to be independent of each other. `Complete` can also be invoked when your `Begin` never ran (for example when another scope's `Begin` threw), so implementations must tolerate completing without having begun.

## Implementing a Custom Execution Scope

```csharp
using Cratis.Arc.Commands;

public class CommandTimingScope : ICommandExecutionScope
{
    static readonly AsyncLocal<long> _started = new();

    public void Begin(CommandContext context) =>
        _started.Value = TimeProvider.System.GetTimestamp();

    public Task Complete(CommandContext context, CommandResult result)
    {
        var elapsed = TimeProvider.System.GetElapsedTime(_started.Value);
        Metrics.RecordCommandDuration(context.Type.Name, elapsed, result.IsSuccess);
        return Task.CompletedTask;
    }
}
```

Because a scope instance is shared across concurrent commands, keep per-command state in an `AsyncLocal` (as above) or in the `CommandContext`, never in instance fields.

## Mutating the Result — Handle With Care

`Complete` receives the final, **mutable** `CommandResult`, and mutating it is powerful enough to lie with. Scopes should *enrich* the result — add validation results, exception outcomes, context — and **never erase failures**: clearing `ValidationResults` or `ExceptionMessages` can flip a failed command into a reported success *after* other scopes already acted on the failure. The transactional scope, for example, rolls the command's events back when the result is unsuccessful — a scope that then scrubs the failure makes the caller believe events were committed that never were. The same caution applies to mutating `CommandContext` values: downstream consumers act on them, so change them only when you own the consequence.

The `CommandContext` gives you:

- `CorrelationId` — the unique identifier for the command execution
- `Type` and `Command` — the command type and instance
- `ServiceProvider` — the command's own scope, for resolving collaborators
- `Values` — ambient values carried through the pipeline

## Built-in Scopes

| Scope | Package | Purpose |
| --- | --- | --- |
| `TransactionalCommandScope` | `Cratis.Arc.Chronicle` | Makes every command a [transactional scope](./transactional-commands.md): begins a Chronicle unit of work, commits the command's enrolled events atomically when the command succeeds — surfacing constraint violations on the `CommandResult` — and rolls them back when the command fails. Also observes immediate appends so a failed one fails the command instead of being silently swallowed. |

## Scope of the Extension Point

Execution scopes run wherever the model-bound command pipeline runs: commands executed over HTTP, directly through [`ICommandPipeline`](./command-pipeline.md), from reactors, and in the `CommandScenario` test harness. Controller-based commands do not go through the pipeline and are not covered.
