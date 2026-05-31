---
title: Run a command from React
description: Wire a React form or button to a command through the generated proxy — with CommandDialog handling instantiation, validation, and the buttons.
---

**Goal:** a user clicks a button or fills a form, and a command runs — with validation feedback, a disabled-while-running button, and no hand-written API client.

## Call the proxy, don't write a client

When the backend builds, Arc generates a typed proxy for every command. You don't write a `fetch`, redeclare the command's shape, or wire validation — the proxy already knows the types, and `CommandDialog` drives the whole interaction.

## Do it

1. **Use `CommandDialog` for a form.** Pass the command constructor; the dialog instantiates it, renders the confirm/cancel buttons, and disables confirm while it executes. Use command form fields for the inputs:

   ```tsx title="AddAuthor.tsx"
   import { CommandDialog } from '@cratis/components/CommandDialog';
   import { InputTextField } from '@cratis/components/CommandForm';
   import { RegisterAuthor } from './Authors/RegisterAuthor';   // generated proxy

   export const AddAuthor = () => (
       <CommandDialog<RegisterAuthor> command={RegisterAuthor} title="Add author" okLabel="Add">
           <InputTextField<RegisterAuthor> value={i => i.name} title="Name" />
       </CommandDialog>
   );
   ```

   The field accessor `i => i.name` is a property on the generated type — rename it in C#, rebuild, and the frontend stops compiling until you fix it.

2. **Inject context the form needs to be valid via `initialValues`.** A parent id the user shouldn't type goes here — not in `onBeforeExecute`, which fires too late to affect validity:

   ```tsx
   <CommandDialog<AddBook> command={AddBook} initialValues={{ authorId, bookId: Guid.create() }}>
       <InputTextField<AddBook> value={i => i.title} title="Title" />
   </CommandDialog>
   ```

3. **Validation surfaces itself.** Rules you wrote on the command (and its value types) run client-side and on the server, and `CommandDialog` renders the messages against the fields. You write none of that wiring.

For executing a command without a dialog (a plain button, custom flow), the proxy exposes an execute call that returns a `CommandResult` — check `isSuccess` and read any returned value off it.

## See also

- [Dialogs](/components/) — `CommandDialog`, `Dialog`, and the form-field components.
- [Command Result](/arc/frontend/core/commands/command-result/) — handling the result in code.
- [Validate a command](./validate-a-command.md) — the rules that surface in the form.
