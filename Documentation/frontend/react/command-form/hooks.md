# Working with Hooks

CommandForm provides hooks for advanced scenarios.

## useCommandFormContext

Access the form context to inspect state and control behavior:

```tsx
import { useCommandFormContext } from '@cratis/applications-react/commands';
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

## useCommandInstance

Create and manage a command instance outside of CommandForm:

```tsx
import { useCommandInstance } from '@cratis/applications-react/commands';

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
