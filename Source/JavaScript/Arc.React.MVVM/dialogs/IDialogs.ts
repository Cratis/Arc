// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DialogResponse, DialogResult } from '@cratis/arc.react/dialogs';
import { DialogButtons } from '@cratis/arc.react/dialogs';
import { BusyIndicator } from './BusyIndicator.js';

/**
 * Defines a service for working with dialogs from a view model.
 */
export abstract class IDialogs {
    /**
     * Show a dialog in the context of the current view and view model. This requires the view to host the dialog.
     * @param {*} input The input to pass to the dialog.
     * @returns {Promise<*>} The output from the dialog.
     */
    abstract show<TRequest extends object, TResponse = object>(input: TRequest): Promise<DialogResponse<TResponse>>;

    /**
     * Show a standard confirmation dialog.
     * @param {String} title Title of the dialog.
     * @param {String} message Message to show inside the dialog.
     * @param {DialogButtons} buttons Buttons to have on the dialog
     * @returns {Promise<DialogResult>} The result of the dialog.
     */
    abstract showConfirmation(title: string, message: string, buttons: DialogButtons): Promise<DialogResult>;

    /**
     * Show a standard busy indicator dialog.
     * @param {String} title Title of the dialog.
     * @param {String} message Message to show inside the dialog.
     * @returns {BusyIndicator} The busy indicator instance.
     */
    abstract showBusyIndicator(title: string, message: string): BusyIndicator;
}


