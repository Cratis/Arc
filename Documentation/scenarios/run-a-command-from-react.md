---
title: Run a Cratis Arc command from React
description: Submit a Cratis Arc command from a React form using a generated proxy and CommandDialog, with validation feedback and submission state.
---

Use a generated Cratis Arc command proxy to submit a React form without writing a separate API client. `CommandDialog` binds the fields to the command, displays validation feedback, and prevents another submission while the command is running. This recipe registers an author from a dialog.

## Call the proxy, don't write a client

The generated proxy carries the command's shape and supported validation rules into TypeScript. Use that contract rather than redeclaring it beside a `fetch` call. The server still validates every submitted command.

## Before you start

Follow your [backend's setup and proxy-generation guide](/arc/backend/) and the [full-stack tutorial setup](/arc/tutorial/first-slice/) through the React bootstrap. You need the `RegisterAuthor` command, its generated proxy, the Components dependencies, and the providers and dialog composition shown there. For the C# backend, generate the proxy with the configured Debug [proxy-generation build](/arc/backend/csharp/proxy-generation/getting-started/). This standalone flow does not require Cratis Chronicle.

For an inline form instead of a dialog, [frontend getting started](/arc/frontend/react/getting-started/) shows Arc's `CommandForm` with Components fields and a route without Components.

## Do it

1. Pass the generated command constructor to `CommandDialog` and bind an input to its `name` property. The dialog creates the command instance and supplies confirm/cancel buttons:

   ```tsx title="AddAuthor.tsx"
   import { useState } from 'react';
   import { Guid } from '@cratis/fundamentals';
   import { CommandDialog } from '@cratis/components/CommandDialog';
   import { InputTextField } from '@cratis/components/CommandForm';
   import { RegisterAuthor } from './Authors/RegisterAuthor';   // generated proxy

   export const AddAuthor = () => {
       const [id] = useState(() => Guid.create());
       return (
           <CommandDialog<RegisterAuthor> command={RegisterAuthor} title="Add author" okLabel="Add" initialValues={{ id }}>
               <InputTextField<RegisterAuthor> value={i => i.name} title="Name" />
           </CommandDialog>
       );
   };
   ```

   Mount a new dialog instance per operation and unmount on close. `id` is stable during editing and fresh for the next registration; a required Guid is not automatically filled by the proxy.

   The field accessor `i => i.name` refers to the generated type. If you rename that property on the backend, regenerate the proxy and type-check the frontend to find references that need updating.

2. Supply values the user should not type through `initialValues`. For a book, that includes the parent author identifier. Do not supply required values only in `onBeforeExecute`: it runs after validation, too late to make an invalid form valid.

   This alternative form fragment needs the [book dialog's imports and per-operation `bookId` state](/arc/tutorial/books-and-relationships/). Keep `authorId` as the generated `Guid`, not a string:

   ```tsx
   <CommandDialog<AddBook> command={AddBook} title="Add book" initialValues={{ authorId, bookId }}>
       <InputTextField<AddBook> value={i => i.title} title="Title" />
   </CommandDialog>
   ```

3. Open the dialog through the tutorial's dialog composition and submit a valid name. Supported rules extracted from the command and its value types run client-side. Async, custom, and state-dependent rules still run on the server; returned field messages appear in the dialog.

## Check the result

With the tutorial's required-name rule in place, leave the name empty and confirm that submission is disabled. Enter a valid, unused name and submit: confirm should stay disabled while the request runs, and a successful registration should close the dialog. Check the stored author or author query as well; closing a dialog alone does not prove that the intended data was written.

With the tutorial's duplicate-name validation in place, submit an existing name. The dialog should stay open and show the name-field feedback. If a required value leaves confirm disabled, check `initialValues` before adding a submit callback.

For a plain button or custom flow without a dialog, call the proxy's `execute()` method. Check the returned `CommandResult.isSuccess` before reading `response`; the [Command Result](/arc/frontend/core/commands/command-result/) guide covers failure handling.

## See also

- [Dialogs](/components/) — `CommandDialog`, `Dialog`, and the form-field components.
- [Command Result](/arc/frontend/core/commands/command-result/) — handling the result in code.
- [Validate a command](./validate-a-command.mdx) — the rules that surface in the form.
