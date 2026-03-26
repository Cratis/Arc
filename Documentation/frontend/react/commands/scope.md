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

Command scopes can be nested to create a hierarchy. Child scopes automatically recognize their parent scope,
allowing you to track state at different levels of your component tree.

```typescript
export const MyPage = () => {
    return (
        <CommandScope>
            <PageToolbar/>
            <Section1>
                <CommandScope>
                    <SectionToolbar/>
                    <SectionContent/>
                </CommandScope>
            </Section1>
        </CommandScope>
    );
};
```

In this example, the inner `CommandScope` has access to its parent scope through the `parent` property.

## Callbacks

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
| parent | ICommandScope \| undefined | The parent scope, if this scope is nested |
| execute() | Promise\<CommandResults\> | Execute all commands with changes within the scope |
| revertChanges() | void | Revert any changes to commands within the scope |
| addCommand(command) | void | Manually add a command for tracking (usually done automatically) |
| addQuery(query) | void | Manually add a query for tracking (usually done automatically) |

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
