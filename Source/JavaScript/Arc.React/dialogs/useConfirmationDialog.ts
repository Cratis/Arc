// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useContext } from 'react';
import { DialogButtons } from './DialogButtons.js';
import { DialogComponentsContext, IDialogComponents } from './DialogComponents.js';
import { ConfirmationDialogRequest } from './ConfirmationDialogRequest.js';
import { DialogResult } from './DialogResult.js';

/**
 * Represents the signature for showing a confirmation dialog.
 * @param title Optional title of the confirmation dialog.
 * @param message Optional message to display in the confirmation dialog.
 * @param buttons Optional buttons to display in the confirmation dialog. If not provided, defaults to `DialogButtons.Ok`. 
 * @return A promise that resolves to a tuple containing the dialog result and any additional data.
 */
export type ShowConfirmationDialog = (title?: string, message?: string, buttons?: DialogButtons) => Promise<DialogResult>;

/**
 * Uses a confirmation dialog in your application.
 * @param title Optional title of the confirmation dialog.
 * @param message Optional message to display in the confirmation dialog.
 * @param buttons Optional buttons to display in the confirmation dialog. If not provided, defaults to `DialogButtons.Ok`. 
 * @returns A tuple containing function to show the dialog.
 */
export const useConfirmationDialog = (title?: string, message?: string, buttons?: DialogButtons): [ShowConfirmationDialog] => {
    const components = useContext<IDialogComponents>(DialogComponentsContext);

    return [
        async (delegateTitle?: string, delegateMessage?: string, delegateButtons?: DialogButtons) => {
            const request = new ConfirmationDialogRequest(
                delegateTitle ?? title ?? '',
                delegateMessage ?? message ?? '',
                delegateButtons ?? buttons ?? DialogButtons.Ok);
            const [result] = await components.showConfirmation(request);
            return result;
        }
    ];
};