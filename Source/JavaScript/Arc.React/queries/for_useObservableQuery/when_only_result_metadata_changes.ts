// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { act, render } from '@testing-library/react';
import { QueryInstanceCache, QueryResultWithState } from '@cratis/arc/queries';
import { ArcContext } from '../../ArcContext.js';
import { QueryInstanceCacheContext } from '../QueryInstanceCacheContext.js';
import { useObservableQuery } from '../useObservableQuery.js';
import { FakeStringObservableQuery } from './FakeStringObservableQuery.js';

describe('when only observable query result metadata changes', () => {
    let cache: QueryInstanceCache;
    const results: QueryResultWithState<string[]>[] = [];
    let firstData: string[];

    const Consumer = ({ index }: { index: number }) => {
        const [result] = useObservableQuery<string[], FakeStringObservableQuery>(FakeStringObservableQuery);
        results[index] = result;
        return React.createElement('div');
    };

    const snapshot = (totalItems: number, message: string) => new QueryResultWithState(
        ['row'], { page: 0, size: 1, totalItems, totalPages: totalItems },
        false, true, true, [], true, [message], '', false
    );

    beforeEach(async () => {
        cache = new QueryInstanceCache();
        FakeStringObservableQuery.reset();
        results.length = 0;
        render(React.createElement(QueryInstanceCacheContext.Provider, { value: cache },
            React.createElement(ArcContext.Provider, { value: { microservice: '' } },
                React.createElement(Consumer, { index: 0 }),
                React.createElement(Consumer, { index: 1 }))));
        await act(async () => { FakeStringObservableQuery.subscribeCallbacks[0](snapshot(1, 'first')); });
        firstData = results[0].data;
        await act(async () => { FakeStringObservableQuery.subscribeCallbacks[0](snapshot(5, 'second')); });
    });

    afterEach(() => cache.dispose());

    it('should publish the new paging to the first consumer', () => results[0].paging.totalItems.should.equal(5));
    it('should publish the new paging to the second consumer', () => results[1].paging.totalPages.should.equal(5));
    it('should publish the new failure message to the first consumer', () => results[0].exceptionMessages.should.deep.equal(['second']));
    it('should publish the new failure message to the second consumer', () => results[1].exceptionMessages.should.deep.equal(['second']));
    it('should preserve unchanged data references', () => results[0].data.should.equal(firstData));
});
