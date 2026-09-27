// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as React from 'react';
import { useContext } from 'react';
import { CloseDialog } from './CloseDialog.js';

/**
 * Represents the content of the dialog context, including the request and a function to close the dialog.
 */
export class DialogContextContent<TRequest, TResponse> {

    /**
     * Initializes a new instance of {@link DialogContextContent}.
     * @param request The request for the dialog, which contains the data needed to display the dialog. 
     * @param closeDialog A function to close the dialog, which takes a result and an optional response.
     */
    constructor(readonly request: TRequest, readonly closeDialog: CloseDialog<TResponse>) {
    }
}

/**
 * The context for dialog components, providing access to the current dialog request and a method to close the dialog.
 */
export const DialogContext = React.createContext<DialogContextContent<object, object>>(undefined!);

/**
 * A custom hook to access the dialog context in your components.
 * @returns The current dialog context, which includes the request and a method to close the dialog.
 */
export const useDialogContext = <TRequest = object, TResponse = object>(): DialogContextContent<TRequest, TResponse> => {
    return useContext(DialogContext) as unknown as DialogContextContent<TRequest, TResponse>;
};
