// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { act, render, screen } from '@testing-library/react';
import { QueryResult } from '@cratis/arc/queries';
import { type ArcConfiguration, ArcContext } from '../../ArcContext';
import {
    clearSuspenseObservableQueryCache,
    useSuspenseObservableQuery,
} from '../useSuspenseObservableQuery';
import {
    FakeSuspenseObservableQuery,
    type FakeSuspenseObservableQueryResult,
} from './FakeSuspenseObservableQuery';

const config: ArcConfiguration = {
    microservice: 'test-microservice',
    apiBasePath: '/api',
    origin: 'https://example.com',
};
const maximumUnclaimedResourceCount = 100;

function createReadyResult(
    name: string,
): QueryResult<FakeSuspenseObservableQueryResult[]> {
    return new QueryResult<FakeSuspenseObservableQueryResult[]>(
        {
            data: [{ id: '1', name }],
            isSuccess: true,
            isReady: true,
            isAuthorized: true,
            isValid: true,
            hasExceptions: false,
            validationResults: [],
            exceptionMessages: [],
            exceptionStackTrace: '',
            paging: { page: 0, size: 0, totalItems: 1, totalPages: 1 },
        },
        Object,
        true,
    );
}

function renderConsumer(cacheDiscriminator: number, testId?: string) {
    const Consumer = () => {
        const [result] = useSuspenseObservableQuery<
            FakeSuspenseObservableQueryResult[],
            FakeSuspenseObservableQuery,
            { cacheDiscriminator: number }
        >(FakeSuspenseObservableQuery, { cacheDiscriminator });
        return React.createElement(
            'div',
            testId === undefined ? undefined : { 'data-testid': testId },
            result.data[0]?.name,
        );
    };

    return render(
        React.createElement(
            ArcContext.Provider,
            { value: config },
            React.createElement(
                React.Suspense,
                { fallback: React.createElement('div', null, 'Loading...') },
                React.createElement(Consumer),
            ),
        ),
    );
}

function abandonConsumers(count: number, firstCacheDiscriminator: number = 0): void {
    for (let index = 0; index < count; index++) {
        renderConsumer(firstCacheDiscriminator + index).unmount();
    }
}

describe('when unclaimed suspense observable resources exceed capacity', () => {
    beforeEach(() => {
        clearSuspenseObservableQueryCache();
        FakeSuspenseObservableQuery.reset();
    });

    afterEach(() => {
        clearSuspenseObservableQueryCache();
    });

    it('should bound abandoned resources by disposing the oldest first', () => {
        abandonConsumers(maximumUnclaimedResourceCount + 1);

        FakeSuspenseObservableQuery.subscribeCallbacks.should.have.lengthOf(
            maximumUnclaimedResourceCount + 1,
        );
        FakeSuspenseObservableQuery.unsubscribeCallCount.should.equal(1);
        FakeSuspenseObservableQuery.unsubscribedSubscriptionIndexes.should.deep.equal([
            0,
        ]);
    });

    it('should ignore a late callback from the evicted resource', async () => {
        abandonConsumers(maximumUnclaimedResourceCount + 1);
        const evictedCallback = FakeSuspenseObservableQuery.subscribeCallbacks[0];

        await act(async () => {
            evictedCallback(createReadyResult('Evicted'));
        });

        const replacement = renderConsumer(0, 'content');
        FakeSuspenseObservableQuery.subscribeCallbacks.should.have.lengthOf(
            maximumUnclaimedResourceCount + 2,
        );

        await act(async () => {
            FakeSuspenseObservableQuery.subscribeCallbacks[
                maximumUnclaimedResourceCount + 1
            ](createReadyResult('Replacement'));
        });

        (screen.getByTestId('content').textContent ?? '').should.equal('Replacement');
        replacement.unmount();
    });

    it('should never evict a committed resource under pressure', async () => {
        const committedConsumer = renderConsumer(-1, 'committed-content');
        const committedCallback = FakeSuspenseObservableQuery.subscribeCallbacks[0];

        await act(async () => {
            committedCallback(createReadyResult('Initial'));
        });

        abandonConsumers(maximumUnclaimedResourceCount + 1);

        FakeSuspenseObservableQuery.unsubscribedSubscriptionIndexes.should.deep.equal([
            1,
        ]);

        await act(async () => {
            committedCallback(createReadyResult('Updated'));
        });

        (screen.getByTestId('committed-content').textContent ?? '').should.equal(
            'Updated',
        );

        await act(async () => {
            committedConsumer.unmount();
        });
        FakeSuspenseObservableQuery.unsubscribedSubscriptionIndexes.should.include(0);
    });
});
