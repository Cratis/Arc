# Advanced Patterns

Advanced techniques and patterns for complex CommandForm scenarios.

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
- [Layouts](./layouts.md)
- [Working with Hooks](./hooks.md)
- [Form Lifecycle](./form-lifecycle.md)
