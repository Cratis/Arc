// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Constructor } from '@cratis/fundamentals';
import { CloseDialog, DialogResponse } from '@cratis/arc.react/dialogs';
import { DialogRegistration, DialogRequest } from './DialogRegistration.js';

/**
 * Defines a system that can handle dialog requests and responses.
 */
export abstract class IDialogMediatorHandler {

    /**
     * Subscribes to a request type.
     * @param {Constructor} requestType Type of request.
     * @param {DialogRequest} requester The delegate that will be called when a request is made.
     * @param {CloseDialog} closeDialog The delegate that will be called when dialog is typically closed and response is resolved.
     */
    abstract subscribe<TRequest extends object, TResponse>(requestType: Constructor<TRequest>, requester: DialogRequest<TRequest, TResponse>, closeDialog: CloseDialog<TResponse>): void;

    /**
     * Check if there is a subscriber for a given request type.
     * @param {Constructor} requestType Type of request.
     * @returns {boolean} True if there is a subscriber, false otherwise.
     */
    abstract hasSubscriber<TRequest extends object>(requestType: Constructor<TRequest>): boolean;

    /**
     * Show a dialog based on a request.
     * @param {*} request An instance of the dialog request.
     */
    abstract show<TRequest extends object, TResponse>(request: TRequest): Promise<DialogResponse<TResponse>>;

    /**
     * Get the registration for a given request type.
     * @param {Constructor} requestType Type of request.
     * @returns {DialogRegistration} The registration for the request type.
     */
    abstract getRegistration<TRequest extends object, TResponse>(requestType: Constructor<TRequest>): DialogRegistration<TRequest, TResponse>;
}
