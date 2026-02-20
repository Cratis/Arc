# Form Lifecycle

Control form behavior throughout its lifecycle with hooks, state management, and auto-save functionality.

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

## See Also

- [CommandForm Overview](./index.md)
- [Working with Hooks](./hooks.md)
- [Validation](./validation.md)
