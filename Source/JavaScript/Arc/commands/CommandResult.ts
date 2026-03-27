// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Guid } from '@cratis/fundamentals';
import { ICommandResult } from './ICommandResult';
import { ValidationResult } from '../validation/ValidationResult';
import { Constructor, JsonSerializer } from '@cratis/fundamentals';

/**
 * Delegate type for the onSuccess callback.
 */
type OnSuccess = (<TResponse>(response: TResponse) => void) | (() => void);

/**
 * Delegate type for the onFailed callback.
 */
type OnFailed<TResponse> = ((commandResult: CommandResult<TResponse>) => void) | (() => void);

/**
 * Delegate type for the onException callback.
 */
type OnException = (messages: string[], stackTrace: string) => void;

/**
 * Delegate type for the onUnauthorized callback.
 */
type OnUnauthorized = () => void;

/**
 * Delegate type for the onValidationFailure callback.
 */
type OnValidationFailure = (validationResults: ValidationResult[]) => void;

type ServerCommandResult = {
    correlationId: string;
    isSuccess: boolean;
    isAuthorized: boolean;
    isValid: boolean;
    hasExceptions: boolean;
    validationResults: {
        severity: number;
        message: string;
        members: string[];
        state: object;
    }[];
    exceptionMessages: string[];
    exceptionStackTrace: string;
    authorizationFailureReason: string;

    /* eslint-disable @typescript-eslint/no-explicit-any */
    response: any;
    /* eslint-enable @typescript-eslint/no-explicit-any */
}

/**
 * Represents the result from executing a {@link ICommand}.
 */
export class CommandResult<TResponse = object> implements ICommandResult<TResponse> {

    static empty: CommandResult = new CommandResult({
        correlationId: Guid.empty.toString(),
        isSuccess: true,
        isAuthorized: true,
        isValid: true,
        hasExceptions: false,
        validationResults: [],
        exceptionMessages: [],
        exceptionStackTrace: '',
        authorizationFailureReason: '',
        response: null
    }, Object, false);

    static failed = (exceptionMessages: string[]): CommandResult => {
        return new CommandResult({
            correlationId: Guid.empty.toString(),
            isSuccess: false,
            isAuthorized: true,
            isValid: true,
            hasExceptions: true,
            validationResults: [],
            exceptionMessages: exceptionMessages,
            exceptionStackTrace: '',
            authorizationFailureReason: '',
            response: null
        }, Object, false);
    };

    static validationFailed = (validationResults: ValidationResult[]): CommandResult => {
        return new CommandResult({
            correlationId: Guid.empty.toString(),
            isSuccess: false,
            isAuthorized: true,
            isValid: false,
            hasExceptions: false,
            validationResults: validationResults.map(_ => ({
                severity: _.severity,
                message: _.message,
                members: _.members,
                state: _.state
            })),
            exceptionMessages: [],
            exceptionStackTrace: '',
            authorizationFailureReason: '',
            response: null
        }, Object, false);
    };

    /** @inheritdoc */
    readonly correlationId: Guid;

    /** @inheritdoc */
    readonly isSuccess: boolean;

    /** @inheritdoc */
    readonly isAuthorized: boolean;

    /** @inheritdoc */
    readonly isValid: boolean;

    /** @inheritdoc */
    readonly hasExceptions: boolean;

    /** @inheritdoc */
    readonly validationResults: ValidationResult[];

    /** @inheritdoc */
    readonly exceptionMessages: string[];

    /** @inheritdoc */
    readonly exceptionStackTrace: string;

    /** @inheritdoc */
    readonly authorizationFailureReason: string;

    /** @inheritdoc */
    readonly response?: TResponse;

    /**
     * Creates an instance of command result.
     * @param {*} result The JSON/any representation of the command result;
     * @param {Constructor} responseInstanceType The {@see Constructor} that represents the type of response, if any. Defaults to {@see Object}.
     * @param {boolean} isResponseTypeEnumerable Whether or not the response type is an enumerable or not.
     */
    constructor(result: ServerCommandResult, responseInstanceType: Constructor = Object, isResponseTypeEnumerable: boolean) {
        this.correlationId = Guid.parse(result.correlationId);
        this.isSuccess = result.isSuccess;
        this.isAuthorized = result.isAuthorized;
        this.isValid = result.isValid;
        this.hasExceptions = result.hasExceptions;
        this.validationResults = result.validationResults.map(_ => new ValidationResult(_.severity, _.message, _.members, _.state));
        this.exceptionMessages = result.exceptionMessages;
        this.exceptionStackTrace = result.exceptionStackTrace;
        this.authorizationFailureReason = result.authorizationFailureReason;

        if (result.response) {
            if (isResponseTypeEnumerable) {
                this.response = JsonSerializer.deserializeArrayFromInstance(responseInstanceType, result.response) as TResponse;
            } else {
                this.response = JsonSerializer.deserializeFromInstance(responseInstanceType, result.response) as TResponse;
            }
        }
    }

    /**
     * Set up a callback for when the command was successful.
     * @param {OnSuccess} callback The callback to call when the command was successful.
     * @returns {CommandResult} The instance of the command result.
     */
    onSuccess(callback: OnSuccess): CommandResult<TResponse> {
        if (this.isSuccess) {
            callback(this.response as TResponse);
        }
        return this;
    }

    /**
     * Set up a callback for when the command failed.
     * @param {OnFailed} callback The callback to call when the command failed.
     * @returns {CommandResult} The instance of the command result.
     */
    onFailed(callback: OnFailed<TResponse>): CommandResult<TResponse> {
        if (!this.isSuccess) {
            callback(this);
        }
        return this;
    }

    /**
     * Set up a callback for when the command failed with an exception.
     * @param {OnException} callback The callback to call when the command had an exception.
     * @returns {CommandResult} The instance of the command result.
     */
    onException(callback: OnException): CommandResult<TResponse> {
        if (this.hasExceptions) {
            callback(this.exceptionMessages, this.exceptionStackTrace);
        }
        return this;
    }

    /**
     * Set up a callback for when the command was unauthorized.
     * @param {OnUnauthorized} callback The callback to call when the command was unauthorized.
     * @returns {CommandResult} The instance of the command result.
     */
    onUnauthorized(callback: OnUnauthorized): CommandResult<TResponse> {
        if (!this.isAuthorized) {
            callback();
        }
        return this;
    }

    /**
     * Set up a callback for when the command had validation errors.
     * @param {OnSuccess} callback The callback to call when the command was invalid.
     * @returns {CommandResult} The instance of the command result.
     */
    onValidationFailure(callback: OnValidationFailure): CommandResult<TResponse> {
        if (!this.isValid) {
            callback(this.validationResults);
        }
        return this;
    }
}
