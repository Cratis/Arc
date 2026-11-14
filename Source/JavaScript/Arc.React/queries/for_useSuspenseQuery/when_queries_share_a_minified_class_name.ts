// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { act, render, screen } from '@testing-library/react';
import { QueryFor } from '@cratis/arc/queries';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';
import { ArcContext } from '../../ArcContext.js';
import { clearSuspenseQueryCache, useSuspenseQuery } from '../useSuspenseQuery.js';

function createQuery(route: string, queryName?: string) {
    class Query extends QueryFor<string[]> {
        readonly route = route;
        readonly queryName = queryName;
        readonly parameterDescriptors = [];
        readonly requiredRequestParameters = [];
        defaultValue: string[] = [];
        constructor() { super(String, true); }
    }
    Object.defineProperty(Query, 'name', { value: 'a' });
    return Query;
}

function consumer(query: ReturnType<typeof createQuery>, id: string) {
    return function Consumer() {
        const [result] = useSuspenseQuery<string[], InstanceType<typeof query>>(query);
        return React.createElement('div', { 'data-testid': id }, result.data.join(','));
    };
}

function tree(first: React.ComponentType, second: React.ComponentType) {
    return React.createElement(ArcContext.Provider, { value: { microservice: '', apiBasePath: '', origin: '' } },
        React.createElement(React.Suspense, { fallback: 'Loading...' },
            React.createElement(first), React.createElement(second)));
}

const describeSharedMinifiedClassName = (generatedNames: boolean) => describe(`when suspense queries share a minified class name with generated names: ${generatedNames}`, () => {
    const First = createQuery('/api/first', generatedNames ? 'Namespace.First.Query' : undefined);
    const Second = createQuery('/api/second', generatedNames ? 'Namespace.Second.Query' : undefined);
    let fetchHelper: ReturnType<typeof createFetchHelper>;
    let fetchStub: ReturnType<ReturnType<typeof createFetchHelper>['stubFetch']>;

    beforeEach(() => {
        clearSuspenseQueryCache();
        fetchHelper = createFetchHelper();
        fetchStub = fetchHelper.stubFetch();
        fetchStub.callsFake(async (url: unknown) => ({
            json: async () => ({
                data: [String(url).includes('/first') ? 'First' : 'Second'],
                isSuccess: true, isReady: true, isAuthorized: true, isValid: true,
                hasExceptions: false, validationResults: [], exceptionMessages: [],
                exceptionStackTrace: '', paging: { page: 0, size: 0, totalItems: 1, totalPages: 1 }
            })
        } as Response));
    });

    afterEach(() => {
        fetchHelper.restore();
        clearSuspenseQueryCache();
    });

    it('should request and render each query independently', async () => {
        render(tree(consumer(First, 'first'), consumer(Second, 'second')));
        await act(async () => {});
        fetchStub.callCount.should.equal(2);
        (screen.getByTestId('first').textContent ?? '').should.equal('First');
        (screen.getByTestId('second').textContent ?? '').should.equal('Second');
    });

    it('should share a request between consumers of the same query', async () => {
        render(tree(consumer(First, 'first'), consumer(First, 'second')));
        await act(async () => {});
        fetchStub.callCount.should.equal(1);
        (screen.getByTestId('first').textContent ?? '').should.equal('First');
        (screen.getByTestId('second').textContent ?? '').should.equal('First');
    });
});

describeSharedMinifiedClassName(false);
describeSharedMinifiedClassName(true);
