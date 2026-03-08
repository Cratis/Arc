# Async Data Loading

Load data asynchronously for use in form fields.

## Loading Options for SelectField

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
        <CommandForm command={CreateOrder}>
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

## Dependent Dropdowns

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
        <CommandForm command={SaveLocation}>
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

## See Also

- [CommandForm Overview](./index.md)
- [Working with Hooks](./hooks.md)
- [Field Types](./field-types.md)
