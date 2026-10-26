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

Three sources feed a scope:

- **Commands and queries in use.** `useCommand`, `useQuery`, `useQueryWithPaging`, `useObservableQuery`, `useObservableQueryWithPaging`, `useChangeStream` and the suspense query hooks register the slice location of their proxy on the nearest scope while the component is mounted. Every component rendered in that scope, including shared components nested inside other components, sees it.
- **The slice folder of a component.** With the [`SliceLocationPlugin`](#resolve-from-slice-folders-at-build-time) in your Vite configuration, exported components in slice folders give what they render the location of their folder.
- **An explicit override.** `<Slice module="..." feature="..." slice="...">` sets the location for everything rendered within it.

When commands and queries from different slices are in use in the same scope, the scope keeps the parts they agree on, from the module down. A page using `Billing/Invoices/Pay` and `Billing/Invoices/Listing` resolves to module `Billing`, feature `Invoices` and an empty slice. When they do not even share a module, the scope falls back to its enclosing scope.

### Precedence

1. **Explicit override.** An enclosing `<Slice>` with a location wins over everything nested within it, until a deeper explicit `<Slice>` takes over. Commands, queries and slice folders within it are ignored.
2. **Slice folder.** The folder location is used, refined by commands and queries registered within it when their location lies on the folder's path. A feature page in `Billing/Invoices` that uses the `Pay` command resolves to `Billing/Invoices/Pay`; a command from `Sales/Orders` does not change it.
3. **Commands and queries in use**, combined as described above.
4. **The enclosing scope**, when a scope has none of the above.

The location from commands and queries is registered when the component using them has mounted, so the first render of a component sees the location from its enclosing scope and the hook updates it right after. Suspense query hooks register once the component stops suspending.

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

Arc applications keep the React components of a vertical slice in the slice folder, next to the backend code. `SliceLocationPlugin` from `@cratis/arc.vite` maps that folder to a slice location, so components in slice folders without commands or queries, such as feature composition pages, also provide a location:

```ts
// vite.config.ts
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { SliceLocationPlugin } from '@cratis/arc.vite';

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

| Option | Description |
| --- | --- |
| `roots` | Folders whose subfolders are modules, features and slices, absolute or relative to the Vite root. When several match a file, the longest wins. |
| `segmentsToSkip` | For files under none of the roots, the number of leading folders below the Vite root to skip. When not set, those files are left alone. |
| `include` | The files to process. Defaults to `.tsx` and `.jsx` files. |
| `exclude` | Files to leave alone, such as folders holding shared components. `node_modules` is always left alone. |
| `importFrom` | Where the generated code imports from. Defaults to `@cratis/arc.react/slices`. |

The folder segments are split the same way the proxy generator splits namespace segments: the first is the module, the last is the slice and those in between form the feature, joined with a period. With fewer than three segments the deepest parts are filled, so `Features/Billing/Invoices/Invoices.tsx` resolves to feature `Billing` and slice `Invoices`, and its string form is `Billing/Invoices`. Configure `roots` and `segmentsToSkip` to match the `NamespaceRoot` and `CratisProxiesSegmentsToSkip` of your backend so both sides agree.

The plugin changes only exported components that render JSX: exported functions, and functions passed to exported calls such as `memo(...)`, `forwardRef(...)` or `withViewModel(...)`. Named exports must start with an uppercase letter. Every expression such a component returns is wrapped with `withSliceLocation(location, ...)`, which puts a slice scope around what the component renders. Functions nested in the component, unexported components and classes are left alone.

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
| `withSliceLocation(location, children)` | Wraps rendered output in a scope for a slice folder; used by `SliceLocationPlugin`. |
| `SliceLocation` | Re-exported from `@cratis/arc/reflection`. |
| `SliceScope`, `SliceScopeKind`, `SliceScopeContext` | The scope store and its React context, for advanced composition and tests. |
