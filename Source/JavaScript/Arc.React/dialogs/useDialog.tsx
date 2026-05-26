// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DialogContext, DialogContextContent } from './DialogContext';
import { DialogResponse } from './DialogResponse';
import { DialogResult } from './DialogResult';
import { useCallback, useRef, useState, ComponentType, FC, ReactElement, useMemo } from 'react';
import { ShowDialog } from './ShowDialog';

/**
 * Use a dialog component in you application. This hook manages the visibility and properties of the dialog.
 * @param DialogComponent The dialog component to use.
 * @returns A tuple containing the wrapped dialog component and a function to show the dialog.
 * The wrapped dialog component will receive the properties passed to it, excluding the `closeDialog` property.
 */
export function useDialog<TResponse = object, TProps = object>(
    DialogComponent: ComponentType<TProps>
): [FC<TProps>, ShowDialog<TProps, TResponse>, DialogContextContent<TProps, TResponse>] {

    const [visible, setVisible] = useState(false);
    const [dialogProps, setDialogProps] = useState<TProps | undefined>();
    const resolverRef = useRef<((value: DialogResponse<TResponse>) => void) | undefined>(undefined);

    const showDialog = useCallback((p?: TProps) => {
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

    const dialogContextValue = useRef<DialogContextContent<TProps, TResponse>>(undefined!);
    dialogContextValue.current = useMemo(() => {
        return new DialogContextContent(dialogProps!, closeDialog);
    }, [dialogProps, closeDialog]);

    // Capture the latest render in a ref so DialogWrapper's identity stays stable across renders
    // while still reflecting current state. Without this, every parent re-render produced a fresh
    // DialogWrapper function — React would treat each render as a new component type and unmount
    // the dialog subtree on every render, which caused PrimeReact (and other portal-based dialogs)
    // to leak portals or visibly remount on each parent update.
    const renderRef = useRef<(extraProps: TProps) => ReactElement | null>(() => null);
    renderRef.current = (extraProps: TProps) => visible
        ? (
            <DialogContext.Provider value={dialogContextValue.current as unknown as DialogContextContent<object, object>}>
                <DialogComponent
                    {...extraProps}
                    {...(dialogProps as TProps)}
                    closeDialog={closeDialog} />
            </DialogContext.Provider>
        )
        : null;

    const DialogWrapper = useMemo<FC<TProps>>(() => {
        const Component: FC<TProps> = (extraProps) => renderRef.current(extraProps);
        Component.displayName = `DialogWrapper(${DialogComponent.displayName ?? DialogComponent.name ?? 'Anonymous'})`;
        return Component;
    }, [DialogComponent]);

    return [DialogWrapper, showDialog, dialogContextValue.current];
}
