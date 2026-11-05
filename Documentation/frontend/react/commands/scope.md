---
title: React command scopes for shared save actions
description: Coordinate a shared Save action for Cratis Arc commands in React, report execution failures, and keep nested scope boundaries explicit.
---

Use a Cratis Arc command scope to give several React editors a shared Save action. The scope executes its own registered commands that have changes. Each command still has its own result: saving a scope is not a transaction, and it does not recursively save nested scopes.

## Using command scope in React

Commands created by generated `.use()` hooks register with the nearest scope. `<Arc>` supplies a default scope, but a local `CommandScope` lets you keep a section's Save action separate from the rest of the page.

Place the toolbar and editors inside that provider. The example passes `setHasChanges` and `setIsPerforming` callbacks into the scope so changes in its state update the toolbar through React state. Supply your editor components as children:

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

`Toolbar` calls `useCommandScope()` beneath the provider, so its Save action targets this scope rather than the parent. The provider takes elements, not a function child.

Add `onSuccess` and `onFailed` callbacks when the toolbar needs to show save feedback; the example only displays changed and busy state. A failed command keeps its baseline and remains changed if its draft still differs. Inspect results rather than treating a disabled Save button as proof that every request succeeded.

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

A parent can read stored failures from its children, but that does not make the parent a page-wide Save boundary. If a toolbar must save several nested scopes, coordinate those calls explicitly. Use [QueryScope](../queries/scope.md) for query activity; adding a query to a command scope does not track its loading state.

Getter aggregation does not itself trigger React or MobX updates. For a visible reset, notify each editor so it calls its tuple's `setValues(baseline)` with an explicit baseline, as shown in [resetting to initial values](./data-binding.md#resetting-to-initial-values). Calling `scope.revertChanges()` alone can leave displayed inputs stale even though command values reverted.

## Execution callbacks

Callbacks run inside `scope.execute()` for each changed command it executes. They do not intercept direct `command.execute()` calls or form submissions, and parent callbacks do not intercept child-scope execution. Use them for feedback on the shared Save action, not as a global command interceptor.

| Callback | When called during scope execution |
| --- | --- |
| `onBeforeExecute(command)` | Before a command executes |
| `onSuccess(command, result)` | Successful result |
| `onFailed(command, result)` | Any unsuccessful result |
| `onException(command, result)` | Result has exceptions |
| `onUnauthorized(command, result)` | Result is unauthorized |
| `onValidationFailure(command, result)` | Result is invalid |

An unsuccessful result calls `onFailed` and any applicable failure-specific callbacks. It does not stop the scope from attempting the remaining changed commands. Because successful commands are not rolled back, the toolbar must be able to report a partially successful save.

Stored failures for a command are cleared immediately before the scope executes it again, not whenever anyone calls that command directly. Commands skipped because they have no changes retain their previous scope entries.

## Validation failures and exceptions

Use `validationFailures` to look up a command's `ValidationResult[]` and `exceptions` for its exception messages (`string[]`). For a summary that includes nested scopes, read `aggregatedValidationFailures` and `aggregatedExceptions`. These maps store outcomes from scope execution; they are not live validation of every draft. Update the UI through execution and state callbacks rather than assuming mutations to the maps trigger a render.

## Using command scope in view models

In an MVVM view model, inject the nearest `ICommandScope` to read its state and call `execute()`. Making the view model observable does not make the injected scope's internals observable. Copy the state you display through an explicit callback or notification path so the view updates when execution finishes.

See [MVVM context](../../react/mvvm/mvvm-context.md) and [command results](../../core/commands/command-result.md).
