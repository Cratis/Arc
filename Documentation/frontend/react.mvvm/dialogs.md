---
title: Dialogs in MVVM
description: Request a dialog from a view model and handle its result/response tuple through a mounted view wrapper.
---

A view model can ask the user a question without rendering a modal. `IDialogs` sends a typed request; the view registers and renders the matching dialog. Configure [TSyringe and bindings](./tsyringe.md) and the router/MVVM context before using these examples.

## Custom dialogs

Use a **class** for the request because registration needs a runtime constructor; a TypeScript interface alone cannot identify a message type. The following files form a contract checkpoint beneath your existing Arc/MVVM host. The native nonmodal dialog is deliberately minimal; use an accessible modal renderer in a production application.

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

`FeatureViewModel.ts`:

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

`Feature.tsx`:

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

The MVVM `useDialog<TRequest, TResponse>(RequestType, Component)` returns `[Wrapper, showDialog]`, **not a context object**. This differs from the base React hook's three-element tuple and generic ordering. Register the wrapper in the view before the view model sends requests.

`IDialogs.show<TRequest, TResponse>()` resolves to `[DialogResult, response?]`. Do not compare the entire tuple with a string. `Cancelled` is the cancellation enum member; `None`, `Ok`, `Yes`, and `No` are also available.

## Confirmation dialogs

`IDialogs.showConfirmation(title, message, DialogButtons)` resolves directly to a `DialogResult`. This special convenience method does not return the custom-dialog tuple. Only perform a confirmed operation after explicitly matching the expected affirmative result.

Configure `DialogComponents confirmation={YourConfirmationDialog}` as described in [React dialogs](../react/dialogs.md#defining-the-confirmation-dialog). Handle all requested button sets, including the Cancel button for `YesNoCancel`.

## Busy indicator dialogs

`IDialogs.showBusyIndicator(title, message)` returns an object with `close()`. Configure the busy renderer on `DialogComponents`, and call `close()` in a `finally` block around the application operation. Rendering a busy dialog is not a transactional guarantee or cancellation mechanism.

## Dialog with a view model

A dialog can itself use `withViewModel(DialogViewModel, renderer)`. Inject `DialogContextContent<TRequest, TResponse>` into its view model; both type arguments are required. Read `.request` and call `.closeDialog(DialogResult.Ok, response)` or `.closeDialog(DialogResult.Cancelled)` there. `withViewModel` still needs **both** the constructor and renderer, just like the complete Feature example.

Use the [base React dialog contract](../react/dialogs.md) for visibility and request/response semantics, and [view-model lifecycle](./using-view-model.md) for cleanup.
