// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { act, render } from '@testing-library/react';
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

function createReadyResult(): QueryResult<FakeSuspenseObservableQueryResult[]> {
    return new QueryResult<FakeSuspenseObservableQueryResult[]>(
        {
            data: [{ id: '1', name: 'Shared' }],
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

function renderConsumer() {
    const Consumer = () => {
        useSuspenseObservableQuery<
            FakeSuspenseObservableQueryResult[],
            FakeSuspenseObservableQuery
        >(FakeSuspenseObservableQuery);
        return React.createElement('div', null, 'Ready');
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

describe('when multiple consumers share a suspense observable resource', () => {
    beforeEach(() => {
        clearSuspenseObservableQueryCache();
        FakeSuspenseObservableQuery.reset();
    });

    afterEach(() => {
        clearSuspenseObservableQueryCache();
    });

    it('should retain the subscription until the last owner unmounts', async () => {
        const firstConsumer = renderConsumer();
        const secondConsumer = renderConsumer();

        FakeSuspenseObservableQuery.subscribeCallbacks.should.have.lengthOf(1);

        await act(async () => {
            FakeSuspenseObservableQuery.subscribeCallbacks[0](createReadyResult());
        });

        await act(async () => {
            firstConsumer.unmount();
        });
        FakeSuspenseObservableQuery.unsubscribeCallCount.should.equal(0);

        await act(async () => {
            secondConsumer.unmount();
        });
        FakeSuspenseObservableQuery.unsubscribeCallCount.should.equal(1);
    });
});
