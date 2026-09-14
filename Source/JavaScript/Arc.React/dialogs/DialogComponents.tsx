// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { DialogResult } from './DialogResult';
import { ShowDialog } from './ShowDialog';
import { useDialog } from './useDialog';
import { ConfirmationDialogRequest } from './ConfirmationDialogRequest';
import { BusyIndicatorDialogRequest } from './BusyIndicatorDialogRequest';
import { DialogButtons } from './DialogButtons';
import { CloseDialog } from './CloseDialog';

/**
 * Defines the interface representing the context of dialog components.
 */
export interface IDialogComponents {

    /**
     * The confirmation dialog component.
     */
    confirmation?: React.FC<Partial<ConfirmationDialogRequest>>;

    /**
     * Shows a confirmation dialog with the specified request.
     */
    showConfirmation: ShowDialog<ConfirmationDialogRequest>;

    /**
     * The busy indicator dialog component, typically used for spinners.
     */
    busyIndicator?: React.FC<Partial<BusyIndicatorDialogRequest>>;

    /**
     * Shows a busy indicator dialog with the specified request.
     */
    showBusyIndicator: ShowDialog<BusyIndicatorDialogRequest>;

    /**
     * Closes the busy indicator dialog.
     */
    closeBusyIndicator: CloseDialog<object>;
}

/**
 * The context for dialog components.
 */
export const DialogComponentsContext = React.createContext<IDialogComponents>({
    showConfirmation: () => Promise.resolve([DialogResult.Cancelled, {}]),
    showBusyIndicator: () => Promise.resolve([DialogResult.Cancelled, {}]),
    /* eslint-disable-next-line @typescript-eslint/no-empty-function */
    closeBusyIndicator: () => { }
});

/**
 * Props for the DialogComponentsWrapper component.
 */
export interface DialogComponentsProps {
    /**
     * Optional children elements to render within the dialog components context.
     */
    children?: JSX.Element | JSX.Element[];

    /**
     * Optional confirmation dialog component to use.
     * If not provided, a default empty fragment will be used.
     */
    confirmation?: React.FC<ConfirmationDialogRequest>;

    /**
     * Optional busy indicator dialog component to use.
     * If not provided, a default empty fragment will be used.
     */
    busyIndicator?: React.FC<BusyIndicatorDialogRequest>;
}

const DialogComponentsWrapper = (props: DialogComponentsProps) => {
    const [Confirmation, showConfirmation] = useDialog(props.confirmation ?? React.Fragment as React.FC<ConfirmationDialogRequest>);
    const [BusyIndicator, showBusyIndicator, closeBusyIndicatorDialogContext] = useDialog(props.busyIndicator ?? React.Fragment as React.FC<BusyIndicatorDialogRequest>);

    const configuration: IDialogComponents = {
        confirmation: Confirmation,
        showConfirmation,
        busyIndicator: BusyIndicator,
        showBusyIndicator,
        closeBusyIndicator: closeBusyIndicatorDialogContext.closeDialog
    };

    return (
        <DialogComponentsContext.Provider value={configuration}>
            <>
                {props.children}
                {props.confirmation &&
                    <Confirmation title='' message='' buttons={DialogButtons.Ok} />}

                {props.busyIndicator &&
                    <BusyIndicator title='' message='' />}
            </>
        </DialogComponentsContext.Provider>
    );
};

/**
 * A React component that provides a context for dialog components.
 * @param props The properties for the dialog components wrapper.
 * @returns A React component that provides the dialog components context.
 */
export const DialogComponents = (props: DialogComponentsProps) => {
    return (
        <DialogComponentsWrapper {...props}>
            {props.children}
        </DialogComponentsWrapper>
    );
};
