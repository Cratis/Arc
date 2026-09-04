// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, ComponentType, FC, useCallback } from 'react';
import { Constructor } from '@cratis/fundamentals';
import { DialogResult, ShowDialog, useDialog as useDialogBase } from '@cratis/arc.react/dialogs';
import { useDialogMediator } from './DialogMediator';

/**
 * Use a dialog request for showing a dialog, similar to useDialog.
 * @param requestType Type of request to use that represents a request that will be made by your view model.
 * @param DialogComponent The dialog component to render.
 * @returns A tuple with a component to use for rendering the dialog.
 */
export function useDialog<TProps extends object = object, TResponse = object>(
    requestType: Constructor<TProps>,
    DialogComponent: ComponentType<TProps>
): [FC<Partial<TProps>>, ShowDialog<TProps, TResponse>] {
    const mediator = useDialogMediator();
    const [DialogWrapper, showDialog, actualDialogContext] = useDialogBase<TResponse, TProps>(DialogComponent);

    const closeDialog = useCallback((result: DialogResult, value?: TResponse) => {
        actualDialogContext.closeDialog(result, value as TResponse);
    }, []);

    useEffect(() => {
        mediator.subscribe(requestType, async (request, resolver) => {
            const [result, response] = await showDialog(request as unknown as TProps);
            resolver(result, response);
        }, closeDialog);
    }, []);

    return [DialogWrapper, showDialog];
}
