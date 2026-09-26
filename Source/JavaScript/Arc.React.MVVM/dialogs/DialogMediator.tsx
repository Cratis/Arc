// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { IDialogMediatorHandler } from './IDialogMediatorHandler.js';

/**
 * Context for the dialog mediator.
 */
export const DialogMediatorContext = React.createContext<IDialogMediatorHandler>(undefined!);

/**
 * Props for the dialog mediator.
 */
export interface DialogMediatorProps {
    /**
     * Children to provide the dialog mediator to.
     */
    children?: JSX.Element | JSX.Element[];

    /**
     * The dialog mediator context.
     */
    handler: IDialogMediatorHandler;

    /**
     * Parent handler, if any.
     */
    parentHandler?: IDialogMediatorHandler;
}

/**
 * Use the dialog mediator.
 * @returns The dialog mediator.
 */
export const useDialogMediator = () => {
    return React.useContext(DialogMediatorContext);
};

/**
 * Provide the dialog mediator to the children.
 * @param {DialogMediatorProps} props Props for the dialog mediator.
 * @returns Provider for the dialog mediator.
 */
export const DialogMediator = (props: DialogMediatorProps) => {
    return (
        <DialogMediatorContext.Provider value={props.handler}>
            {props.children}
        </DialogMediatorContext.Provider>
    );
};
