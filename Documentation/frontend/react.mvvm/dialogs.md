# Dialogs

Working with dialogs is different when decoupling your code as you do with the MVVM paradigm.
Even though the view models are not responsible for rendering and should be blissfully unaware of how things gets rendered,
you do need at times to interact with the user.

Cratis Arc supports an approach to working with dialogs and still maintain the clear separation of concerns.
It promotes the idea of letting the view and React as a rendering library do just that and then bridges everything through a
service called `IDialogs` and the use of specific hooks to glue it together, making it feel natural for you as a React developer
whilst having a clear separation and making your view model logic clear and concise.

The beauty of this is that you can quite easily also write automated unit tests that test for the scenarios, involving dialogs.

## Confirmation Dialogs

A common use of modal dialogs are the standard confirmation dialogs. These are dialogs where you ask the user to confirm
a specific action. The Arc supports these out of the box and you have options for what type of confirmation you're
looking for in the form of passing it which buttons to show.

There is an enum called `DialogButtons` that has the following options:

| Value | Description |
| ----- | ----------- |
| Ok    | Only show a single Ok button, typically used to inform the user and the user to acknowledge |
| OkCancel | Show both an Ok and a Cancel button |
| YesNo | Show a Yes and No button |
| YesNoCancel | Show Yes, No and a Cancel button |

For standard confirmation dialogs, there is a specific expected result called `DialogResult` that the dialog needs to communicate back.
The values are:

* Yes
* No
* Ok
* Cancel

To use a confirmation dialog from a ViewModel, you need to take a dependency to the `IDialogs`, assuming you have hooked up [TSyringe and bindings](./tsyringe.md).
Then in a method, you can call the `showConfirmation()` on the `IDialogs` to show the confirmation.

Below is a full sample of how this works.

```ts
import { injectable } from 'tsyringe';
import { DialogResult } from '@cratis/arc.react/dialogs';
import { DialogButtons, IDialogs } from '@cratis/arc.react.mvvm/dialogs';

@injectable()
export class YourViewModel {
    constructor(
        private readonly _dialogs: IDialogs) {
    }

    // Method called from typically your view
    async deleteTheThing() {
        const result = await this._dialogs.showConfirmation('Delete?', 'Are you sure you want to delete?', DialogButtons.YesNo);
        if( result == DialogResult.Yes ) {
            // Do something - typically call your server
        }
    }
}
```

In order for the dialogs to show, you need to configure the component that represent it.
You can read more about [configuring dialogs](../react/dialogs.md).

## Busy indicator dialogs

Another common type of modal dialog is the indeterminate busy indicator dialog. You typically use these dialogs for giving
a visual clue to the user that the system is working. These type of dialogs are not meant to be something the user can
close, but rather something the system closes when it is ready with the work the system is doing.

To use a busy indicator dialog from a ViewModel, you need to take a dependency to the `IDialogs`, assuming you have hooked up [TSyringe and bindings](./tsyringe.md).
Then in a method, you can call the `showBusyIndicator()` on the `IDialogs` to show the confirmation.

Below is a full sample of how this works.

```ts
import { injectable } from 'tsyringe';
import { DialogResult } from '@cratis/arc.react/dialogs';
import { DialogButtons, IDialogs } from '@cratis/arc.react.mvvm/dialogs';

@injectable()
export class YourViewModel {
    constructor(
        private readonly _dialogs: IDialogs) {
    }

    // Method called from typically your view
    async performLongRunningOperation() {
        const busyIndicator = this._dialogs.showBusyIndicator('Performing something that will take a while', 'Please wait');
        setTimeout(() => {
            busyIndicator.close();
        }, 1000);
    }
}
```

The `showBusyIndicator()` returns an object that has a method called `close()`. This method is then something you use to close the dialog.

In order for the dialogs to show, you need to configure the component that represent it.
You can read more about that [configuring dialogs](../react/dialogs.md)

## Custom dialogs

The anatomy of dialogs in general is based on a **request** and **response** pattern.
You request a dialog through the `IDialogs` service by giving it an instance of a type of a message that the view knows
how to resolve into a dialog. This mechanism is in use on the confirmation dialogs and is the same for a custom dialog.

For the dialog to know the context in which it is rendering, there is a hook called `useDialogContext()`.
In the view where the dialog is used, you define the context implicitly by using the `useDialogRequest()`.
This establishes the **subscriber** that responds to a request from your view model of showing a dialog.

Subscriptions are based on type and it must be a well known type at runtime, so typically in TypeScript you'd define the
request as a class as `interface` and `type` is optimized away by the TypeScript transpiler and are not present at runtime.

The following code creates a custom dialog component.

```tsx
import { Button } from 'primereact/button';
import { Dialog } from 'primereact/dialog';
import { useDialogContext } from '@cratis/arc.react/dialogs';

export class CustomDialogRequest { 
    constructor(readonly content: string) {
    }
}

export const CustomDialog = () => {
    const { request, closeDialog } = useDialogContext<CustomDialogRequest, string>();

    return (
        <Dialog header="My custom dialog" visible={true} onHide={() => closeDialog(DialogResult.Cancelled, 'Did not do it..')}>
            <h2>Dialog</h2>
            {request.content}
            <br />
            <Button onClick={() => closeDialog(DialogResult.Ok, 'Done done done...')}>We're done</Button>
        </Dialog>
    );
};
```

Notice that the code creates a `CustomDialogRequest` class, it is defined as an immutable class with a constructor that
holds the properties as `readonly`. The purpose of the the request object is to provide information that can be passed along
from a view model to the dialog. This could for instance be data is needed to be displayed in the dialog or similar.
You don't need to have any properties on it, the type as a class is however required.

Within the dialog component, you use the `useDialogContext()` and pass it the request type and the expected response type.
The hook returns an object called `IDialogContext`, this holds the request and a delegate type that can be called to
close the dialog. Both properties are type-safe based on the generic parameters passed to the hook.

With the custom dialog defined, we can start using it.

Below is an example of a view that leverages the dialog and has a view model behind that actually shows it.

```tsx
import { withViewModel } from '@cratis/arc.react.mvvm';
import { FeatureViewModel } from './FeatureViewModel';
import { useDialogRequest } from '@cratis/arc.react.mvvm/dialogs';
import { CustomDialog, CustomDialogRequest } from './CustomDialog';

export const Feature = withViewModel<FeatureViewModel>(FeatureViewModel, ({ viewModel }) => {

    // Use the dialog request to get a wrapper for rendering our dialog
    const [CustomDialogWrapper] = useDialogRequest<CustomDialogRequest, string>(CustomDialogRequest, CustomDialog);

    return (
        <div>
            {/* Use the dialog wrapper here. It will automatically include the actual dialog and show your dialog.
            If using a component that represents the dialog and it has a property for visibility, just set it to true.
             */}
            <CustomDialogWrapper/>
        </div>
    );
});
```

The code leverages the `useDialogRequest()` with the generic parameters corresponding to the request and response types,
as you saw when defining the `CustomDialog` component. It returns a **tuple** that holds a wrapper as a React functional component,
then the context which holds the request when a request is made and a function to close the dialog. This allows for inlining dialogs or passing the information
on to things that needs it. But for this scenario, we don't need them and we therefor only capture the wrapper.

> Note: See the sample later on how to create dialogs with a view model for an example of context and `closeDialog` use.

With the wrapper, the code wraps the actual `CustomDialog` component as part of the rendering of the component. This ensures that
is will only be displayed when it is supposed to.

The last piece of the puzzle is now to use it from the view model. Following is a sample that shows the usage.

```ts
import { injectable } from 'tsyringe';
import { DialogButtons, IDialogs } from '@cratis/arc.react.mvvm/dialogs';
import { CustomDialogRequest } from './CustomDialogRequest;

@injectable()
export class FeatureViewModel {
    constructor(
        private readonly _dialogs: IDialogs) {
    }

    async doThings() {
        // Show the custom dialog
        const result = await this._dialogs.show<CustomDialogRequest, string>(new CustomDialogRequest('This is the content to show'));
        if( result == 'Done done done...') {
            // Do something
        }
    }
}
```

The view model takes a dependency to `IDialogs` which is resolved by the IoC, assuming you have hooked up [TSyringe and bindings](./tsyringe.md).

In the `doThings()` method we show the dialog by calling `.show()` on the `IDialogs` service, giving it an instance of the
`CustomDialogRequest`. With the generic arguments; `CustomDialogRequest` and `string` we are sure to get type-safety for the response.
If you don't provide any of the generic arguments, the return type will become `unknown`.

The `.show()` method is an async, `Promise` based method that will return when the dialog is resolved.
The return from the `.show()` method will then be the response type, in this case; **a string**.

### Dialog with a view model

You might want to use a view model for the dialog itself. That is fully possible and recommended for scenarios where there will be logic
and / or you want to be able to test the dialog logic code.

Your dialog view would then look like below:

```tsx
import { Button } from 'primereact/button';
import { Dialog } from 'primereact/dialog';
import { useDialogContext, CloseDialog } from '@cratis/arc.react/dialogs';
import { withViewModel } from '@cratis/arc.react.mvvm';
import { CustomDialogViewModel } from './CustomDialogViewModel';

export class CustomDialogRequest { 
    constructor(readonly content: string) {
    }
}

// Use the withViewModel() to pull in and specify props
export const CustomDialog = withViewModel<CustomDialogViewModel>({ viewModel }) => {
    return (
        <Dialog header="My custom dialog" visible={true} onHide={() => viewModel.cancel() }>
            <h2>Dialog</h2>
            {request.content}
            <br />
            <Button onClick={() => viewModel.done() }>We're done</Button>
        </Dialog>
    );
};
```

The above code is not different for the component except that it uses the `withViewModel()` which points it
to the view model type.

Your view model can then be something like the following:

```ts
import { inject, injectable } from 'tsyringe';
import { DialogContextContent, DialogResult } from '@cratis/arc.react/dialogs';

@injectable()
export class CustomDialogViewModel {

    constructor(private readonly _dialogContext: DialogContextContent) {
    }

    name: string = '';

    done() {
        this._dialogContext.closeDialog(DialogResult.Ok, 'Done done done...');
    }

    cancel() {
        this._dialogContext.closeDialog(DialogResult.Cancelled, 'Did not do it..');
    }
}
```

The **view model** now takes the `DialogContextContent` as a dependency. This will be the correct
context for the current dialog and contains the `closeDialog` function that can be called for
closing the dialog in addition to the request instance.

Using the dialog is exactly the same as before.
