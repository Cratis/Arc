---
title: Cratis Arc React dialogs and user decisions
description: Use Cratis Arc React dialogs to await a user's decision, read optional response data and keep dialog rendering in your chosen component library.
---

Cratis Arc React dialogs let a caller open a dialog and await the user's decision without managing its visibility. You supply the UI and close it with a result and optional response data. Use your own renderer or a component library; Arc does not require Components or PrimeReact.

## Custom dialogs

Start with a dialog that asks the user to accept a name. This example uses native `<dialog open>`, which is nonmodal: it does not block interaction with the rest of the page. For a modal dialog, choose an accessible renderer that manages focus, dismissal and labeling, such as [Components dialogs](/components/).

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

`useDialog<TResponse, TInput>(Component)` returns `[Wrapper, showDialog, context]`. Render the wrapper as `Feature` does above; calling `showDialog` alone cannot display the dialog. The wrapper mounts the dialog while it is visible.

`showDialog(input)` passes the request to the dialog and waits for `closeDialog(result, response?)`. Its promise resolves to `[DialogResult, response?]`, so the caller checks both the result and the optional value before using the name. Canceling can return no response. Open only one request at a time per hook: it stores a single pending resolver.

Inside the dialog, read input through props or `useDialogContext<TInput, TResponse>()`, which also supplies `closeDialog`. The type parameters are reversed from `useDialog`: input comes first in the context hook.

## Dialogs and slice scope

A shared dialog renderer may live far from the feature that opened it. `useDialog` captures the caller's slice scope and gives the dialog a child scope, so `useSliceContext()` inside the dialog starts with the owning feature's location. Commands and queries in the dialog refine that location just as they do on a page.

```tsx
import { useDialog, useDialogSliceLocation } from '@cratis/arc.react/dialogs';

const EditOrderDialog = () => {
    const location = useDialogSliceLocation();     // Orders/Editing/ChangeOrder
    return <header>Edit order <small>{location.toString()}</small></header>;
};

const ChangeOrder = () => {
    const [EditOrder, showEditOrder] = useDialog(EditOrderDialog, { title: 'Edit order' });
    return <><button onClick={() => showEditOrder()}>Edit</button><EditOrder /></>;
};
```

A dialog can declare its own location, which wins over the inherited one:

```tsx
useDialog(AddressDialog, { slice: { module: 'Orders', feature: 'Shipping', slice: 'Address' } });
```

`useDialog` takes these options as a second argument:

| Option | Description |
| --- | --- |
| `slice` | An explicit location for the dialog. Parts not given are empty. |
| `title` | The title reported in the [application context](./application-context.md). |
| `trackInApplicationContext` | Set to `false` to keep the open dialog out of the application context. Defaults to `true`. The built-in confirmation and busy indicator dialogs of `DialogComponents` do not track. |

Tracked dialogs appear in `useApplicationContext().dialogs`, bottom first. The application's focused `location` follows the top-most dialog; closing it restores the dialog below or the page. Changing an open dialog's title or slice updates its entry without changing the stack order.

For a shared dialog header, use `useDialogSliceLocation()` to read the containing dialog's location directly. Outside a dialog it returns `SliceLocation.empty`.

When `sliceMarkers` is enabled on `<Arc>`, the dialog content is wrapped in the marker element too. Renderers that portal the content elsewhere can use `sliceMarkerAttributes(useDialogSliceLocation())` on their own element.

## Confirmation dialogs

`DialogButtons` supports `Ok`, `OkCancel`, `YesNo`, and `YesNoCancel`. `DialogResult` supports `None`, `Ok`, `Yes`, `No`, and **`Cancelled`**, not `Cancel`.

`useConfirmationDialog(title?, message?, buttons?)` returns `[showConfirmation]`. Call `showConfirmation()` to use those defaults, or pass overrides for that request. Its promise resolves to `DialogResult` alone, so there is no response tuple to unpack.

### Defining the confirmation dialog

Configure one shared confirmation renderer with `DialogComponents`. This example renders each button configuration using the same nonmodal native presentation as the name dialog:

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

`DialogComponents` mounts the renderer and supplies its request. Place it above components that call `useConfirmationDialog`; without a configured renderer, the caller has no usable confirmation UI.

## Busy indicator dialogs

Configure a `busyIndicator` renderer on `DialogComponents`. Inside it, read `useDialogContext<BusyIndicatorDialogRequest>()` for `request.title` and `request.message`.

`useBusyIndicator(title?, message?)` returns `[showBusyIndicator, closeBusyIndicator]`. Your application still owns the operation. Close the indicator in `finally` so a failed operation does not leave it visible; closing the indicator neither cancels the work nor proves it succeeded.

If your application uses view models, continue with [MVVM dialogs](../react/mvvm/dialogs.md) to open dialogs through request classes and the `IDialogs` service.
