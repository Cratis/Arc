# Async Data Loading

Load data asynchronously for use in form fields.

## Loading Options for SelectField

```tsx
function OrderForm() {
    const [products, setProducts] = useState<Array<{ id: string; name: string }>>([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        const loadProducts = async () => {
            try {
                const data = await fetch('/api/products').then((r) => r.json());
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
                value={(c) => c.productId}
                title='Product'
                options={products}
                optionIdField='id'
                optionLabelField='name'
                placeholder='Select a product...'
                required
            />
            <NumberField<CreateOrder>
                value={(c) => c.quantity}
                title='Quantity'
                min={1}
                required
            />
        </CommandForm>
    );
}
```

## Dependent Dropdowns

Load options based on another field's value:

```tsx
function LocationForm() {
    const command = useCommandInstance(SaveLocation);
    const [cities, setCities] = useState<Array<{ id: string; name: string }>>([]);

    useEffect(() => {
        if (command.country) {
            // Load cities for selected country
            const loadCities = async () => {
                const data = await fetch(`/api/cities?country=${command.country}`).then(
                    (r) => r.json(),
                );
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
                value={(c) => c.country}
                title='Country'
                options={countries}
                optionIdField='id'
                optionLabelField='name'
                required
            />

            <SelectField<SaveLocation>
                value={(c) => c.city}
                title='City'
                options={cities}
                optionIdField='id'
                optionLabelField='name'
                placeholder={
                    command.country ? 'Select a city...' : 'Select country first'
                }
                required
            />
        </CommandForm>
    );
}
```

## Populating a Form from a Query

The examples above load data for a _field's options_ - the form's own values still come from `initialValues`/`currentValues`, fetched by hand. `CommandForm` can fetch its initial values itself instead, from a single-instance query:

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
            <InputTextField<UpdateProfile>
                value={(c) => c.firstName}
                title='First name'
            />
            <InputTextField<UpdateProfile> value={(c) => c.lastName} title='Last name' />
            <InputTextField<UpdateProfile> value={(c) => c.email} title='Email' />
        </CommandForm>
    );
}
```

Each field is matched onto the query's result by property name - the command's `firstName` is seeded from the query result's `firstName`, and so on. `GetUserProfile` must be a **single-instance query** - a query returning multiple instances throws. For an observable query, use `populateFromObservableQuery` instead; both read their arguments from `populateFromQueryArgs`.

The resolved data becomes the form's **baseline**, not just a one-time overlay - `command.hasChanges` reads `false` right after the query resolves, and only an edit the user actually makes flips it to `true`. Validation reruns only when the resolved data itself changes, not on every unrelated re-render - so a query that happens to return the same value twice does not re-trigger validation.

### Per-field control

Two field props refine what a query populates:

| Prop             | Effect                                                                                                                                                                               |
| ---------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `noInitialValue` | Skips this field entirely, even if the query result has a same-named property.                                                                                                       |
| `initialValue`   | Overrides how the field's value is derived from the query result - either a property accessor matched by name, or an arbitrary function that composes a value from the whole result. |
| `populationKey`  | Identifies semantics captured by `initialValue`, such as a locale. Change it to recompute the field from the current query result with the latest callback.                          |

```tsx
<InputTextField<UpdateProfile>
    value={(c) => c.displayName}
    initialValue={(profile: UserProfile) => `${profile.firstName} ${profile.lastName}`}
    title='Display name'
/>
```

An inline callback may close over values that CommandForm cannot discover. Give that semantic input a
`populationKey`; when the key changes, CommandForm evaluates the latest callback once against the
current population source and stores the result as the new baseline:

```tsx
function DisplayNameField({ locale }: { locale: string }) {
    return (
        <InputTextField<UpdateProfile>
            value={(c) => c.displayName}
            initialValue={(profile: UserProfile) =>
                locale === 'nb'
                    ? `${profile.lastName}, ${profile.firstName}`
                    : `${profile.firstName} ${profile.lastName}`
            }
            populationKey={locale}
            title='Display name'
        />
    );
}
```

CommandForm keeps each mounted field registration stable and always retains the latest accessor and
`initialValue` callback. Recreating an equivalent inline function does not repopulate the field. The
callback runs again only when the population source changes, automatic field metadata changes, or its
`populationKey` changes. Use a stable semantic value such as a locale, selected mode, or revision - not
a newly created object on every render.

## See Also

- [CommandForm Overview](./index.md)
- [Working with Hooks](./hooks.md)
- [Field Types](./field-types/index.md)
