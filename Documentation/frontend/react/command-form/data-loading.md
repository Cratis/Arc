---
title: Async data loading
description: Load field options separately from command values, and populate a form's baseline from a query.
---

Options and edited values have different owners: React can hold a product list while CommandForm owns the selected product ID. Do not create a second command to load options for the form.

## Loading options for SelectField

This complete component expects a generated `CreateOrder` with `productId: string` and `quantity: number`. `loadProducts` is an application-owned function that returns option objects; it is not an Arc API.

```tsx
import { useEffect, useState } from 'react';
import { CommandForm, NumberField, SelectField } from '@cratis/arc.react/commands';
import { CreateOrder } from './commands/CreateOrder';

type Product = { id: string; name: string };
type OrderFormProps = { loadProducts: () => Promise<Product[]> };

export function OrderForm({ loadProducts }: OrderFormProps) {
    const [products, setProducts] = useState<Product[]>();
    const [error, setError] = useState('');

    useEffect(() => {
        let active = true;
        setProducts(undefined);
        setError('');
        void loadProducts().then(
            values => { if (active) setProducts(values); },
            () => { if (active) setError('Products could not be loaded.'); }
        );
        return () => { active = false; };
    }, [loadProducts]);

    if (error) return <p role="alert">{error}</p>;
    if (!products) return <p role="status">Loading products…</p>;
    return (
        <CommandForm command={CreateOrder} initialValues={{ productId: '', quantity: 1 }}>
            <SelectField<CreateOrder>
                value={c => c.productId} title="Product" options={products}
                optionIdField="id" optionLabelField="name" placeholder="Select a product"
            />
            <NumberField<CreateOrder> value={c => c.quantity} title="Quantity" min={1} />
            <button type="submit">Order</button>
        </CommandForm>
    );
}
```

Supply a stable `loadProducts` function (module-level or memoized) so parent renders do not restart the effect. The active flag ignores superseded/unmounted results; it does not abort an already sent request. Implement HTTP status checks and response decoding in your loader. The command must validate the selected product and quantity; `min={1}` is only an input attribute.

## Dependent dropdowns

Read the country from a **descendant** using the zero-argument `useCommandInstance<SaveLocation>()`. This complete component expects a generated `SaveLocation` with string `country` and `city` properties. The caller supplies countries and a stable application loader.

```tsx
import { useEffect, useState } from 'react';
import { CommandForm, SelectField, useCommandInstance } from '@cratis/arc.react/commands';
import { SaveLocation } from './commands/SaveLocation';

type Option = { id: string; name: string };
type LoadCities = (country: string) => Promise<Option[]>;
type CityResult = { country: string; options: Option[]; error: string };

function CityField({ loadCities }: { loadCities: LoadCities }) {
    const command = useCommandInstance<SaveLocation>();
    const country = command.country;
    const [result, setResult] = useState<CityResult>();

    useEffect(() => {
        let active = true;
        if (country) {
            void loadCities(country).then(
                options => { if (active) setResult({ country, options, error: '' }); },
                () => { if (active) setResult({ country, options: [], error: 'Cities could not be loaded.' }); }
            );
        }
        return () => { active = false; };
    }, [country, loadCities]);

    const current = country && result?.country === country ? result : undefined;
    return (
        <>
            <SelectField<SaveLocation>
                value={c => c.city} title="City" options={current?.options ?? []}
                optionIdField="id" optionLabelField="name"
                placeholder={!country ? 'Select a country first' : current ? 'Select a city' : 'Loading cities…'}
            />
            {current?.error && <p role="alert">{current.error}</p>}
        </>
    );
}

export function LocationForm({ countries, loadCities }: { countries: Option[]; loadCities: LoadCities }) {
    return (
        <CommandForm
            command={SaveLocation}
            initialValues={{ country: '', city: '' }}
            onFieldChange={(command, field, oldValue, newValue) => {
                if (field === 'country' && oldValue !== newValue) command.city = '';
            }}
        >
            <SelectField<SaveLocation>
                value={c => c.country} title="Country" options={countries}
                optionIdField="id" optionLabelField="name" placeholder="Select a country"
            />
            <CityField loadCities={loadCities} />
            <button type="submit">Save location</button>
        </CommandForm>
    );
}
```

Changing country clears the old city synchronously during the field edit, before that edit's silent validation. Matching the response to its country prevents stale options from appearing, and cleanup ignores superseded requests. Validate country/city membership on the server; an empty or failed option list is not an authorization mechanism.

## Populating a form from a query

Use `populateFromQuery` when the form should establish field baselines from a single-instance read. This complete component expects generated `GetUserProfile` and `UpdateProfile` classes; the query accepts `userId` and returns a single object with matching `name`/`email` properties. If your update command also needs an ID, seed that required property explicitly—it is not inferred from query arguments.

```tsx
import { CommandForm, InputTextField } from '@cratis/arc.react/commands';
import { GetUserProfile } from './queries/GetUserProfile';
import { UpdateProfile } from './commands/UpdateProfile';

export function ProfileEditor({ userId }: { userId: string }) {
    return (
        <CommandForm
            key={userId}
            command={UpdateProfile}
            populateFromQuery={GetUserProfile}
            populateFromQueryArgs={{ userId }}
        >
            <InputTextField<UpdateProfile> value={c => c.name} title="Name" />
            <InputTextField<UpdateProfile> value={c => c.email} title="Email" />
            <button type="submit">Save</button>
        </CommandForm>
    );
}
```

The query must be **single-instance**, not enumerable; an enumerable query throws. For an emitting source, use `populateFromObservableQuery` instead, with the same `populateFromQueryArgs` prop. Choose one source. Observable updates require an actual emitting backend source; Arc does not infer one from a database write.

Matching fields are seeded by name through `setInitialValues`, including falsy values. These populated properties become the change-tracking baseline. Unrelated edits are preserved when other populated properties change; a changed source value for an edited property can overwrite that edit. Decide whether live repopulation is appropriate for your editor. The `key` above deliberately creates a fresh form when the selected user changes.

Population is not a loading/error UI. If editing or saving before a successful load is unsafe, load and inspect the query result in the parent and mount the form only when ready. Do not mistake `isValid` for query-loading state.

## Initial and current values

| Source | Behavior |
| --- | --- |
| `initialValues` | Synchronous seed/baseline. Undefined entries supply nothing. Changing this prop alone after mount does not repopulate the form. |
| Field `currentValue` | Seed/baseline for that field; undefined supplies nothing. |
| Population query | Maps registered fields and updates their baselines when resolved values change. |
| `currentValues` | Reactive overlay. A present key is written even if null/undefined (when allowed by the command type); an absent key leaves the property alone. Later overlay edits do not reset the baseline. |

Where layers overlap, the merge order from lowest to highest priority is `currentValues`, query population, field `currentValue`, then defined `initialValues`. Avoid competing sources for the same property. For a late load that should be considered unchanged, use query population or mount a new form with loaded `initialValues`, rather than assuming every `currentValues` update resets change tracking.

## Per-field control

These population props do not restrict explicit `initialValues`/`currentValues` supplied by the caller:

| Prop | Effect |
| --- | --- |
| `noInitialValue` | Skips this field during query population. |
| `initialValue` | Computes the field's value from the source; defaults to matching the command property's name. |
| `populationKey` | Identifies captured semantics (for example locale) that should trigger recomputation from the current source. |

This **field fragment** belongs inside a form whose population query returns `UserProfile`, and whose generated `UpdateProfile` includes `displayName`. It needs the corresponding application type imports and a `locale` variable:

```tsx
<InputTextField<UpdateProfile>
    value={c => c.displayName}
    initialValue={(profile: UserProfile) => locale === 'nb'
        ? `${profile.lastName}, ${profile.firstName}`
        : `${profile.firstName} ${profile.lastName}`}
    populationKey={locale}
    title="Display name"
/>
```

Each mounted field has a stable registration. Recreating an equivalent inline callback does not repopulate it; only committed callbacks become visible. A source or `populationKey` change evaluates the latest committed callback, including when both change in the same commit. Use a stable semantic key, not a fresh object every render. Do not put side effects in `initialValue`.

## See also

- [CommandForm overview](./index.md)
- [Working with hooks](./hooks.md)
- [Field types](./field-types/index.md)
- [Form lifecycle](./form-lifecycle.md)
