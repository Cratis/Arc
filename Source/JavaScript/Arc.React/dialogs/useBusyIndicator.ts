// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useContext } from 'react';
import { DialogComponentsContext, IDialogComponents } from './DialogComponents.js';
import { DialogResult } from './DialogResult.js';
import { BusyIndicatorDialogRequest } from './BusyIndicatorDialogRequest.js';

/**
 * Represents the signature for showing a confirmation dialog.
 * @param title Optional title of the confirmation dialog.
 * @param message Optional message to display in the confirmation dialog.
 * @return A promise that resolves to a tuple containing the dialog result and any additional data.
 */
export type ShowBusyIndicatorDialog = (title?: string, message?: string) => Promise<DialogResult>;

/**
 * Represents the signature for closing a busy indicator dialog.
 */
export type CloseBusyIndicatorDialog = () => void;

/**
 * Uses a busy indicator dialog in your application.
 * @param title Optional title of the confirmation dialog.
 * @param message Optional message to display in the confirmation dialog.
 * @returns A tuple containing the a function to show the dialog and one to close the dialog.
 */
export const useBusyIndicator = (title?: string, message?: string): [ShowBusyIndicatorDialog, CloseBusyIndicatorDialog] => {
    const components = useContext<IDialogComponents>(DialogComponentsContext);

    return [
        async (delegateTitle?: string, delegateMessage?: string) => {
            const request = new BusyIndicatorDialogRequest(
                delegateTitle ?? title ?? '',
                delegateMessage ?? message ?? '');
            const [result] = await components.showBusyIndicator(request);
            return result;
        },
        () => components.closeBusyIndicator?.(DialogResult.Cancelled)
    ];
};