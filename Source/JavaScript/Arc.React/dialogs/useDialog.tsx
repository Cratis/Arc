// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DialogContext, DialogContextContent } from './DialogContext';
import { DialogResponse } from './DialogResponse';
import { DialogResult } from './DialogResult';
import { CloseDialog } from './CloseDialog';
import { useCallback, useRef, useState, ComponentType, FC, ReactElement, useMemo } from 'react';
import { ShowDialog } from './ShowDialog';

/**
 * Use a dialog component in your application. This hook manages the visibility and properties of the dialog.
 *
 * The dialog component only needs to declare the input properties it expects — typically as
 * `(props: TInput) => JSX.Element`. It reads `closeDialog` (and the request) from
 * {@link useDialogContext} rather than receiving them as explicit props, so no wrapper prop
 * type or `as unknown as ComponentType<TInput>` cast is required at the call site. `TInput` is
 * inferred directly from the component, and a component whose input type does not match the
 * requested one is rejected at compile time.
 * @typeParam TResponse The response type the dialog resolves with when it is closed.
 * @typeParam TInput The input properties the dialog component expects. Inferred from the component.
 * @param DialogComponent The dialog component to use.
 * @returns A tuple containing the wrapped dialog component, a function to show the dialog and the dialog context content.
 * The wrapped dialog component will receive the properties passed to it, excluding the `closeDialog` property.
 */
export function useDialog<TResponse = object, TInput extends object = object>(
    DialogComponent: ComponentType<TInput>
): [FC<TInput>, ShowDialog<TInput, TResponse>, DialogContextContent<TInput, TResponse>] {

    const [visible, setVisible] = useState(false);
    const [dialogProps, setDialogProps] = useState<TInput | undefined>();
    const resolverRef = useRef<((value: DialogResponse<TResponse>) => void) | undefined>(undefined);

    const showDialog = useCallback((p?: TInput) => {
        setDialogProps(p);
        setVisible(true);
        return new Promise<DialogResponse<TResponse>>((resolve) => {
            resolverRef.current = resolve;
        });
    }, []);

    const closeDialog = useCallback((result: DialogResult, value?: TResponse) => {
        setVisible(false);
        resolverRef.current?.([result, value]);
        resolverRef.current = undefined;
    }, []);

    const dialogContextValue = useRef<DialogContextContent<TInput, TResponse>>(undefined!);
    dialogContextValue.current = useMemo(() => {
        return new DialogContextContent(dialogProps!, closeDialog);
    }, [dialogProps, closeDialog]);

    // Capture the latest render in a ref so DialogWrapper's identity stays stable across renders
    // while still reflecting current state. Without this, every parent re-render produced a fresh
    // DialogWrapper function — React would treat each render as a new component type and unmount
    // the dialog subtree on every render, which caused PrimeReact (and other portal-based dialogs)
    // to leak portals or visibly remount on each parent update.
    //
    // The component only has to declare its input props (TInput); `closeDialog` is also provided
    // through the context above. We still spread it as a prop so components that read it directly
    // keep working, casting to allow the extra prop on a component whose props are exactly TInput.
    const RenderedComponent = DialogComponent as ComponentType<TInput & { closeDialog?: CloseDialog<TResponse> }>;
    const renderRef = useRef<(extraProps: TInput) => ReactElement | null>(() => null);
    renderRef.current = (extraProps: TInput) => visible
        ? (
            <DialogContext.Provider value={dialogContextValue.current as unknown as DialogContextContent<object, object>}>
                <RenderedComponent
                    {...extraProps}
                    {...(dialogProps as TInput)}
                    closeDialog={closeDialog} />
            </DialogContext.Provider>
        )
        : null;

    const DialogWrapper = useMemo<FC<TInput>>(() => {
        const Component: FC<TInput> = (extraProps) => renderRef.current(extraProps);
        Component.displayName = `DialogWrapper(${DialogComponent.displayName ?? DialogComponent.name ?? 'Anonymous'})`;
        return Component;
    }, [DialogComponent]);

    return [DialogWrapper, showDialog, dialogContextValue.current];
}
