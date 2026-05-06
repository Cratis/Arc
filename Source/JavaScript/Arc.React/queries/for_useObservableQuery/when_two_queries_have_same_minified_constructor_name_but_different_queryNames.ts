// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, act } from '@testing-library/react';
import { useObservableQuery } from '../useObservableQuery';
import { ObservableQueryFor, QueryResult, ObservableQuerySubscription, OnNextResult, QueryInstanceCache } from '@cratis/arc/queries';
import { ParameterDescriptor } from '@cratis/arc/reflection';
import { ArcContext, ArcConfiguration } from '../../ArcContext';
import { QueryInstanceCacheContext } from '../QueryInstanceCacheContext';

/* eslint-disable @typescript-eslint/no-explicit-any */

interface Item {
    id: string;
    name: string;
}

type ItemCallback = OnNextResult<QueryResult<Item[]>>;

class FirstQuery extends ObservableQueryFor<Item[]> {
    readonly route = '/api/first';
    readonly queryName = 'Namespace.First.Query';
    readonly parameterDescriptors: ParameterDescriptor[] = [];

    static subscribeCallbacks: ItemCallback[] = [];

    get requiredRequestParameters(): string[] {
        return [];
    }

    defaultValue: Item[] = [];

    constructor() {
        super(Object, true);
    }

    subscribe(callback: ItemCallback): ObservableQuerySubscription<Item[]> {
        FirstQuery.subscribeCallbacks.push(callback);
        return { unsubscribe: () => {} } as unknown as ObservableQuerySubscription<Item[]>;
    }

    static reset() {
        FirstQuery.subscribeCallbacks = [];
    }
}

class SecondQuery extends ObservableQueryFor<Item[]> {
    readonly route = '/api/second';
    readonly queryName = 'Namespace.Second.Query';
    readonly parameterDescriptors: ParameterDescriptor[] = [];

    static subscribeCallbacks: ItemCallback[] = [];

    get requiredRequestParameters(): string[] {
        return [];
    }

    defaultValue: Item[] = [];

    constructor() {
        super(Object, true);
    }

    subscribe(callback: ItemCallback): ObservableQuerySubscription<Item[]> {
        SecondQuery.subscribeCallbacks.push(callback);
        return { unsubscribe: () => {} } as unknown as ObservableQuerySubscription<Item[]>;
    }

    static reset() {
        SecondQuery.subscribeCallbacks = [];
    }
}

// Simulate minification — both classes receive the same single-letter constructor name.
// Without the queryName fix, both hooks would build the same cache key ('a::') and
// share a single subscription, delivering each other's data.
Object.defineProperty(FirstQuery, 'name', { value: 'a', configurable: true });
Object.defineProperty(SecondQuery, 'name', { value: 'a', configurable: true });

describe('when two queries have same minified constructor name but different queryNames', () => {
    let firstData: Item[] | null = null;
    let secondData: Item[] | null = null;
    let cache: QueryInstanceCache;

    const config: ArcConfiguration = {
        microservice: '',
        apiBasePath: '/api',
        origin: 'https://example.com',
    };

    beforeEach(async () => {
        FirstQuery.reset();
        SecondQuery.reset();
        firstData = null;
        secondData = null;
        cache = new QueryInstanceCache(0);

        const FirstComponent = () => {
            const [result] = useObservableQuery(FirstQuery);
            if (result.isSuccess) firstData = result.data as Item[];
            return React.createElement('div', null, 'First');
        };

        const SecondComponent = () => {
            const [result] = useObservableQuery(SecondQuery);
            if (result.isSuccess) secondData = result.data as Item[];
            return React.createElement('div', null, 'Second');
        };

        render(
            React.createElement(
                QueryInstanceCacheContext.Provider,
                { value: cache },
                React.createElement(
                    ArcContext.Provider,
                    { value: config },
                    React.createElement(FirstComponent),
                    React.createElement(SecondComponent)
                )
            )
        );

        await act(async () => {
            FirstQuery.subscribeCallbacks[0]?.(new QueryResult({
                data: [{ id: '1', name: 'From First' }],
                isSuccess: true,
                isAuthorized: true,
                isValid: true,
                hasExceptions: false,
                validationResults: [],
                exceptionMessages: [],
                exceptionStackTrace: '',
                paging: { page: 0, size: 0, totalItems: 1, totalPages: 1 }
            }, Object, true));
        });
    });

    it('should establish separate subscriptions for each query', () => {
        FirstQuery.subscribeCallbacks.should.have.lengthOf(1);
        SecondQuery.subscribeCallbacks.should.have.lengthOf(1);
    });

    it('should deliver data to the first query component', () => {
        firstData!.should.not.be.null;
        (firstData as unknown as Item[]).should.have.lengthOf(1);
        (firstData as unknown as Item[])[0].name.should.equal('From First');
    });

    it('should not deliver first query data to the second query component', () => {
        // SecondComponent may hold its defaultValue [] because initial state has isSuccess: true,
        // but it must NOT contain the data delivered to the first query.
        const items = secondData as unknown as Item[] | null;
        const hasFirstQueryData = items !== null && items.length > 0 && items[0]?.name === 'From First';
        hasFirstQueryData.should.be.false;
    });
});
