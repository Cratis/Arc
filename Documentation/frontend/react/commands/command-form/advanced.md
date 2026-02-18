# Advanced Usage

This guide covers advanced patterns and techniques for working with CommandForm.

## Custom Layouts

CommandForm renders children in the order they are defined. You can create custom layouts using standard HTML and CSS.

### Multi-Column Layout

```tsx
<CommandForm<UpdateProfile> command={UpdateProfile}>
    <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
        <InputTextField<UpdateProfile> value={c => c.firstName} title="First Name" />
        <InputTextField<UpdateProfile> value={c => c.lastName} title="Last Name" />
    </div>
    
    <InputTextField<UpdateProfile> value={c => c.email} type="email" title="Email" />
    <TextAreaField<UpdateProfile> value={c => c.bio} title="Biography" rows={5} />
</CommandForm>
```

### Grouped Fields

```tsx
<CommandForm<RegisterUser> command={RegisterUser}>
    <fieldset style={{ border: '1px solid #e5e7eb', borderRadius: '0.5rem', padding: '1rem', marginBottom: '1rem' }}>
        <legend style={{ fontWeight: 'bold', padding: '0 0.5rem' }}>Account Information</legend>
        <InputTextField<RegisterUser> value={c => c.username} title="Username" />
        <InputTextField<RegisterUser> value={c => c.email} type="email" title="Email" />
        <InputTextField<RegisterUser> value={c => c.password} type="password" title="Password" />
    </fieldset>
    
    <fieldset style={{ border: '1px solid #e5e7eb', borderRadius: '0.5rem', padding: '1rem' }}>
        <legend style={{ fontWeight: 'bold', padding: '0 0.5rem' }}>Personal Information</legend>
        <InputTextField<RegisterUser> value={c => c.firstName} title="First Name" />
        <InputTextField<RegisterUser> value={c => c.lastName} title="Last Name" />
        <TextAreaField<RegisterUser> value={c => c.bio} title="Bio" rows={4} />
    </fieldset>
</CommandForm>
```

### Conditional Fields

Show fields based on other field values:

```tsx
function RegistrationForm() {
    const command = useCommandInstance(RegisterUser);
    
    return (
        <CommandForm<RegisterUser> command={RegisterUser}>
            <InputTextField<RegisterUser> value={c => c.email} type="email" title="Email" />
            
            <SelectField<RegisterUser>
                value={c => c.accountType}
                title="Account Type"
                options={[
                    { id: 'personal', name: 'Personal' },
                    { id: 'business', name: 'Business' }
                ]}
                optionIdField="id"
                optionLabelField="name"
            />
            
            {command.accountType === 'business' && (
                <>
                    <InputTextField<RegisterUser> value={c => c.companyName} title="Company Name" />
                    <InputTextField<RegisterUser> value={c => c.taxId} title="Tax ID" />
                </>
            )}
        </CommandForm>
    );
}
```

## Working with Hooks

CommandForm provides hooks for advanced scenarios.

### useCommandFormContext

Access the form context to inspect state and control behavior:

```tsx
import { useCommandFormContext } from '@cratis/applications-react/commands';

function CustomSubmitButton() {
    const { instance, onExecute } = useCommandFormContext();
    
    const handleClick = async () => {
        if (instance.hasErrors()) {
            alert('Please fix validation errors');
            return;
        }
        
        await onExecute?.();
    };
    
    return (
        <button 
            onClick={handleClick}
            disabled={!instance.hasChanges}
        >
            {instance.hasChanges ? 'Save Changes' : 'No Changes'}
        </button>
    );
}

// Use within CommandForm
<CommandForm<UpdateProfile> command={UpdateProfile}>
    <InputTextField<UpdateProfile> value={c => c.name} title="Name" />
    <CustomSubmitButton />
</CommandForm>
```

### useCommandInstance

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
            <CommandForm<CreateOrder> command={CreateOrder} initialValues={command}>
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

## Async Data Loading

Load data asynchronously for use in form fields.

### Loading Options for SelectField

```tsx
function OrderForm() {
    const [products, setProducts] = useState<Array<{ id: string, name: string }>>([]);
    const [loading, setLoading] = useState(true);
    
    useEffect(() => {
        const loadProducts = async () => {
            try {
                const data = await fetch('/api/products').then(r => r.json());
                setProducts(data);
            } finally {
                setLoading(false);
            }
        };
        loadProducts();
    }, []);
    
    if (loading) {
        return <div>Loading...</div>;
    }
    
    return (
        <CommandForm<CreateOrder> command={CreateOrder}>
            <SelectField<CreateOrder>
                value={c => c.productId}
                title="Product"
                options={products}
                optionIdField="id"
                optionLabelField="name"
                placeholder="Select a product..."
                required
            />
            <NumberField<CreateOrder> value={c => c.quantity} title="Quantity" min={1} required />
        </CommandForm>
    );
}
```

### Dependent Dropdowns

Load options based on another field's value:

```tsx
function LocationForm() {
    const command = useCommandInstance(SaveLocation);
    const [cities, setCities] = useState<Array<{ id: string, name: string }>>([]);
    
    useEffect(() => {
        if (command.country) {
            // Load cities for selected country
            const loadCities = async () => {
                const data = await fetch(`/api/cities?country=${command.country}`)
                    .then(r => r.json());
                setCities(data);
            };
            loadCities();
        } else {
            setCities([]);
        }
    }, [command.country]);
    
    return (
        <CommandForm<SaveLocation> command={SaveLocation}>
            <SelectField<SaveLocation>
                value={c => c.country}
                title="Country"
                options={countries}
                optionIdField="id"
                optionLabelField="name"
                required
            />
            
            <SelectField<SaveLocation>
                value={c => c.city}
                title="City"
                options={cities}
                optionIdField="id"
                optionLabelField="name"
                placeholder={command.country ? "Select a city..." : "Select country first"}
                required
            />
        </CommandForm>
    );
}
```

## Before Execute Hook

Execute code before the command is submitted:

```tsx
function OrderForm() {
    const handleBeforeExecute = async (command: CreateOrder): Promise<boolean> => {
        // Confirm before submitting
        const confirmed = window.confirm('Submit this order?');
        
        if (!confirmed) {
            return false; // Cancel submission
        }
        
        // Add calculated fields
        command.totalAmount = calculateTotal(command.items);
        command.submittedAt = new Date();
        
        return true; // Proceed with submission
    };
    
    return (
        <CommandForm<CreateOrder>
            command={CreateOrder}
            beforeExecute={handleBeforeExecute}
        >
            <InputTextField<CreateOrder> value={c => c.customerName} title="Customer" required />
            {/* More fields... */}
        </CommandForm>
    );
}
```

The `beforeExecute` callback:
- Receives the command instance
- Returns `true` to proceed, `false` to cancel
- Can modify the command before submission
- Can perform async operations

## Form State Management

Track form state for enhanced UX:

```tsx
function SmartForm() {
    const command = useCommandInstance(UpdateProfile);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [lastSaved, setLastSaved] = useState<Date | null>(null);
    
    const handleExecute = async () => {
        setIsSubmitting(true);
        try {
            const result = await command.execute();
            if (result.isSuccess) {
                setLastSaved(new Date());
            }
        } finally {
            setIsSubmitting(false);
        }
    };
    
    return (
        <div>
            <CommandForm<UpdateProfile> command={UpdateProfile}>
                <InputTextField<UpdateProfile> value={c => c.name} title="Name" />
                <InputTextField<UpdateProfile> value={c => c.email} type="email" title="Email" />
            </CommandForm>
            
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: '1rem' }}>
                <button 
                    onClick={handleExecute}
                    disabled={!command.hasChanges || isSubmitting}
                >
                    {isSubmitting ? 'Saving...' : 'Save'}
                </button>
                
                {lastSaved && (
                    <span style={{ fontSize: '0.875rem', color: '#6b7280' }}>
                        Last saved: {lastSaved.toLocaleTimeString()}
                    </span>
                )}
                
                {command.hasChanges && (
                    <span style={{ fontSize: '0.875rem', color: '#f59e0b' }}>
                        Unsaved changes
                    </span>
                )}
            </div>
        </div>
    );
}
```

## Auto-Save

Implement auto-save functionality:

```tsx
function AutoSaveForm() {
    const command = useCommandInstance(UpdateDraft);
    const timeoutRef = useRef<NodeJS.Timeout>();
    
    useEffect(() => {
        if (command.hasChanges) {
            // Clear previous timeout
            if (timeoutRef.current) {
                clearTimeout(timeoutRef.current);
            }
            
            // Set new timeout to auto-save after 2 seconds
            timeoutRef.current = setTimeout(async () => {
                await command.execute();
                console.log('Auto-saved');
            }, 2000);
        }
        
        return () => {
            if (timeoutRef.current) {
                clearTimeout(timeoutRef.current);
            }
        };
    }, [command.hasChanges]);
    
    return (
        <CommandForm<UpdateDraft> command={UpdateDraft}>
            <InputTextField<UpdateDraft> value={c => c.title} title="Title" />
            <TextAreaField<UpdateDraft> value={c => c.content} title="Content" rows={10} />
            <div style={{ fontSize: '0.875rem', color: '#6b7280', marginTop: '0.5rem' }}>
                Changes are saved automatically
            </div>
        </CommandForm>
    );
}
```

## Computed Fields

Add computed or derived fields:

```tsx
function InvoiceForm() {
    const command = useCommandInstance(CreateInvoice);
    
    const subtotal = (command.items || []).reduce((sum, item) => 
        sum + (item.quantity * item.price), 0
    );
    const tax = subtotal * 0.1; // 10% tax
    const total = subtotal + tax;
    
    return (
        <CommandForm command={CreateInvoice}>
            {/* Item fields */}
            
            <div style={{ 
                marginTop: '2rem', 
                padding: '1rem', 
                backgroundColor: '#f9fafb',
                borderRadius: '0.5rem'
            }}>
                <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                    <span>Subtotal:</span>
                    <span>${subtotal.toFixed(2)}</span>
                </div>
                <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                    <span>Tax (10%):</span>
                    <span>${tax.toFixed(2)}</span>
                </div>
                <div style={{ 
                    display: 'flex', 
                    justifyContent: 'space-between',
                    fontWeight: 'bold',
                    fontSize: '1.25rem',
                    marginTop: '0.5rem',
                    paddingTop: '0.5rem',
                    borderTop: '2px solid #d1d5db'
                }}>
                    <span>Total:</span>
                    <span>${total.toFixed(2)}</span>
                </div>
            </div>
        </CommandForm>
    );
}
```

## Integration with External Libraries

Integrate CommandForm with UI component libraries:

```tsx
// Example with a modal library
import { Modal } from 'some-modal-library';

function EditUserModal({ userId, onClose }: { userId: string, onClose: () => void }) {
    const command = useCommandInstance(UpdateUser, { id: userId });
    const [open, setOpen] = useState(true);
    
    useEffect(() => {
        // Load user data
        const loadUser = async () => {
            const user = await fetchUser(userId);
            Object.assign(command, user);
        };
        loadUser();
    }, [userId]);
    
    const handleSubmit = async () => {
        const result = await command.execute();
        if (result.isSuccess) {
            setOpen(false);
            onClose();
        }
    };
    
    return (
        <Modal open={open} onClose={onClose}>
            <h2>Edit User</h2>
            <CommandForm<UpdateUser> command={UpdateUser} initialValues={command}>
                <InputTextField<UpdateUser> value={c => c.name} title="Name" required />
                <InputTextField<UpdateUser> value={c => c.email} type="email" title="Email" required />
            </CommandForm>
            <button onClick={handleSubmit}>Save</button>
            <button onClick={onClose}>Cancel</button>
        </Modal>
    );
}
```

## Best Practices

1. **Separation of Concerns**: Keep business logic in command handlers, UI logic in components
2. **Error Handling**: Always handle errors from `execute()` and `validate()`
3. **Performance**: Use `React.memo` for expensive custom field components
4. **Cleanup**: Clear timeouts and cancel async operations in useEffect cleanup
5. **Type Safety**: Leverage TypeScript for command definitions and props
6. **Testing**: Test forms with various states (empty, invalid, valid, submitting)
7. **Accessibility**: Ensure custom layouts maintain proper tab order and focus management

## See Also

- [CommandForm Overview](./index.md)
- [Built-in Field Types](./field-types.md)
- [Customization](./customization.md)
- [Validation](./validation.md)
- [Commands Overview](../index.md)
