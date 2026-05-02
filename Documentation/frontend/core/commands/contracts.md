# Command Contracts

Core command support in `@cratis/arc` is built around typed command classes generated from backend command definitions.

## ICommand

The core command interface provides execution and state-management capabilities:

```typescript
interface ICommand<TCommandContent = object, TCommandResponse = object> {
    readonly route: string;
    execute(): Promise<CommandResult<TCommandResponse>>;
    clear(): void;
    setInitialValues(values: TCommandContent): void;
    setInitialValuesFromCurrentValues(): void;
    revertChanges(): void;
    hasChanges: boolean;
    onPropertyChanged(callback: PropertyChanged, thisArg?: any): void;
}
```

## Change Tracking

Commands track property changes automatically.

- `hasChanges` indicates whether current values differ from the baseline.
- `setInitialValues()` sets an explicit baseline.
- `setInitialValuesFromCurrentValues()` snapshots current values as baseline.
- `revertChanges()` restores baseline values.

## Property Change Notifications

Commands expose property-change callbacks for reactive flows:

```typescript
command.onPropertyChanged((property: string) => {
    console.log(`Property ${property} changed`);
});
```

## See Also

- [Validation And Results](./validation-and-results.md)
- [Configuration](./configuration.md)
- [React Commands](../../react/commands/index.md)
