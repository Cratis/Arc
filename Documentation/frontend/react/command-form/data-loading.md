---
title: CommandForm data loading and initial values
description: Load select options in React and populate Cratis Arc CommandForm from a query, choosing which values establish the baseline for change tracking.
---

Load option lists in React state and keep the selected values on the command owned by Cratis Arc `CommandForm`. To edit an existing record, populate that same form from a query or mount it with loaded initial values. The choice determines which values count as unchanged before the user starts editing.

## Loading options for SelectField

Wait for the product list before mounting the order form so the user can choose from loaded options. This example expects a generated `CreateOrder` with `productId: string` and `quantity: number`. Supply `loadProducts` from your application; it returns the option objects and is not a Cratis Arc API.

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

When one selection controls another, read the first value from inside the form rather than creating a second command. `CityField` below uses the zero-argument `useCommandInstance<SaveLocation>()` to load cities for the country being edited. The generated `SaveLocation` needs string `country` and `city` properties; the caller supplies countries and a stable city loader.

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

Use `populateFromQuery` when loaded values should become the baseline against which later edits are compared. The query must return a single object rather than a collection. This example expects generated `GetUserProfile` and `UpdateProfile` classes: the query accepts `userId` and returns matching `name`/`email` properties. If the update command also needs an ID, seed it explicitly; query arguments do not initialize command properties.

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

Population does not supply loading or error UI. If editing or saving before a successful load would be unsafe, load and inspect the query result in the parent, show its loading/error state there, and mount the form only when ready. `isValid` describes validation, not whether the query has loaded.

## Initial and current values

| Source | Behavior |
| --- | --- |
| `initialValues` | Synchronous seed/baseline. Undefined entries supply nothing. Changing this prop alone after mount does not repopulate the form. |
| Field `currentValue` | Seed/baseline for that field; undefined supplies nothing. |
| Population query | Maps registered fields and updates their baselines when resolved values change. |
| `currentValues` | Reactive overlay. A present key is written even if null/undefined (when allowed by the command type); an absent key leaves the property alone. Later overlay edits do not reset the baseline. |

Where layers overlap, the merge order from lowest to highest priority is `currentValues`, query population, field `currentValue`, then defined `initialValues`. Give each property one source where possible so a higher-priority seed does not mask the value you expected. For a late load that should count as unchanged, use query population or mount a new form with loaded `initialValues`. Later `currentValues` updates change values without resetting the baseline.

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

Changing the locale changes `populationKey`, so the form recomputes the display name from the current source. Recreating an equivalent inline callback alone does not repopulate a field. When the source or key changes, the form uses the latest callback committed by React, including when both change in the same commit. Use a stable value such as the locale, not a fresh object each render, and keep `initialValue` free of side effects.

Once population is in place, use the [form lifecycle callbacks](./form-lifecycle.md) to decide what happens after Save, including failures that should leave the editor open.

## See also

- [CommandForm overview](./index.md)
- [Working with hooks](./hooks.md)
- [Field types](./field-types/index.md)
- [Form lifecycle](./form-lifecycle.md)
