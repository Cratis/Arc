// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { act, render, screen } from '@testing-library/react';
import { QueryResult } from '@cratis/arc/queries';
import { ArcContext } from '../../ArcContext.js';
import { clearSuspenseObservableQueryCache, useSuspenseObservableQuery } from '../useSuspenseObservableQuery.js';
import { FakeSuspenseObservableQuery, type FakeSuspenseObservableQueryResult } from './FakeSuspenseObservableQuery.js';

function createQuery(queryName?: string) {
    class Query extends FakeSuspenseObservableQuery {
        readonly queryName = queryName;
    }
    Object.defineProperty(Query, 'name', { value: 'a' });
    return Query;
}

function consumer(query: ReturnType<typeof createQuery>, id: string) {
    return function Consumer() {
        const [result] = useSuspenseObservableQuery<FakeSuspenseObservableQueryResult[], InstanceType<typeof query>>(query);
        return React.createElement('div', { 'data-testid': id }, result.data[0]?.name);
    };
}

function readyResult(name: string) {
    return new QueryResult<FakeSuspenseObservableQueryResult[]>({
        data: [{ id: name, name }], isSuccess: true, isReady: true, isAuthorized: true,
        isValid: true, hasExceptions: false, validationResults: [], exceptionMessages: [],
        exceptionStackTrace: '', paging: { page: 0, size: 0, totalItems: 1, totalPages: 1 }
    }, Object, true);
}

const describeSharedMinifiedClassName = (generatedNames: boolean) => describe(`when suspense observable queries share a minified class name with generated names: ${generatedNames}`, () => {
    const First = createQuery(generatedNames ? 'Namespace.First.Query' : undefined);
    const Second = createQuery(generatedNames ? 'Namespace.Second.Query' : undefined);

    beforeEach(() => {
        clearSuspenseObservableQueryCache();
        FakeSuspenseObservableQuery.reset();
    });

    afterEach(() => {
        clearSuspenseObservableQueryCache();
    });

    it('should subscribe and update each query independently', async () => {
        render(React.createElement(ArcContext.Provider, { value: { microservice: '' } },
            React.createElement(React.Suspense, { fallback: 'Loading...' },
                React.createElement(consumer(First, 'first')),
                React.createElement(consumer(Second, 'second')))));

        FakeSuspenseObservableQuery.subscribeCallbacks.should.have.lengthOf(2);
        await act(async () => {
            FakeSuspenseObservableQuery.subscribeCallbacks[0](readyResult('First'));
            FakeSuspenseObservableQuery.subscribeCallbacks[1](readyResult('Second'));
        });
        (screen.getByTestId('first').textContent ?? '').should.equal('First');
        (screen.getByTestId('second').textContent ?? '').should.equal('Second');

        await act(async () => {
            FakeSuspenseObservableQuery.subscribeCallbacks[0](readyResult('Updated First'));
        });
        (screen.getByTestId('first').textContent ?? '').should.equal('Updated First');
        (screen.getByTestId('second').textContent ?? '').should.equal('Second');
    });
});

describeSharedMinifiedClassName(false);
describeSharedMinifiedClassName(true);
