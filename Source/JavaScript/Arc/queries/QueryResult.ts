// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Constructor } from '@cratis/fundamentals';
import { ValidationResult } from '../validation/ValidationResult';
import { IQueryResult } from './IQueryResult';
import { PagingInfo } from './PagingInfo';
import { ChangeSet } from './ChangeSet';
import { deserializeQueryModel, deserializeQueryModels } from './deserializeQueryModel';

type ServerQueryResult = {
    /* eslint-disable @typescript-eslint/no-explicit-any */
    data: any;
    /* eslint-enable @typescript-eslint/no-explicit-any */
    isSuccess: boolean;
    isAuthorized: boolean;
    isValid: boolean;
    hasExceptions: boolean;
    validationResults: ServerValidationResult[];
    exceptionMessages: string[];
    exceptionStackTrace: string;
    paging: ServerPagingInfo;
    changeSet?: ServerChangeSet;
}

type ServerChangeSet = {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    added: any[];
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    replaced: any[];
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    removed: any[];
}

type ServerValidationResult = {
    severity: number;
    message: string;
    members: string[];
    state: object;
}

type ServerPagingInfo = {
    page: number;
    size: number; 
    totalItems: number;
    totalPages: number;
}

/**
 * Represents the result from executing a {@link IQueryFor}.
 * @template TDataType The data type.
 */
export class QueryResult<TDataType = object> implements IQueryResult<TDataType> {

    static empty<TDataType>(defaultValue: TDataType): QueryResult<TDataType> {
        return new QueryResult({
            data: defaultValue as object,
            isSuccess: true,
            isAuthorized: true,
            isValid: true,
            hasExceptions: false,
            validationResults: [],
            exceptionMessages: [],
            exceptionStackTrace: '',
            paging: {
                totalItems: 0,
                totalPages: 0,
                page: 0,
                size: 0
            }

        }, Object, false);
    }

    /**
     * Creates a {@link QueryResult} representing a query rejected by client-side validation, mirroring
     * {@link CommandResult.validationFailed} so a caller reads a failed query the same way it reads a failed command.
     * @param {ValidationResult[]} validationResults The validation results that caused the failure.
     * @param {object} query The query being rejected, which describes how to shape its own result.
     * @returns {QueryResult<TDataType>} A result that is neither successful nor valid.
     */
    static validationFailed<TDataType>(
        validationResults: ValidationResult[],
        query: { readonly defaultValue: TDataType; readonly modelType: Constructor; readonly enumerable: boolean }): QueryResult<TDataType> {
        const { defaultValue, modelType, enumerable } = query;
        return new QueryResult({
            data: defaultValue as object,
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
            paging: {
                totalItems: 0,
                totalPages: 0,
                page: 0,
                size: 0
            }
        }, modelType, enumerable) as QueryResult<TDataType>;
    }

    static unauthorized<TDataType>(): QueryResult<TDataType> {
        return new QueryResult({
            data: null as unknown as object,
            isSuccess: false,
            isAuthorized: false,
            isValid: true,
            hasExceptions: false,
            validationResults: [],
            exceptionMessages: [],
            exceptionStackTrace: '',
            paging: {
                totalItems: 0,
                totalPages: 0,
                page: 0,
                size: 0
            }
        }, Object, false);
    }

    static noSuccess: QueryResult = new QueryResult({
        data: {},
        isSuccess: false,
        isAuthorized: true,
        isValid: true,
        hasExceptions: false,
        validationResults: [],
        exceptionMessages: [],
        exceptionStackTrace: '',
        paging: {
            totalItems: 0,
            totalPages: 0,
            page: 0,
            size: 0
        }
    }, Object, false);

    /**
     * Creates an instance of query result.
     * @param {*} result The raw result from the backend.
     * @param {Constructor} instanceType The type of instance to deserialize.
     * @param {boolean} enumerable Whether or not the result is supposed be an enumerable or not.
     */
    constructor(result: ServerQueryResult, instanceType: Constructor, enumerable: boolean) {
        this.isSuccess = result.isSuccess;
        this.isAuthorized = result.isAuthorized;
        this.isValid = result.isValid;
        this.hasExceptions = result.hasExceptions;
        this.validationResults = result.validationResults.map(_ => new ValidationResult(_.severity, _.message, _.members, _.state));
        this.exceptionMessages = result.exceptionMessages;
        this.exceptionStackTrace = result.exceptionStackTrace;
        this.paging = new PagingInfo();
        this.paging.page = result.paging.page;
        this.paging.size = result.paging.size;
        this.paging.totalItems = result.paging.totalItems;
        this.paging.totalPages = result.paging.totalPages;

        if (result.data) {
            this.data = (enumerable
                ? deserializeQueryModels(instanceType, result.data)
                : deserializeQueryModel(instanceType, result.data)) as TDataType;
        } else {
            this.data = (enumerable ? [] : null) as TDataType;
        }

        if (enumerable && result.changeSet) {
            this.changeSet = {
                added: deserializeQueryModels(instanceType, result.changeSet.added ?? []),
                replaced: deserializeQueryModels(instanceType, result.changeSet.replaced ?? []),
                removed: deserializeQueryModels(instanceType, result.changeSet.removed ?? []),
            } as ChangeSet<unknown>;
        }
    }

    /** @inheritdoc */
    readonly data: TDataType;

    /** @inheritdoc */
    readonly paging: PagingInfo;

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

    /**
     * Gets the optional change set describing what changed since the previous update.
     * Set by the server when the delta transfer mode is active.
     * When present, clients can apply the delta to their local state rather than replacing
     * the full dataset.
     */
    readonly changeSet?: ChangeSet<unknown>;

    /**
     * Gets whether or not the query has data.
     */
    get hasData(): boolean {
        if (this.data) {
            if (Array.isArray(this.data)) {
                return this.data.length > 0;
            }
            return true;
        }
        return false;
    }
}
