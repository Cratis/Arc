---
title: Getting started (React)
description: From zero to a React screen that runs a type-safe query and command against your Arc backend — using the generated proxies.
---

This gets a React app talking to your Arc backend through the **generated proxies** — no hand-written API client. By the end you'll have a screen that reads data from a query and executes a command, both fully typed from your C#.

:::note[Backend first]
Proxies are generated when you **build the backend**. Make sure your C# commands and queries compile and you've run `dotnet build` before wiring up the frontend — until then the proxy files don't exist. If you scaffolded with `dotnet new cratis`, the frontend and a sample feature are already wired; this page explains what's happening so you can add your own.
:::

## 1. Set up the app

The `cratis` template gives you a Vite + React app with `@cratis/arc.react` and `@cratis/components` installed. Two things happen at startup:

- **Bindings are initialized** so the generated proxies know how to reach the backend.
- The app is wrapped in the Cratis providers (and the Components provider, if you use the component library).

```tsx
import { Bindings } from './Bindings';        // generated
import { CratisComponentsProvider } from '@cratis/components';

Bindings.initialize();

export const App = () => (
    <CratisComponentsProvider>
        <YourRoutes />
    </CratisComponentsProvider>
);
```

## 2. Read data with a query

A query you wrote in C# is generated as a typed proxy. If it's an **observable** query, the `.use()` hook re-renders the component whenever the underlying read model changes — live updates for free:

```tsx
import { AllAuthors } from './Authors/Author';   // generated proxy

export const Authors = () => {
    const [authors] = AllAuthors.use();
    return (
        <ul>
            {authors.data.map(a => <li key={String(a.id)}>{a.name}</li>)}
        </ul>
    );
};
```

## 3. Execute a command

Use `CommandDialog` to run a generated command. It instantiates the command, renders the form fields and the confirm/cancel buttons, and disables confirm while it executes:

```tsx
import { CommandDialog } from '@cratis/components/CommandDialog';
import { InputTextField } from '@cratis/components/CommandForm';
import { RegisterAuthor } from './Authors/RegisterAuthor';   // generated proxy

export const AddAuthor = () => (
    <CommandDialog<RegisterAuthor> command={RegisterAuthor} title="Add author" okLabel="Add">
        <InputTextField<RegisterAuthor> value={i => i.name} title="Name" />
    </CommandDialog>
);
```

Because `RegisterAuthor` is generated from your C# command, the field accessor `i => i.name` is type-checked — rename the property in C#, rebuild, and the frontend won't compile until you update it. That's the full-stack type safety working for you.

## What you did

- Initialized the generated **bindings** and mounted the providers.
- Read a read model through a **typed, observable query proxy**.
- Executed a **typed command** with `CommandDialog`.

## Next

- See the whole slice end to end in [Build a full-stack feature](/build-a-full-app/).
- Go deeper on the [React](./react/) integration, or the [MVVM with React](./react.mvvm/) approach.
- Browse the building blocks in [Components](/components/).
