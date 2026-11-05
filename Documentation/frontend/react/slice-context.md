---
title: Cratis Arc React slice context
description: Read a React component's module, feature and slice with useSliceContext(), using generated proxy metadata, folder locations or an explicit boundary.
---

Cratis Arc's `useSliceContext()` tells a React component which module, feature and slice it is rendered in. Use it in a report button, help link or error panel that needs to identify its part of the application without receiving location props through every parent.

The hook reads a slice scope, a boundary in the React tree that resolves a location for the components inside it. In this example, `report` is your application's reporting callback:

```tsx
import { useSliceContext } from '@cratis/arc.react/slices';

export const ReportButton = () => {
    const location = useSliceContext();

    return (
        <button onClick={() => report(location.module, location.feature, location.slice)}>
            Report a problem in {location.toString()}
        </button>
    );
};
```

The hook returns a `SliceLocation` with `module`, `feature` and `slice`. When nothing is known it returns `SliceLocation.empty`; a part that cannot be determined is an empty string, never `null` or `undefined`. `SliceLocation` is the same type that generated proxies carry; see [slice location](../../backend/csharp/proxy-generation/Configuration/slice-location.md) for how the backend resolves it.

## Where the location comes from

`<Arc>` provides the root scope. Commands and queries register their generated proxy's location on the nearest scope while their component is mounted. Shared components in that scope see the same location, even when nested inside other components. This applies to `useCommand`, `useQuery`, `useQueryWithPaging`, `useObservableQuery`, `useObservableQueryWithPaging`, `useChangeStream` and the suspense query hooks. [View models](#view-models) register the proxies resolved through their container too.

For a component with no commands or queries, the [`SliceLocationPlugin`](#resolve-from-slice-folders-at-build-time) can derive a location from its folder. You can also declare a location with `<Slice module="..." feature="..." slice="...">`.

Outside both `<Arc>` and a `<Slice>` boundary, the hook returns `SliceLocation.empty`.

When commands and queries from different slices are in use in the same scope, the scope keeps the parts they agree on, from the module down. A page using `Billing/Invoices/Pay` and `Billing/Invoices/Listing` resolves to module `Billing`, feature `Invoices` and an empty slice. When they do not even share a module, the scope falls back to its enclosing scope.

### Precedence

1. An explicit `<Slice>` location wins until a deeper explicit `<Slice>` takes over. Commands, queries and slice folders inside it do not change the location.
2. A folder location is refined by registered commands and queries only when their location lies on its path, compared part by part. A page in `Billing/Invoices` using `Pay` resolves to `Billing/Invoices/Pay`. Neither `Sales/Orders` nor the distinct feature `Billing/Invoices.Recurring` changes it.
3. Without an override or folder location, registered command and query locations are combined as described above.
4. With no local location, the scope falls back to its parent.

Registration happens after the component mounts, so its first render sees the enclosing scope's location before the hook updates. Suspense query hooks register once the component stops suspending. A query with `isEnabled: false` contributes no location until enabled.

## View models

If you use `withViewModel` from `@cratis/arc.react.mvvm`, it registers the command and query proxies resolved through that view model's dependency container. This covers constructor injection and later resolutions through the same container. Injecting `PayInvoice` and `AllInvoices`, for example, gives the scope their common location, `Billing/Invoices`.

Commands and queries the view model creates with `new`, or resolves from the root `container` instead of the one it was given, are not seen. Register those yourself with `useRegisterSliceLocation(SliceLocation.ofType(PayInvoice))` in the component, or wrap it in a `<Slice>`.

## Override the location

Use `<Slice>` for components that use no commands or queries and are not in a slice folder, or when the automatic location is wrong for a part of the tree:

```tsx
import { Slice } from '@cratis/arc.react/slices';

export const Help = () => (
    <Slice module="Support" feature="Help">
        <HelpArticles />
        <ReportButton />
    </Slice>
);
```

Parts you leave out are empty. You can also pass a `SliceLocation` instance, for example the one a generated proxy carries:

```tsx
<Slice location={PayInvoice.sliceLocation}>
    <PaymentSummary />
</Slice>
```

`<Slice>` without any location is a boundary instead of an override. Commands and queries used within it register on it and do not affect the components outside it. When nothing within it is registered, it falls back to its enclosing scope.

```tsx
<Slice>
    <PayInvoiceForm />
    <ReportButton />
</Slice>
```

## Resolve from slice folders at build time

If you keep a feature's React components beside its backend code, `SliceLocationPlugin` can derive their location from the folder. This gives a composition page a location even if it has no command or query of its own. Add the plugin from `@cratis/arc.vite/slices` and configure the roots that contain your modules:

```ts
// vite.config.ts
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { SliceLocationPlugin } from '@cratis/arc.vite/slices';

export default defineConfig({
    plugins: [
        react(),
        SliceLocationPlugin({
            roots: ['Features'],
            exclude: /[\\/]Features[\\/]Shared[\\/]/
        })
    ]
});
```

This example uses `Features` as its root. Choose roots for your own folder layout rather than copying that name unchanged. The plugin has a separate entry point because it loads the native `oxc-parser`; importing `EmitMetadataPlugin` from `@cratis/arc.vite` does not load the slice plugin.

| Option | Description |
| --- | --- |
| `roots` | Folders whose subfolders are modules, features and slices, absolute or relative to the Vite root. When several match a file, the longest wins. |
| `segmentsToSkip` | For files under none of the roots, the number of leading folders below the Vite root to skip. When not set, those files are left alone. |
| `include` | The files to process. Defaults to `.tsx` and `.jsx` files. |
| `exclude` | Files to leave alone, such as folders holding shared components. `node_modules` is always left alone. |
| `importFrom` | Where the generated code imports from. Defaults to `@cratis/arc.react/slices`. |

### Folders map from the module down

The first folder below a root is the module, the last is the slice and those in between form the feature, joined with a period. With fewer than three folders, the parts are filled from the module down and the slice part is empty, never `null`:

| File | Module | Feature | Slice |
| --- | --- | --- | --- |
| `Features/Billing/Invoices/Pay/Pay.tsx` | `Billing` | `Invoices` | `Pay` |
| `Features/Billing/Invoices/Recurring/Pay/Pay.tsx` | `Billing` | `Invoices.Recurring` | `Pay` |
| `Features/Billing/Invoices/Invoices.tsx` | `Billing` | `Invoices` | _(empty)_ |
| `Features/Billing/Billing.tsx` | `Billing` | _(empty)_ | _(empty)_ |

The backend fills the deepest parts of a short namespace, as explained in [slice location](../../backend/csharp/proxy-generation/Configuration/slice-location.md#how-it-is-resolved). Folder mapping fills from the module down so a component in a feature folder belongs to that feature, not to a slice.

Align `roots` and `segmentsToSkip` with your backend's `NamespaceRoot` and `CratisProxiesSegmentsToSkip`. A module/feature/slice layout gives both mappings three levels to work with. If you use a shallower layout, compare the folder location with the generated proxy's `sliceLocation` before relying on them to match.

### What the plugin changes

The plugin wraps each exported component at its declaration with `withSliceLocation`:

```tsx
// Features/Billing/Invoices/Pay/Pay.tsx, as written
export function Pay(props: PayProps) {
    const [payInvoice] = PayInvoice.use();
    return <PaymentForm command={payInvoice} />;
}

// as the plugin passes it on
export const Pay = withSliceLocation(function Pay(props: PayProps) {
    const [payInvoice] = PayInvoice.use();
    return <PaymentForm command={payInvoice} />;
}, new SliceLocation('Billing', 'Invoices', 'Pay'), 'Pay');
```

The wrapped component renders the original inside a slice scope for its folder, so the component's own `useSliceContext()` reads the folder location and its own command and query hooks register within that scope. The wrapper keeps the component's name and `displayName`, forwards refs to `forwardRef` components, and stays a `memo` component when given one.

The plugin identifies components from the function's own return values. Every return must resolve to JSX, `null` or `undefined`, and at least one must be JSX. It follows `?:`, `&&`, `||`, `??` and local constants, but does not inspect nested functions. A store such as `create(set => ({ render: () => <div /> }))` or a function returning column definitions is therefore left alone.

It wraps exported component functions and exported calls given such a function, including `memo(...)`, `forwardRef(...)` and `withViewModel(...)`. Named exports must start with an uppercase letter. Classes, async functions and unexported components are not wrapped.

An exported function declaration becomes a constant, so it is no longer hoisted: code at the top level of the same module that uses the component before its declaration fails. Use it after the declaration, or move the component to its own module.

### Fast Refresh

Keep `SliceLocationPlugin` in the plugin list; it runs before the React plugin automatically. The transform puts the component first in `withSliceLocation(...)` so both `@vitejs/plugin-react` and `@vitejs/plugin-react-swc` can find it for Fast Refresh. The wrapper preserves component names and keeps `memo` and `forwardRef` components recognizable to React's refresh handling.

:::caution[Keep shared components out of the roots]
A slice scope follows the rendered tree, not where an element was created. A shared component in a slice folder, such as a panel that renders its `children`, would give those children its own folder location. Keep shared components outside the roots, or leave their folders out with `exclude`.
:::

The plugin is optional. Without it, command and query registration and explicit `<Slice>` boundaries still resolve locations; only folder-derived locations are missing.

## API

| Export from `@cratis/arc.react/slices` | Description |
| --- | --- |
| `useSliceContext()` | Returns the `SliceLocation` the component is rendered in. |
| `Slice` | Overrides the location with `location`, `module`, `feature` or `slice`; without them, a boundary. |
| `useRegisterSliceLocation(location)` | Registers a location on the nearest scope while mounted. Use it from custom hooks that work with commands or queries without the Arc hooks. |
| `withSliceLocation(component, location, exportedAs?)` | Returns a component that renders the given one within a scope for a slice folder; used by `SliceLocationPlugin`. Values that are not components are returned as they are. |
| `SliceLocation` | Re-exported from `@cratis/arc/reflection`. |
| `SliceScope`, `SliceScopeKind`, `SliceScopeContext` | The scope store and its React context, for advanced composition and tests. |

To read the location of the whole page from outside any slice scope, such as a top-bar button, see [application context](./application-context.md).
