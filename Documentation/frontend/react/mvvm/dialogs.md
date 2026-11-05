---
title: Cratis Arc MVVM dialog requests
description: Request a dialog from a Cratis Arc view model, host it in the view, and handle confirmation, cancellation, and the returned response.
---

Cratis Arc's `IDialogs` lets a view model ask for user input and await the answer without rendering the dialog itself. The view registers a request class and hosts the matching component. Use this split when an operation needs a user's decision before the view model can continue.

Configure [TSyringe metadata](./tsyringe.md) and the [MVVM/router host](./mvvm-context.md) first. `withViewModel()` supplies an `IDialogs` instance for its view; you do not register a global dialog service.

## Custom dialogs

To let a user confirm a name, define the request, handle it in a dialog component, then host that component in the feature view. Use a class for `NameRequest`: registration matches its runtime constructor, which a TypeScript interface cannot provide.

The following three files belong beneath your existing Arc/MVVM host. The native dialog here is nonmodal and only demonstrates the request and response. Use an accessible modal renderer when the task requires modal interaction.

`NameDialog.tsx`:

```tsx
import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';

export class NameRequest {
    constructor(readonly name: string) {}
}

export function NameDialog({ name }: NameRequest) {
    const { closeDialog } = useDialogContext<NameRequest, string>();
    return (
        <dialog open aria-label="Confirm name">
            <p>{name}</p>
            <button onClick={() => closeDialog(DialogResult.Ok, name)}>Use name</button>
            <button onClick={() => closeDialog(DialogResult.Cancelled)}>Cancel</button>
        </dialog>
    );
}
```

In `FeatureViewModel.ts`, await `show()` and update the selection only after an affirmative result. Cancelling leaves the existing selection alone:

```ts
import { injectable } from 'tsyringe';
import { DialogResult } from '@cratis/arc.react/dialogs';
import { IDialogs } from '@cratis/arc.react.mvvm/dialogs';
import { NameRequest } from './NameDialog';

@injectable()
export class FeatureViewModel {
    selectedName = '';

    constructor(private readonly _dialogs: IDialogs) {}

    async chooseName() {
        const [result, response] = await this._dialogs.show<NameRequest, string>(new NameRequest('Ada'));
        if (result === DialogResult.Ok && response !== undefined) {
            this.selectedName = response;
        }
    }
}
```

In `Feature.tsx`, register `NameRequest` with the MVVM `useDialog` hook and render the returned wrapper. The wrapper stays mounted with the feature and renders the dialog when a request arrives:

```tsx
import { withViewModel } from '@cratis/arc.react.mvvm';
import { useDialog } from '@cratis/arc.react.mvvm/dialogs';
import { FeatureViewModel } from './FeatureViewModel';
import { NameDialog, NameRequest } from './NameDialog';

export const Feature = withViewModel(FeatureViewModel, ({ viewModel }) => {
    const [NameWrapper] = useDialog<NameRequest, string>(NameRequest, NameDialog);
    return (
        <>
            <button onClick={() => void viewModel.chooseName()}>Choose name</button>
            <p>{viewModel.selectedName}</p>
            <NameWrapper name="" />
        </>
    );
});
```

After the feature mounts, select “Choose name”, then “Use name” to display `Ada`. Cancel instead to leave `selectedName` unchanged. Register and mount the wrapper before sending requests; a request with no matching registration in the current view or its ancestors rejects with `No registration found for request`.

The MVVM `useDialog<TRequest, TResponse>(RequestType, Component)` returns `[Wrapper, showDialog]`, not a context object. It differs from the base React hook, which returns three elements and takes its generic types in a different order. Use the MVVM import shown above when connecting requests from a view model.

`IDialogs.show<TRequest, TResponse>()` resolves to `[DialogResult, response?]`. Destructure the tuple and inspect the enum result before using the response, which can be absent on cancellation. `Cancelled` is the cancellation member; `None`, `Ok`, `Yes`, and `No` are also available.

Keep the dialog host mounted until it responds, and close it explicitly through `closeDialog`. Unmounting the host does not resolve an outstanding request as cancelled.

## Confirmation dialogs

For a standard decision without a custom response, configure `DialogComponents confirmation={YourConfirmationDialog}` around the feature as described in [React dialogs](../../react/dialogs.md#defining-the-confirmation-dialog). Provide the renderer before calling the service; MVVM does not supply visible confirmation UI. The renderer must handle every requested button set, including Cancel for `YesNoCancel`.

`IDialogs.showConfirmation(title, message, DialogButtons)` resolves directly to a `DialogResult`, unlike the custom-dialog tuple. Perform the operation only after matching the affirmative result for the button set you requested.

## Busy indicator dialogs

Configure the busy renderer on `DialogComponents`, then call `IDialogs.showBusyIndicator(title, message)` when an operation begins. The returned object has a `close()` method. Call it in a `finally` block so the indicator closes whether the operation succeeds or fails. Closing the indicator does not cancel the operation or change its outcome.

## Dialog with a view model

When the dialog has behavior of its own, wrap it with `withViewModel(DialogViewModel, renderer)`. Inject `DialogContextContent<TRequest, TResponse>` from `@cratis/arc.react/dialogs` into that view model, with both generic arguments. The context belongs to the rendered dialog: read `.request` for its input and call `.closeDialog(DialogResult.Ok, response)` or `.closeDialog(DialogResult.Cancelled)` to finish the request. Supply both the constructor and renderer to `withViewModel`, as in the feature example.

Continue with the [base React dialog contract](../../react/dialogs.md) for visibility and request/response behavior. If the dialog view model owns subscriptions, follow the [view model lifecycle](./using-view-model.md) for disposal, including the limit on cleanup with `<Arc development>`.
