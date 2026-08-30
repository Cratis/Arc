// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import {
    type IObservableQueryFor,
    type IQueryFor,
    ObservableQueryFor,
    QueryFor,
} from '@cratis/arc/queries';
import type { ParameterDescriptor } from '@cratis/arc/reflection';
import type { Constructor } from '@cratis/fundamentals';
import { useMemo } from 'react';
import { useObservableQuery } from '../../queries/useObservableQuery';
import { useQuery } from '../../queries/useQuery';
import { QueryReturnsMultipleInstances } from '../../queries/QueryReturnsMultipleInstances';

/* eslint-disable @typescript-eslint/no-empty-object-type */
class NoPopulationQuery extends QueryFor<Record<string, never>> {
    readonly route = '';
    readonly parameterDescriptors: ParameterDescriptor[] = [];
    defaultValue: Record<string, never> = {};

    get requiredRequestParameters(): string[] {
        return [];
    }

    constructor() {
        super(Object, false);
    }
}

class NoPopulationObservableQuery extends ObservableQueryFor<Record<string, never>> {
    readonly route = '';
    readonly parameterDescriptors: ParameterDescriptor[] = [];
    readonly defaultValue: Record<string, never> = {};

    get requiredRequestParameters(): string[] {
        return [];
    }

    constructor() {
        super(Object, false);
    }
}
/* eslint-enable @typescript-eslint/no-empty-object-type */

function guardSingleInstance(query: Constructor<{ enumerable?: boolean }> | undefined) {
    if (!query) {
        return;
    }

    const probe = new query();
    if (probe.enumerable) {
        throw new QueryReturnsMultipleInstances(query.name);
    }
}

/**
 * Populates a command's initial values from a single-instance query, matched onto the command's
 * properties by name - the same population source a {@link CommandFormProps.populateFromQuery} prop
 * uses. The query is optional: omit it and the hook is a no-op.
 * @template TDataType Type of data the query returns.
 * @template TQuery Type of query to use.
 * @template TArguments Optional: arguments for the query, if any.
 * @param query Optional: the query type constructor. Must be a single-instance (non-enumerable) query.
 * @param args Optional: arguments for the query, if any.
 * @returns The query's resolved data, or `undefined` while it has not resolved yet or no query was given.
 */
export function usePopulateFromQuery<
    TDataType extends object,
    TQuery extends IQueryFor<TDataType>,
    TArguments = object,
>(query?: Constructor<TQuery>, args?: TArguments): TDataType | undefined {
    // SAFETY: Arc query constructors expose the runtime enumerable metadata used by the guard.
    useMemo(
        () =>
            guardSingleInstance(
                query as unknown as Constructor<{ enumerable?: boolean }> | undefined,
            ),
        [query],
    );

    // SAFETY: The no-op query is selected only while disabled, so its data type is never observed.
    const queryType = query ?? (NoPopulationQuery as unknown as Constructor<TQuery>);
    const [result] = useQuery<TDataType, TQuery, TArguments>(
        queryType,
        args,
        undefined,
        query !== undefined,
    );

    return query !== undefined &&
        result.isReady !== false &&
        result.isSuccess &&
        result.hasData
        ? result.data
        : undefined;
}

/**
 * Populates a command's initial values from a single-instance observable query, matched onto the
 * command's properties by name - the same population source a
 * {@link CommandFormProps.populateFromObservableQuery} prop uses. The query is optional: omit it and
 * the hook is a no-op.
 * @template TDataType Type of data the query returns.
 * @template TQuery Type of query to use.
 * @template TArguments Optional: arguments for the query, if any.
 * @param query Optional: the observable query type constructor. Must be a single-instance (non-enumerable) query.
 * @param args Optional: arguments for the query, if any.
 * @returns The query's resolved data, or `undefined` while it has not resolved yet or no query was given.
 */
export function usePopulateFromObservableQuery<
    TDataType extends object,
    TQuery extends IObservableQueryFor<TDataType>,
    TArguments = object,
>(query?: Constructor<TQuery>, args?: TArguments): TDataType | undefined {
    // SAFETY: Arc observable-query constructors expose the runtime enumerable metadata used by the guard.
    useMemo(
        () =>
            guardSingleInstance(
                query as unknown as Constructor<{ enumerable?: boolean }> | undefined,
            ),
        [query],
    );

    // SAFETY: The no-op query is selected only while disabled, so its data type is never observed.
    const queryType =
        query ?? (NoPopulationObservableQuery as unknown as Constructor<TQuery>);
    const [result] = useObservableQuery<TDataType, TQuery, TArguments>(
        queryType,
        args,
        undefined,
        query !== undefined,
    );

    return query !== undefined &&
        result.isReady !== false &&
        result.isSuccess &&
        result.hasData
        ? result.data
        : undefined;
}
