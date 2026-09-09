---
title: Dialogs
description: Separate dialog rendering from requests and correctly handle result/response tuples.
---

Arc manages dialog visibility and request/response flow without choosing your component library. You provide the rendering; a caller awaits the user's decision. Components and PrimeReact are optional renderers, not Arc prerequisites.

## Custom dialogs

The following contract checkpoint uses a native nonmodal `<dialog open>` to keep the request/response example self-contained. For production modal UI, use an accessible renderer with focus management, dismissal, and labeling appropriate to your application, such as the optional [Components dialogs](/components/).

```tsx
import { DialogResult, useDialog, useDialogContext } from '@cratis/arc.react/dialogs';

type NameRequest = { currentName: string };

function NameDialog({ currentName }: NameRequest) {
    const { closeDialog } = useDialogContext<NameRequest, string>();
    return (
        <dialog open aria-label="Confirm name">
            <p>Use {currentName}?</p>
            <button onClick={() => closeDialog(DialogResult.Ok, currentName)}>Use name</button>
            <button onClick={() => closeDialog(DialogResult.Cancelled)}>Cancel</button>
        </dialog>
    );
}

export function Feature() {
    const [NameDialogWrapper, showNameDialog] = useDialog<string, NameRequest>(NameDialog);

    async function choose() {
        const [result, response] = await showNameDialog({ currentName: 'Ada' });
        if (result === DialogResult.Ok && response !== undefined) {
            console.log(response);
        }
    }

    return (
        <>
            <button onClick={() => void choose()}>Choose name</button>
            <NameDialogWrapper />
        </>
    );
}
```

React `useDialog<TResponse, TInput>(Component)` returns **three** elements: `[Wrapper, showDialog, context]`. The wrapper must be rendered; it mounts the dialog only while visible. `showDialog(input)` resolves to `[DialogResult, response?]`, not the response alone. Inside the dialog, use `useDialogContext<TInput, TResponse>()` or read request props. Notice the generic order differs between these two hooks.

`closeDialog(result, response?)` resolves the pending call. A cancellation may have no response, so check both the result and optional value. Do not open the same hook concurrently: it stores one pending resolver.

## Confirmation dialogs

`DialogButtons` supports `Ok`, `OkCancel`, `YesNo`, and `YesNoCancel`. `DialogResult` supports `None`, `Ok`, `Yes`, `No`, and **`Cancelled`**, not `Cancel`.

`useConfirmationDialog(title?, message?, buttons?)` returns `[showConfirmation]`. The delegate accepts optional overrides and resolves to **`DialogResult` only**, unlike custom dialog calls.

### Defining the confirmation dialog

This minimal renderer handles every button configuration. As above, the nonmodal native presentation is a contract example, not a complete modal design:

```tsx
import {
    ConfirmationDialogRequest, DialogButtons, DialogResult,
    DialogComponents, useConfirmationDialog, useDialogContext
} from '@cratis/arc.react/dialogs';

function ConfirmationDialog() {
    const { request, closeDialog } = useDialogContext<ConfirmationDialogRequest>();
    const yesNo = request.buttons === DialogButtons.YesNo || request.buttons === DialogButtons.YesNoCancel;
    const cancel = request.buttons === DialogButtons.OkCancel || request.buttons === DialogButtons.YesNoCancel;
    return (
        <dialog open aria-label={request.title}>
            <p>{request.message}</p>
            {yesNo ? <>
                <button onClick={() => closeDialog(DialogResult.Yes)}>Yes</button>
                <button onClick={() => closeDialog(DialogResult.No)}>No</button>
            </> : <button onClick={() => closeDialog(DialogResult.Ok)}>OK</button>}
            {cancel && <button onClick={() => closeDialog(DialogResult.Cancelled)}>Cancel</button>}
        </dialog>
    );
}

function ConfirmButton() {
    const [showConfirmation] = useConfirmationDialog('Continue?', 'Use these settings?', DialogButtons.YesNoCancel);
    async function confirm() {
        const result = await showConfirmation();
        if (result === DialogResult.Yes) console.log('Confirmed');
    }
    return <button onClick={() => void confirm()}>Confirm settings</button>;
}

export const ConfirmationExample = () => (
    <DialogComponents confirmation={ConfirmationDialog}>
        <ConfirmButton />
    </DialogComponents>
);
```

`DialogComponents` supplies and mounts your shared confirmation renderer. Without a configured renderer there is no usable confirmation UI; Arc does not supply a visual library automatically.

## Busy indicator dialogs

Configure a `busyIndicator` renderer on `DialogComponents`. Inside it, read `useDialogContext<BusyIndicatorDialogRequest>()` for `request.title` and `request.message`.

`useBusyIndicator(title?, message?)` returns `[showBusyIndicator, closeBusyIndicator]`. Close it in `finally` after your application-owned operation, including failures. Busy UI is not cancellation or a guarantee that the operation succeeded.

Continue with [MVVM dialogs](../react.mvvm/dialogs.md) for request classes and the `IDialogs` service.
