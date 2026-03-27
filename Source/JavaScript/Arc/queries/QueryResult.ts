// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Constructor, JsonSerializer } from '@cratis/fundamentals';
import { ValidationResult } from '../validation/ValidationResult';
import { IQueryResult } from './IQueryResult';
import { PagingInfo } from './PagingInfo';

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
            let data: object = result.data;
            if (enumerable) {
                if (Array.isArray(result.data)) {
                    data = JsonSerializer.deserializeArrayFromInstance(instanceType, data);
                } else {
                    data = [];
                }
            } else {
                data = JsonSerializer.deserializeFromInstance(instanceType, data);
            }

            this.data = data as TDataType;
        } else {
            this.data = null as TDataType;
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
