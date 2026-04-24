# Command Scope

If you want to track commands and queries and create an aggregation of their status at a compositional
level, the command scope provides a React context for this.
This is typically useful when having something like a top level toolbar with a **Save** button that
you want to enable or disable depending on whether or not there are changes within any components
used within it, or a loading indicator when commands or queries are being performed.

Using the toolbar scenario as an example; at the top level we can wrap everything in the `<CommandScope>`
component. This will establish a React context for this part of the hierarchy and track any commands
and queries used within any descendants.

```typescript
import { CommandScope } from '@cratis/arc/commands';

export const MyComposition = () => {
    const [hasChanges, setHasChanges] = useState(false);
    const [isPerforming, setIsPerforming] = useState(false);

    return (
        <CommandScope 
            setHasChanges={setHasChanges}
            setIsPerforming={setIsPerforming}>
            <Toolbar hasChanges={hasChanges} isPerforming={isPerforming}/>
            <FirstComponent/>
            <SecondComponent/>
        </CommandScope>
    );
};
```

## Hierarchical Scopes

Command scopes can be nested to create a hierarchy. When you add a `<CommandScope>` inside another
one, the inner scope automatically registers itself with the nearest outer scope. Commands always
bind to the **nearest** enclosing scope, so each part of the component tree only tracks the commands
directly beneath it. The outer scope aggregates state across all inner scopes, giving you both
local and global views of changes, performing state, validation failures, and exceptions.

```typescript
export const MyPage = () => {
    const [hasChanges, setHasChanges] = useState(false);
    const [hasValidationFailures, setHasValidationFailures] = useState(false);

    return (
        <CommandScope setHasChanges={setHasChanges}>
            {/* PageToolbar sees aggregate state for the whole page */}
            <PageToolbar hasChanges={hasChanges} hasValidationFailures={hasValidationFailures}/>
            <Section1>
                <CommandScope>
                    {/* SectionToolbar only sees state for Section1's commands */}
                    <SectionToolbar/>
                    <SectionContent/>
                </CommandScope>
            </Section1>
        </CommandScope>
    );
};
```

In this example:
- Commands inside `<Section1>` bind to the inner `<CommandScope>`.
- The outer `<CommandScope>` sees their state through the nested scope link.
- `pageScope.hasValidationFailures` is `true` whenever any inner scope has failures.

## Validation Failures and Exceptions

The `CommandScope` tracks which commands produced validation failures or exceptions after execution.
These are cleared automatically when a command executes again — you never need to reset them manually.

### Checking aggregate state

```typescript
import { useCommandScope } from '@cratis/arc/commands';

export const Toolbar = () => {
    const scope = useCommandScope();

    return (
        <div>
            {scope.hasValidationFailures && (
                <span className="error">Some inputs have validation errors.</span>
            )}
            {scope.hasExceptions && (
                <span className="error">An unexpected error occurred.</span>
            )}
        </div>
    );
};
```

### Inspecting per-command detail

When you need to know exactly which command had a problem and what the errors were, use the
`validationFailures` and `exceptions` maps. Both are `ReadonlyMap<ICommand, ...>`.

```typescript
const scope = useCommandScope();

// Validation failures — map of command → ValidationResult[]
for (const [command, failures] of scope.validationFailures) {
    failures.forEach(f => console.log(f.message, f.members));
}

// Exceptions — map of command → string[]
for (const [command, messages] of scope.exceptions) {
    messages.forEach(m => console.log(m));
}
```

Only the **own commands** of a scope appear in its `validationFailures` and `exceptions` maps.
Commands from nested child scopes appear in those child scopes' maps, but bubble up to the parent
via `hasValidationFailures` and `hasExceptions`.

### Automatic clearing on re-execution

When a command executes again, its previous failures and exceptions are cleared **before** the new
execution begins. This means a scope's `hasValidationFailures` and `hasExceptions` always reflect
the result of the most recent execution, not a stale prior result.

```typescript
// First execute — validation fails
await scope.execute();
console.log(scope.hasValidationFailures); // true

// User fixes the input; execute again — succeeds
await scope.execute();
console.log(scope.hasValidationFailures); // false — automatically cleared
```



You can provide callbacks to the `<CommandScope>` component to react to command execution results.
These callbacks are invoked for any command tracked within the scope, and each receives the specific
command that was executed together with its result.

### Before Execution

The `onBeforeExecute` callback is called just before each command is executed:

```typescript
<CommandScope onBeforeExecute={(command) => console.log('About to execute', command)}>
    {/* ... */}
</CommandScope>
```

### Result Callbacks

The result callbacks mirror the callbacks available on `CommandResult` but also include the command
instance so you can identify which command produced the result:

```typescript
<CommandScope
    onSuccess={(command, result) => {
        console.log('Command succeeded:', command, result);
    }}
    onFailed={(command, result) => {
        console.log('Command failed:', command, result);
    }}
    onException={(command, result) => {
        console.log('Command threw an exception:', command, result.exceptionMessages);
    }}
    onUnauthorized={(command, result) => {
        console.log('Command was unauthorized:', command);
    }}
    onValidationFailure={(command, result) => {
        console.log('Command had validation errors:', command, result.validationResults);
    }}>
    {/* ... */}
</CommandScope>
```

| Callback | When called |
| -------- | ----------- |
| `onBeforeExecute(command)` | Before each command is executed |
| `onSuccess(command, result)` | When a command executes successfully |
| `onFailed(command, result)` | When a command fails for any reason |
| `onException(command, result)` | When a command fails due to an exception |
| `onUnauthorized(command, result)` | When a command fails due to an authorization failure |
| `onValidationFailure(command, result)` | When a command fails due to validation errors |

Note that `onFailed` is always called alongside the more specific callbacks (`onException`,
`onUnauthorized`, `onValidationFailure`) when the command fails.

## Command Scope API

The command scope provides the following properties and methods:

| Name | Type | Description |
| ---- | ---- | ----------- |
| hasChanges | Boolean | Whether or not there are changes in any commands within the scope |
| isPerforming | Boolean | Whether or not any commands or queries are currently being performed |
| hasValidationFailures | Boolean | Whether any commands in this scope or child scopes have validation failures from the last execution |
| hasExceptions | Boolean | Whether any commands in this scope or child scopes produced exceptions in the last execution |
| validationFailures | ReadonlyMap\<ICommand, ValidationResult[]\> | Validation failures per command for this scope's own commands |
| exceptions | ReadonlyMap\<ICommand, string[]\> | Exception messages per command for this scope's own commands |
| parent | ICommandScope \| undefined | The parent scope, if this scope is nested |
| execute() | Promise\<CommandResults\> | Execute all commands with changes within the scope |
| revertChanges() | void | Revert any changes to commands within the scope |
| addCommand(command) | void | Manually add a command for tracking (usually done automatically) |
| addQuery(query) | void | Manually add a query for tracking (usually done automatically) |
| addChildScope(scope) | void | Register a child scope for aggregate state propagation (done automatically) |

## Using Command Scope in React

To consume the command scope context you can use the hook that is provided.

```typescript
import { useCommandScope } from '@cratis/arc/commands';

export const Toolbar = ({ hasChanges, isPerforming }) => {
    const commandScope = useCommandScope();

    return (
        <div>
            <button 
                disabled={!hasChanges || isPerforming}
                onClick={() => commandScope.execute()}>
                Save
            </button>
            <button 
                disabled={!hasChanges || isPerforming}
                onClick={() => commandScope.revertChanges()}>
                Cancel
            </button>
            {isPerforming && <Spinner />}
        </div>
    );
};
```

The hook is a convenience hook that makes it easier to get the context.
You can also consume the context directly by using its consumer:

```typescript
import { CommandScopeContext } from '@cratis/arc/commands';

export const Toolbar = () => {
    return (
        <div>
            <CommandScopeContext.Consumer>
                {value => {
                    return (
                        <button disabled={!value.hasChanges}>Save</button>
                    )
                }}
            </CommandScopeContext.Consumer>
        </div>
    );
};
```

## Using Command Scope in ViewModels

The command scope can also be injected into ViewModels through dependency injection. The ViewModel will
automatically receive the closest command scope in the component hierarchy.

```typescript
import { ICommandScope } from '@cratis/arc.react/commands';
import { injectable } from 'tsyringe';

@injectable()
export class MyViewModel {
    constructor(private readonly _commandScope: ICommandScope) {
    }

    get hasChanges(): boolean {
        return this._commandScope.hasChanges;
    }

    get isPerforming(): boolean {
        return this._commandScope.isPerforming;
    }

    async save() {
        if (!this.hasChanges || this.isPerforming) {
            return;
        }
        
        await this._commandScope.execute();
    }

    cancel() {
        this._commandScope.revertChanges();
    }
}
```

## Automatic Command and Query Tracking

For the `<FirstComponent>` we could then have something like below:

```typescript
export const FirstComponent = () => {
    const myCommand = MyCommand.use();

    return (
        <div>
            <input type="text" value={command.someValue} onChange={(e,v) => myCommand.someValue = v; }/>
        </div>
    )
}
```

Commands created with the `use()` hook are automatically added to the nearest command scope.

Queries are also automatically tracked:

```typescript
export const SecondComponent = () => {
    const [result] = useQuery(MyQuery, { id: '123' });

    return (
        <div>
            {result.data && <DisplayData data={result.data} />}
        </div>
    )
}
```

Any changes to properties within commands will bubble up to the context and affect the state flags
(`hasChanges`, `isPerforming`).
