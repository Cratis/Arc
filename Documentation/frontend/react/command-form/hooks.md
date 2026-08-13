# Working with Hooks

CommandForm provides hooks for advanced scenarios.

## useCommandFormContext

Access the form context to inspect state and control behavior:

```tsx
import { useCommandFormContext } from '@cratis/arc.react/commands';
import { Command } from '@cratis/arc/commands';

function CustomSubmitButton() {
    const { commandInstance, onExecute, commandResult } = useCommandFormContext();
    
    const handleClick = async () => {
        const command = commandInstance as unknown as Command;
        
        if (!commandResult?.isValid && commandResult?.validationResults?.length > 0) {
            alert('Please fix validation errors');
            return;
        }
        
        await onExecute?.();
    };
    
    return (
        <button 
            type="button"
            onClick={handleClick}
            disabled={!command.hasChanges}
        >
            {command.hasChanges ? 'Save Changes' : 'No Changes'}
        </button>
    );
}

// Use within CommandForm
<CommandForm command={UpdateProfile}>
    <InputTextField<UpdateProfile> value={c => c.name} title="Name" />
    <CustomSubmitButton />
</CommandForm>
```

## useIsCommandExecuting

Reports whether the surrounding form currently has an execution in flight. Use it for anything inside the
form that has to reflect execution — a submit button that disables itself, a spinner — without threading
state down by hand:

```tsx
import { useIsCommandExecuting } from '@cratis/arc.react/commands';

function SubmitButton() {
    const isExecuting = useIsCommandExecuting();

    return (
        <button type="submit" disabled={isExecuting}>
            {isExecuting ? 'Saving…' : 'Save'}
        </button>
    );
}

// Use within CommandForm
<CommandForm command={UpdateProfile}>
    <InputTextField<UpdateProfile> value={c => c.name} title="Name" />
    <SubmitButton />
</CommandForm>
```

The same value is on the form context as `isExecuting`, so `useCommandFormContext().isExecuting` is
equivalent when you need other members of the context anyway.

Executions are counted rather than flagged. A form can be submitted again while the previous submission
is still running — a double click, or a submit button and a keyboard submit racing — and a flag would be
cleared by whichever one finished first, re-enabling the button with a command still in flight. The count
goes `0 → 1 → 2 → 1 → 0` instead, so `isExecuting` stays true until the last execution settles. A command
that rejects gives its count back too, so a failed request never leaves the form stuck.

Like every CommandForm hook, this one must be called from inside a `CommandForm`; it throws otherwise.
To observe execution state from *outside* the form, see [Form Lifecycle](./form-lifecycle.md).

## useCommandInstance

Create and manage a command instance outside of CommandForm:

```tsx
import { useCommandInstance } from '@cratis/arc.react/commands';

function MyComponent() {
    const command = useCommandInstance(CreateOrder, {
        customerId: 'customer-123',
        orderDate: new Date()
    });
    
    const handleValidate = async () => {
        const result = await command.validate();
        console.log('Valid:', result.isValid);
    };
    
    useEffect(() => {
        // Load async data
        const loadData = async () => {
            const products = await fetchProducts();
            command.products = products;
        };
        loadData();
    }, []);
    
    return (
        <div>
            <button onClick={handleValidate}>Check Validity</button>
            <CommandForm command={CreateOrder} initialValues={command}>
                <SelectField<CreateOrder>
                    value={c => c.productId}
                    title="Product"
                    options={command.products || []}
                    optionIdField="id"
                    optionLabelField="name"
                />
            </CommandForm>
        </div>
    );
}
```

## See Also

- [CommandForm Overview](./index.md)
- [Data Loading](./data-loading.md)
- [Form Lifecycle](./form-lifecycle.md)
