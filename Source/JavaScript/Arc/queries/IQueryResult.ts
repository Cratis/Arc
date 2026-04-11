// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ValidationResult } from '../validation/ValidationResult';
import { PagingInfo } from './PagingInfo';
import { ChangeSet } from './ChangeSet';

/**
 * Defines the result from executing a query.
 */
export interface IQueryResult<TDataType> {
    /**
     * Gets the data result from the query.
     */
    readonly data: TDataType;

    /**
     * Gets the paging information.
     */
    readonly paging: PagingInfo;

    /**
     * Gets whether or not the query executed successfully.
     */
    readonly isSuccess: boolean;

    /**
     * Gets whether or not the query was authorized to execute.
     */
    readonly isAuthorized: boolean;

    /**
     * Gets whether or not the query is valid.
     */
    readonly isValid: boolean;

    /**
     * Gets whether or not there are any exceptions that occurred.
     */
    readonly hasExceptions: boolean;

    /**
     * Gets any validation errors. If this collection is empty, there are errors.
     */
    readonly validationResults: ValidationResult[];

    /**
     * Gets any exception messages that might have occurred.
     */
    readonly exceptionMessages: string[];

    /**
     * Gets the stack trace if there was an exception.
     */
    readonly exceptionStackTrace: string;

    /**
     * Gets the optional change set describing what changed since the previous update.
     * When present (server-side delta mode), clients apply the delta to local state instead
     * of replacing the full dataset.
     */
    readonly changeSet?: ChangeSet<unknown>;

    /**
     * Gets whether or not the query has data.
     */
    readonly hasData: boolean;
}
