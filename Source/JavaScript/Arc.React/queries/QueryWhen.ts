// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IQueryFor, QueryResultWithState, Sorting, Paging } from '@cratis/arc/queries';
import { Constructor } from '@cratis/fundamentals';
import { SetSorting } from './SetSorting';
import { SetPage } from './SetPage';
import { SetPageSize } from './SetPageSize';
import { PerformQuery, useQuery, useQueryWithPaging } from './useQuery';
import { useSuspenseQuery, useSuspenseQueryWithPaging } from './useSuspenseQuery';

/**
 * Represents a conditional query binding that conditionally executes the query hooks based on a boolean condition.
 * When the condition is false, all hooks are no-ops and return empty results. When true, they behave exactly
 * as calling the underlying hooks directly.
 *
 * Obtain an instance via the static `when()` method on a generated query class:
 * ```ts
 * const [result] = MyQuery.when(!!id).use({ id });
 * ```
 * @template TQuery The query class type.
 * @template TDataType The data type returned by the query.
 * @template TArguments Optional argument type for the query.
 */
export class QueryWhen<TQuery extends IQueryFor<TDataType>, TDataType, TArguments = object> {
    /**
     * Initializes a new instance of {@link QueryWhen}.
     * @param {Constructor<TQuery>} _query The query constructor.
     * @param {boolean} _isEnabled Whether the query should be executed.
     */
    constructor(
        private readonly _query: Constructor<TQuery>,
        private readonly _isEnabled: boolean
    ) {}

    /**
     * Calls the underlying {@link useQuery} hook, conditionally executing the query.
     * @param {TArguments} args Optional arguments for the query.
     * @param {Sorting} sorting Optional sorting for the query.
     * @returns Tuple of {@link QueryResultWithState} and hook controls.
     */
    use(args?: TArguments, sorting?: Sorting): [QueryResultWithState<TDataType>, PerformQuery<TArguments>, SetSorting] {
        return useQuery<TDataType, TQuery, TArguments>(this._query, args, sorting, this._isEnabled);
    }

    /**
     * Calls the underlying {@link useQueryWithPaging} hook, conditionally executing the query.
     * @param {number} pageSize Number of items per page.
     * @param {TArguments} args Optional arguments for the query.
     * @param {Sorting} sorting Optional sorting for the query.
     * @returns Tuple of {@link QueryResultWithState} and hook controls.
     */
    useWithPaging(pageSize: number, args?: TArguments, sorting?: Sorting): [QueryResultWithState<TDataType>, PerformQuery<TArguments>, SetSorting, SetPage, SetPageSize] {
        return useQueryWithPaging<TDataType, TQuery, TArguments>(this._query, new Paging(0, pageSize), args, sorting, this._isEnabled);
    }

    /**
     * Calls the underlying {@link useSuspenseQuery} hook, conditionally executing the query.
     * When disabled, returns an empty result without suspending the component.
     * @param {TArguments} args Optional arguments for the query.
     * @param {Sorting} sorting Optional sorting for the query.
     * @returns Tuple of {@link QueryResultWithState} and hook controls.
     */
    useSuspense(args?: TArguments, sorting?: Sorting): [QueryResultWithState<TDataType>, PerformQuery<TArguments>, SetSorting] {
        return useSuspenseQuery<TDataType, TQuery, TArguments>(this._query, args, sorting, this._isEnabled);
    }

    /**
     * Calls the underlying {@link useSuspenseQueryWithPaging} hook, conditionally executing the query.
     * When disabled, returns an empty result without suspending the component.
     * @param {number} pageSize Number of items per page.
     * @param {TArguments} args Optional arguments for the query.
     * @param {Sorting} sorting Optional sorting for the query.
     * @returns Tuple of {@link QueryResultWithState} and hook controls.
     */
    useSuspenseWithPaging(pageSize: number, args?: TArguments, sorting?: Sorting): [QueryResultWithState<TDataType>, PerformQuery<TArguments>, SetSorting, SetPage, SetPageSize] {
        return useSuspenseQueryWithPaging<TDataType, TQuery, TArguments>(this._query, new Paging(0, pageSize), args, sorting, this._isEnabled);
    }
}
