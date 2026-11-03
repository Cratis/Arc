---
title: Slice context
description: Read the module, feature and slice a React component is rendered in with useSliceContext(), resolved from commands, queries, slice folders or an explicit override.
---

A report button, a help link or an error panel often needs to know which part of the application it is shown in. Passing the module, feature and slice down as props to every such component is tedious, and the values drift from the backend namespaces they describe. `useSliceContext()` gives any component that location without the developer passing anything.

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

The location is resolved from slice scopes in the React tree. `<Arc>` provides the root scope, so the hook works anywhere inside it. Outside `<Arc>` and any `<Slice>`, it returns `SliceLocation.empty`.

Four sources feed a scope:

- **Commands and queries in use.** `useCommand`, `useQuery`, `useQueryWithPaging`, `useObservableQuery`, `useObservableQueryWithPaging`, `useChangeStream` and the suspense query hooks register the slice location of their proxy on the nearest scope while the component is mounted. Every component rendered in that scope, including shared components nested inside other components, sees it.
- **View models.** A component made with `withViewModel` from `@cratis/arc.react.mvvm` registers the slice locations of the commands and queries resolved for its view model, the same way the hooks do. See [view models](#view-models).
- **The slice folder of a component.** With the [`SliceLocationPlugin`](#resolve-from-slice-folders-at-build-time) in your Vite configuration, exported components in slice folders render within a scope for their folder: the component itself, and everything it renders, sees the folder location.
- **An explicit override.** `<Slice module="..." feature="..." slice="...">` sets the location for everything rendered within it.

When commands and queries from different slices are in use in the same scope, the scope keeps the parts they agree on, from the module down. A page using `Billing/Invoices/Pay` and `Billing/Invoices/Listing` resolves to module `Billing`, feature `Invoices` and an empty slice. When they do not even share a module, the scope falls back to its enclosing scope.

### Precedence

1. **Explicit override.** An enclosing `<Slice>` with a location wins over everything nested within it, until a deeper explicit `<Slice>` takes over. Commands, queries and slice folders within it are ignored.
2. **Slice folder.** The folder location is used, refined by commands and queries registered within it when their location lies on the folder's path, compared part by part. A feature page in `Billing/Invoices` that uses the `Pay` command resolves to `Billing/Invoices/Pay`; a command from `Sales/Orders`, or from the nested feature `Billing/Invoices.Recurring`, does not change it.
3. **Commands and queries in use**, combined as described above.
4. **The enclosing scope**, when a scope has none of the above.

The location from commands and queries is registered when the component using them has mounted, so the first render of a component sees the location from its enclosing scope and the hook updates it right after. Suspense query hooks register once the component stops suspending. A query hook given `isEnabled: false` registers nothing until it is enabled.

## View models

`withViewModel` registers, on the nearest scope, the slice location of every command and query proxy the dependency container resolves for the view model: constructor-injected commands and queries, and those the view model resolves through its container later. A view model that injects `PayInvoice` and `AllInvoices` gives the scope `Billing/Invoices`, the parts the two agree on.

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

Arc applications keep the React components of a vertical slice in the slice folder, next to the backend code. `SliceLocationPlugin` from `@cratis/arc.vite/slices` maps that folder to a slice location, so components in slice folders without commands or queries, such as feature composition pages, also provide a location:

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

The plugin lives in its own entry point because it parses modules with the native `oxc-parser`; importing `EmitMetadataPlugin` from `@cratis/arc.vite` does not load it.

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

This differs on purpose from how the backend maps a namespace that is too shallow, where the deepest parts are filled (see [slice location](../../backend/csharp/proxy-generation/Configuration/slice-location.md#how-it-is-resolved)). A command or query always lives in a slice, so a short namespace still names its slice; a component in a feature folder belongs to the feature, not to a slice. Configure `roots` and `segmentsToSkip` to match the `NamespaceRoot` and `CratisProxiesSegmentsToSkip` of your backend, and keep slices three levels deep, so both sides agree.

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

A function counts as a component only by what the function itself returns: every value it returns must be JSX, `null` or `undefined`, possibly through `?:`, `&&`, `||`, `??` or a constant declared in the function, and at least one must be JSX. Functions nested in it are never looked into, so a store such as `create(set => ({ render: () => <div /> }))` or a function returning column definitions with JSX renderers is left alone. Exported functions, and exported calls given such a function, such as `memo(...)`, `forwardRef(...)` or `withViewModel(...)`, are wrapped. Named exports must start with an uppercase letter. Classes, async functions and unexported components are left alone.

An exported function declaration becomes a constant, so it is no longer hoisted: code at the top level of the same module that uses the component before its declaration fails. Use it after the declaration, or move the component to its own module.

### Fast Refresh

The component goes first in the `withSliceLocation(...)` call because React Fast Refresh, in both `@vitejs/plugin-react` and `@vitejs/plugin-react-swc`, looks only into the first argument of a call to find a component. It registers both the original function and the exported wrapper, so editing a component keeps its state as before. Every export stays a function with an uppercase name, or a `memo` or `forwardRef` component, so the module is still accepted as a Fast Refresh boundary. Keep `SliceLocationPlugin` in the plugin list; it runs before the React plugin on its own.

:::caution[Keep shared components out of the roots]
A slice scope follows the rendered tree, not where an element was created. A shared component in a slice folder, such as a panel that renders its `children`, would give those children its own folder location. Keep shared components outside the roots, or leave their folders out with `exclude`.
:::

Without the plugin, everything else on this page still works; only the folder source is missing.

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
