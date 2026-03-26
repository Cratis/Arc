// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IObservableQueryFor, QueryResultWithState, Sorting, Paging } from '@cratis/arc/queries';
import { Constructor } from '@cratis/fundamentals';
import { SetSorting } from './SetSorting';
import { SetPage } from './SetPage';
import { SetPageSize } from './SetPageSize';
import { useObservableQuery, useObservableQueryWithPaging } from './useObservableQuery';
import { useSuspenseObservableQuery, useSuspenseObservableQueryWithPaging } from './useSuspenseObservableQuery';

/**
 * Represents a conditional observable query binding that conditionally subscribes based on a boolean condition.
 * When the condition is false, all hooks are no-ops and return empty results. When true, they behave exactly
 * as calling the underlying hooks directly.
 *
 * Obtain an instance via the static `when()` method on a generated observable query class:
 * ```ts
 * const [result] = MyObservableQuery.when(!!id).use({ id });
 * ```
 * @template TQuery The observable query class type.
 * @template TDataType The data type returned by the query.
 * @template TArguments Optional argument type for the query.
 */
export class ObservableQueryWhen<TQuery extends IObservableQueryFor<TDataType>, TDataType, TArguments = object> {
    /**
     * Initializes a new instance of {@link ObservableQueryWhen}.
     * @param {Constructor<TQuery>} _query The observable query constructor.
     * @param {boolean} _isEnabled Whether the subscription should be established.
     */
    constructor(
        private readonly _query: Constructor<TQuery>,
        private readonly _isEnabled: boolean
    ) {}

    /**
     * Calls the underlying {@link useObservableQuery} hook, conditionally subscribing.
     * @param {TArguments} args Optional arguments for the query.
     * @param {Sorting} sorting Optional sorting for the query.
     * @returns Tuple of {@link QueryResultWithState} and a {@link SetSorting} delegate.
     */
    use(args?: TArguments, sorting?: Sorting): [QueryResultWithState<TDataType>, SetSorting] {
        return useObservableQuery<TDataType, TQuery, TArguments>(this._query, args, sorting, this._isEnabled);
    }

    /**
     * Calls the underlying {@link useObservableQueryWithPaging} hook, conditionally subscribing.
     * @param {number} pageSize Number of items per page.
     * @param {TArguments} args Optional arguments for the query.
     * @param {Sorting} sorting Optional sorting for the query.
     * @returns Tuple of {@link QueryResultWithState} and paging/sorting controls.
     */
    useWithPaging(pageSize: number, args?: TArguments, sorting?: Sorting): [QueryResultWithState<TDataType>, SetSorting, SetPage, SetPageSize] {
        return useObservableQueryWithPaging<TDataType, TQuery, TArguments>(this._query, new Paging(0, pageSize), args, sorting, this._isEnabled);
    }

    /**
     * Calls the underlying {@link useSuspenseObservableQuery} hook, conditionally subscribing.
     * When disabled, returns an empty result without suspending the component.
     * @param {TArguments} args Optional arguments for the query.
     * @param {Sorting} sorting Optional sorting for the query.
     * @returns Tuple of {@link QueryResultWithState} and a {@link SetSorting} delegate.
     */
    useSuspense(args?: TArguments, sorting?: Sorting): [QueryResultWithState<TDataType>, SetSorting] {
        return useSuspenseObservableQuery<TDataType, TQuery, TArguments>(this._query, args, sorting, this._isEnabled);
    }

    /**
     * Calls the underlying {@link useSuspenseObservableQueryWithPaging} hook, conditionally subscribing.
     * When disabled, returns an empty result without suspending the component.
     * @param {number} pageSize Number of items per page.
     * @param {TArguments} args Optional arguments for the query.
     * @param {Sorting} sorting Optional sorting for the query.
     * @returns Tuple of {@link QueryResultWithState} and paging/sorting controls.
     */
    useSuspenseWithPaging(pageSize: number, args?: TArguments, sorting?: Sorting): [QueryResultWithState<TDataType>, SetSorting, SetPage, SetPageSize] {
        return useSuspenseObservableQueryWithPaging<TDataType, TQuery, TArguments>(this._query, new Paging(0, pageSize), args, sorting, this._isEnabled);
    }
}
