// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandResult, type ICommandResult } from '@cratis/arc/commands';
import { a_command_form_context } from './a_command_form_context';

/**
 * An execution the spec decides the outcome of. Without one, the window in which the form is
 * executing closes before anything can observe it - the assertion would be racing the command.
 */
export class a_settleable_execution {
    readonly promise: Promise<ICommandResult<unknown>>;
    private _resolve!: (result: ICommandResult<unknown>) => void;
    private _reject!: (reason: unknown) => void;

    constructor() {
        this.promise = new Promise<ICommandResult<unknown>>((resolve, reject) => {
            this._resolve = resolve;
            this._reject = reject;
        });
    }

    succeed(result: ICommandResult<unknown>) {
        this._resolve(result);
    }

    fail(reason: unknown) {
        this._reject(reason);
    }
}

export class a_command_form_being_executed extends a_command_form_context {
    readonly executions: a_settleable_execution[] = [];

    /**
     * An execute implementation that hands out a fresh unsettled execution per call, recorded in
     * {@link executions} in call order, so overlapping calls can be settled independently.
     */
    executeDeferred = () => {
        const execution = new a_settleable_execution();
        this.executions.push(execution);
        return execution.promise;
    };

    /**
     * The suite context is constructed once for the whole suite, not once per example, so the
     * executions recorded by one example are still here for the next one. Call this first: without it
     * the next example settles the previous example's execution and waits forever on its own.
     */
    reset() {
        this.executions.length = 0;
    }

    successfulResult(): ICommandResult<unknown> {
        return new CommandResult({
            correlationId: 'b0d1e1b6-3b7b-4a1e-9a0c-2a5a5b7a91f1',
            isSuccess: true,
            isAuthorized: true,
            isValid: true,
            hasExceptions: false,
            validationResults: [],
            exceptionMessages: [],
            exceptionStackTrace: '',
            authorizationFailureReason: '',
            response: {}
        }, Object, false) as unknown as ICommandResult<unknown>;
    }
}
