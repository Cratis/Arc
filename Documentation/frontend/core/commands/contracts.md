# Command Contracts

Core command support in `@cratis/arc` is built around typed command classes generated from backend command definitions.

## ICommand

The core command interface provides execution and state-management capabilities:

```typescript
interface ICommand<TCommandContent = object, TCommandResponse = object> extends ICanBeConfigured {
    readonly route: string;
    readonly roles: string[];
    readonly propertyDescriptors: PropertyDescriptor[];
    execute(
        allowedSeverity?: ValidationResultSeverity,
        ignoreWarnings?: boolean
    ): Promise<CommandResult<TCommandResponse>>;
    validate(): Promise<CommandResult<TCommandResponse>>;
    validateClientSide(): CommandResult<TCommandResponse>;
    clear(): void;
    setInitialValues(values: TCommandContent): void;
    setInitialValuesFromCurrentValues(): void;
    revertChanges(): void;
    readonly hasChanges: boolean;
    propertyChanged(property: string): void;
    onPropertyChanged(callback: PropertyChanged, thisArg: object): void;
}
```

## Change Tracking

Commands track property changes automatically.

- `hasChanges` indicates whether current values differ from the baseline.
- `setInitialValues()` sets an explicit baseline.
- `setInitialValuesFromCurrentValues()` currently snapshots truthy values only. Completed server executions call it even on unsuccessful results; see [baseline limitations](../../react/commands/data-binding.md#execution-baseline-limitations).
- `revertChanges()` restores baseline values.
- `validate()` can return a local validation failure before reaching the server. When reached, server preflight runs filters without executing the handler; `validateClientSide()` never calls the server.

## Property Change Notifications

Commands expose property-change callbacks for reactive flows:

```typescript
// Illustrative fragment: retain this owner while notifications are needed.
const owner = {
    onChanged(property: string) {
        console.log(`Property ${property} changed`);
    }
};
command.onPropertyChanged(owner.onChanged, owner);
```

The callback and its receiver are weakly referenced. Supply the required receiver and retain both it and the callback for the subscription's intended lifetime; an otherwise unreferenced inline function is not a durable listener.

## See Also

- [Validation And Results](./validation-and-results.md)
- [Configuration](./configuration.md)
- [React Commands](../../react/commands/index.md)
