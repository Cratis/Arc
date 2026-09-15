---
title: Command scope
description: Coordinate commands registered in one scope and understand the limits of nested state and execution callbacks.
---

Use a command scope when a toolbar should execute the changed commands registered in one section. It is a coordination mechanism, not a transaction or a recursive “save the entire page” operation.

## Using command scope in React

Commands created by generated `.use()` hooks register with the nearest scope. `<Arc>` supplies a default scope; add a local one when you want a separate toolbar boundary. Pass state callbacks to make toolbar updates explicit.

This composition fragment accepts your editor components as children:

```tsx
import { useState, type ReactElement } from 'react';
import { CommandScope, useCommandScope } from '@cratis/arc.react/commands';

function Toolbar({ changed, busy }: { changed: boolean; busy: boolean }) {
    const scope = useCommandScope();
    return (
        <nav>
            <button disabled={!changed || busy} onClick={() => void scope.execute()}>Save changes</button>
        </nav>
    );
}

export function EditorScope({ children }: { children: ReactElement }) {
    const [changed, setChanged] = useState(false);
    const [busy, setBusy] = useState(false);
    return (
        <CommandScope setHasChanges={setChanged} setIsPerforming={setBusy}>
            <Toolbar changed={changed} busy={busy} />
            {children}
        </CommandScope>
    );
}
```

The toolbar must be a descendant of the scope it consumes. The provider renders elements, not a function child. For rejection recovery, inspect results and offer explicit command-level retry: [baseline limitations](./data-binding.md#execution-baseline-limitations) mean a rejected request can leave no tracked changes.

## Hierarchical scopes

Nested scopes register with their nearest parent, but aggregation differs by member:

| Member | Current behavior |
| --- | --- |
| `hasChanges` | Own registered commands only |
| `isPerforming` | This scope's `execute()` operation only |
| `execute()` | Sequentially executes own changed commands, not child scopes |
| `revertChanges()` | Restores own commands' values, not child scopes; does not guarantee descendant inputs rerender |
| `validationFailures`, `exceptions` | Maps for own commands executed through the scope |
| `hasValidationFailures`, `hasExceptions` | Getters include child scopes |
| `aggregatedValidationFailures`, `aggregatedExceptions` | Flatten own and child stored failures |
| `parent`, `addChildScope(scope)` | Hierarchy registration, not a promise of all-state notification propagation |
| `addCommand(command)` | Registers property-change tracking |
| `addQuery(query)` | Stores a reference; does not track query loading |

Use [QueryScope](../queries/scope.md) for query activity. Getter aggregation does not itself trigger React or MobX updates. Nested change/performing aggregation, notification propagation, and recursive execution are runtime follow-up candidates, not supported guarantees.

For a visible reset, have each editor call its hook tuple's `setValues(baseline)` with an explicit baseline, as shown in [resetting to initial values](./data-binding.md#resetting-to-initial-values). A scope-wide reset needs an explicit notification to every editor so each invokes its setter. Calling `scope.revertChanges()` alone can leave displayed inputs stale even though command values reverted.

## Execution callbacks

Callbacks run **inside `scope.execute()`** for each of its changed commands. They do not intercept direct `command.execute()` calls or form submissions, and parent callbacks do not intercept child-scope execution.

| Callback | When called during scope execution |
| --- | --- |
| `onBeforeExecute(command)` | Before a command executes |
| `onSuccess(command, result)` | Successful result |
| `onFailed(command, result)` | Any unsuccessful result |
| `onException(command, result)` | Result has exceptions |
| `onUnauthorized(command, result)` | Result is unauthorized |
| `onValidationFailure(command, result)` | Result is invalid |

Failure-specific callbacks can accompany `onFailed`. Stored failures for a command are cleared immediately before the scope executes it again, not whenever anyone calls that command. Commands skipped because they have no changes retain their previous scope entries.

## Validation failures and exceptions

`validationFailures` maps commands to `ValidationResult[]`; `exceptions` maps commands to `string[]`. Read `aggregatedValidationFailures` and `aggregatedExceptions` for flattened hierarchy views. Update UI through explicit execution/state callbacks rather than assuming these mutable maps are observable.

## Using command scope in view models

MVVM can inject the nearest `ICommandScope`. Reading its getters and calling `execute()` are supported, but making a view model observable does not make the injected scope's internals observable. Bridge the state you display through an explicit notification/state path.

See [MVVM context](../../react.mvvm/mvvm-context.md) and [command results](../../core/commands/command-result.md).
