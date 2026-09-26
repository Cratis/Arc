// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { act, render } from '@testing-library/react';
import { QueryInstanceCache, QueryResult, QueryResultWithState } from '@cratis/arc/queries';
import { ArcConfiguration, ArcContext } from '../../ArcContext.js';
import { QueryInstanceCacheContext } from '../QueryInstanceCacheContext.js';
import { useObservableQuery } from '../useObservableQuery.js';
import { FakeStringObservableQuery } from './FakeStringObservableQuery.js';

describe('when identity-less snapshots replace shared observable query state', () => {
    const cache = new QueryInstanceCache();
    const results: QueryResultWithState<string[]>[] = [];
    const config: ArcConfiguration = {
        microservice: 'test-microservice',
        apiBasePath: '/api',
        origin: 'https://example.com',
        queryVersion: 0,
    };

    const snapshot = (data: string[]) => ({
        data,
        isSuccess: true,
        isAuthorized: true,
        isValid: true,
        hasExceptions: false,
        validationResults: [],
        exceptionMessages: [],
        exceptionStackTrace: '',
        paging: { page: 0, size: 0, totalItems: data.length, totalPages: 1 },
    } as unknown as QueryResult<string[]>);

    const Consumer = ({ index }: { index: number }) => {
        const [result] = useObservableQuery<string[], FakeStringObservableQuery>(FakeStringObservableQuery);
        results[index] = result;
        return React.createElement('div');
    };

    const view = (context: ArcConfiguration) => React.createElement(
        QueryInstanceCacheContext.Provider,
        { value: cache },
        React.createElement(
            ArcContext.Provider,
            { value: context },
            React.createElement(Consumer, { index: 0 }),
            React.createElement(Consumer, { index: 1 }),
        ),
    );

    beforeEach(() => {
        FakeStringObservableQuery.reset();
        results.length = 0;
    });

    afterEach(() => cache.dispose());

    it('replaces both consumers and the cache with each snapshot, including after reconnect', async () => {
        const rendered = render(view(config));
        FakeStringObservableQuery.subscribeCallbacks.length.should.equal(1);

        await act(async () => {
            FakeStringObservableQuery.subscribeCallbacks[0](snapshot(['first', 'second']));
        });
        await act(async () => {
            FakeStringObservableQuery.subscribeCallbacks[0](snapshot(['second']));
        });
        results[0].data.should.deep.equal(['second']);
        results[1].data.should.deep.equal(['second']);

        cache.teardownAllSubscriptions();
        rendered.rerender(view({ ...config, queryVersion: 1 }));
        FakeStringObservableQuery.subscribeCallbacks.length.should.equal(2);

        await act(async () => {
            FakeStringObservableQuery.subscribeCallbacks[1](snapshot([]));
        });
        results[0].data.should.deep.equal([]);
        results[1].data.should.deep.equal([]);
        cache.getLastResult<string[]>(cache.buildKey(FakeStringObservableQuery.name))!.data.should.deep.equal([]);
    });
});
