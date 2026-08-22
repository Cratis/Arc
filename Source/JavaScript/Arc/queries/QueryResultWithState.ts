// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ValidationResult } from '../validation/ValidationResult';
import { IQueryResult } from './IQueryResult';
import { PagingInfo } from './PagingInfo';
import { QueryResult } from './QueryResult';
import { ChangeSet } from './ChangeSet';

/**
 * Represents a specialized {@link QueryResult<TDataType>} that holds state for its execution
 */
export class QueryResultWithState<TDataType> implements IQueryResult<TDataType> {
    /**
     * Creates a settled empty state, such as the state returned by a disabled query.
     * @param defaultValue The query's empty data value.
     * @returns A ready state that is not performing.
     */
    static empty<TDataType>(defaultValue: TDataType): QueryResultWithState<TDataType> {
        return new QueryResultWithState(
            defaultValue,
            PagingInfo.noPaging,
            true,
            true,
            true,
            [],
            false,
            [],
            '',
            false,
            undefined,
            true,
        );
    }

    /**
     * Creates the local initial state before any server result has been received. This state is not ready because
     * no result exists yet; its performing state independently describes the active client request.
     * @param defaultValue The query's initial data value.
     * @returns A not-ready state that is performing.
     */
    static initial<TDataType>(defaultValue: TDataType): QueryResultWithState<TDataType> {
        return new QueryResultWithState(
            defaultValue,
            PagingInfo.noPaging,
            true,
            true,
            true,
            [],
            false,
            [],
            '',
            true,
            undefined,
            false,
        );
    }

    /**
     * Initializes an instance of {@link QueryResultWithState<TDataType>}.
     * @param {TDataType} data The items returned, if any - can be empty.
     * @param {PagingInfo} paging The paging info.
     * @param {boolean} isSuccess Whether or not the query was successful.
     * @param {boolean} isAuthorized Whether or not the query was authorized.
     * @param {boolean} isValid Whether or not it is valid.
     * @param {ValidationResult[]} validationResults Any validation errors.
     * @param {boolean} hasExceptions Whether or not it has exceptions.
     * @param {string[]} exceptionMessages Any exception messages.
     * @param {string} exceptionStackTrace Exception stack trace, if any.
     * @param {boolean} isPerforming Whether or not the query is being performed. True if its performing, false if it is done.
     * @param {ChangeSet<unknown> | undefined} changeSet Optional change set from the server describing the delta since the last update.
     * @param {boolean} isReady Whether the query has produced a result. Defaults to true so existing constructor calls
     * represent settled synthetic states; {@link initial} explicitly uses false because no server result exists yet.
     */
    constructor(
        readonly data: TDataType,
        readonly paging: PagingInfo,
        readonly isSuccess: boolean,
        readonly isAuthorized: boolean,
        readonly isValid: boolean,
        readonly validationResults: ValidationResult[],
        readonly hasExceptions: boolean,
        readonly exceptionMessages: string[],
        readonly exceptionStackTrace: string,
        readonly isPerforming: boolean,
        readonly changeSet?: ChangeSet<unknown>,
        readonly isReady: boolean = true,
    ) {}

    /** @inheritdoc */
    get hasData(): boolean {
        if (Array.isArray(this.data)) {
            return this.data.length > 0;
        }
        return !!this.data;
    }

    /**
     * Create a new {@link QueryResultWithState<TDataType>} from {@link QueryResult<TDataType>}.
     * @param queryResult The original query result.
     * @param isPerforming Whether or not the query is performing.
     * @returns A new {@link QueryResultWithState<TDataType>}
     */
    static fromQueryResult<TDataType>(
        queryResult: QueryResult<TDataType>,
        isPerforming: boolean,
    ) {
        return new QueryResultWithState<TDataType>(
            queryResult.data,
            queryResult.paging,
            queryResult.isSuccess,
            queryResult.isAuthorized,
            queryResult.isValid,
            queryResult.validationResults,
            queryResult.hasExceptions,
            queryResult.exceptionMessages,
            queryResult.exceptionStackTrace,
            isPerforming,
            queryResult.changeSet,
            queryResult.isReady,
        );
    }
}
