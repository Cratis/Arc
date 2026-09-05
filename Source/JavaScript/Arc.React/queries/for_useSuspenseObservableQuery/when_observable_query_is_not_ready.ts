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

function createResult(
    isReady: boolean,
    name: string,
): QueryResult<FakeSuspenseObservableQueryResult[]> {
    return new QueryResult<FakeSuspenseObservableQueryResult[]>(
        {
            data: [{ id: '1', name }],
            isSuccess: true,
            isReady,
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

function renderTestComponent(onResult: (name: string) => void) {
    const TestComponent = () => {
        const [result] = useSuspenseObservableQuery<
            FakeSuspenseObservableQueryResult[],
            FakeSuspenseObservableQuery
        >(FakeSuspenseObservableQuery);
        onResult(result.data[0]?.name ?? '');
        return React.createElement(
            'div',
            { 'data-testid': 'content' },
            result.data[0]?.name,
        );
    };

    return render(
        React.createElement(
            ArcContext.Provider,
            { value: config },
            React.createElement(
                React.Suspense,
                {
                    fallback: React.createElement(
                        'div',
                        { 'data-testid': 'loading' },
                        'Loading...',
                    ),
                },
                React.createElement(TestComponent),
            ),
        ),
    );
}

describe('when observable query is not ready', () => {
    beforeEach(() => {
        clearSuspenseObservableQueryCache();
        FakeSuspenseObservableQuery.reset();
    });

    afterEach(() => {
        clearSuspenseObservableQueryCache();
    });

    it('should remain suspended until a later ready response arrives', async () => {
        let resultName = '';
        renderTestComponent((name) => {
            resultName = name;
        });

        const callback = FakeSuspenseObservableQuery.subscribeCallbacks[0];
        callback!.should.not.be.undefined;

        await act(async () => {
            callback(createResult(false, 'Not ready'));
        });

        screen.getByTestId('loading');
        (screen.queryByTestId('content') === null).should.be.true;

        await act(async () => {
            callback(createResult(true, 'Ready'));
        });

        screen.getByTestId('content').textContent!.should.equal('Ready');
        resultName.should.equal('Ready');
        FakeSuspenseObservableQuery.subscribeCallbacks.should.have.lengthOf(1);
    });

    it('should remain suspended after repeated not-ready responses', async () => {
        renderTestComponent(() => {});

        const callback = FakeSuspenseObservableQuery.subscribeCallbacks[0];
        callback!.should.not.be.undefined;

        await act(async () => {
            callback(createResult(false, 'First'));
            callback(createResult(false, 'Second'));
        });

        screen.getByTestId('loading');
        (screen.queryByTestId('content') === null).should.be.true;
        FakeSuspenseObservableQuery.subscribeCallbacks.should.have.lengthOf(1);
    });
});
