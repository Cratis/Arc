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

## Populating a Form from a Query

The examples above load data for a *field's options* - the form's own values still come from `initialValues`/`currentValues`, fetched by hand. `CommandForm` can fetch its initial values itself instead, from a single-instance query:

```tsx
import { CommandForm, InputTextField } from '@cratis/arc.react/commands';
import { GetUserProfile } from './queries';
import { UpdateProfile } from './commands';

function ProfileEditor({ userId }: { userId: string }) {
    return (
        <CommandForm
            command={UpdateProfile}
            populateFromQuery={GetUserProfile}
            populateFromQueryArgs={{ userId }}
        >
            <InputTextField<UpdateProfile> value={c => c.firstName} title="First name" />
            <InputTextField<UpdateProfile> value={c => c.lastName} title="Last name" />
            <InputTextField<UpdateProfile> value={c => c.email} title="Email" />
        </CommandForm>
    );
}
```

Each field is matched onto the query's result by property name - the command's `firstName` is seeded from the query result's `firstName`, and so on. `GetUserProfile` must be a **single-instance query** - a query returning multiple instances throws. For an observable query, use `populateFromObservableQuery` instead; both read their arguments from `populateFromQueryArgs`.

The resolved data becomes the form's **baseline**, not just a one-time overlay - `command.hasChanges` reads `false` right after the query resolves, and only an edit the user actually makes flips it to `true`. Validation reruns only when the resolved data itself changes, not on every unrelated re-render - so a query that happens to return the same value twice does not re-trigger validation.

### Per-field control

Two field props refine what a query populates:

| Prop | Effect |
|---|---|
| `noInitialValue` | Skips this field entirely, even if the query result has a same-named property. |
| `initialValue` | Overrides how the field's value is derived from the query result - either a property accessor matched by name, or an arbitrary function that composes a value from the whole result. |

```tsx
<InputTextField<UpdateProfile>
    value={c => c.displayName}
    initialValue={(profile: UserProfile) => `${profile.firstName} ${profile.lastName}`}
    title="Display name"
/>
```

## See Also

- [CommandForm Overview](./index.md)
- [Working with Hooks](./hooks.md)
- [Field Types](./field-types/index.md)
