---
title: Command Contracts
description: The ICommand contract in Cratis Arc defines execution, validation, change tracking, and property-change notifications for generated TypeScript commands.
---

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

- `hasChanges` compares current property values with the baseline using strict inequality; it is not a deep comparison of nested objects.
- `setInitialValues()` assigns the supplied properties and sets their baseline, including falsy values.
- `setInitialValuesFromCurrentValues()` snapshots current declared properties, including `''`, `0`, `false`, `null`, and `undefined` when present.
- Successful `execute()` accepts the submitted snapshot as the new baseline. Unsuccessful execution leaves the previous baseline unchanged. Edits made while the request is pending remain changed if they differ from the submitted snapshot; see [execution baseline behavior](../../react/commands/data-binding.md#execution-baseline-limitations).
- `revertChanges()` restores baseline values.
- `validate()` can return a local validation failure before reaching the server. When reached, server preflight runs filters without executing the handler; `validateClientSide()` never calls the server. Neither validation method changes the baseline.

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
- [React Commands](../../react/commands/index.mdx)
