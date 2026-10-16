// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, waitFor, act } from '@testing-library/react';
import sinon from 'sinon';
import { QueryInstanceCache, QueryResultWithState } from '@cratis/arc/queries';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';
import { useQuery } from '../../useQuery.js';
import { FakeQueryWithRequiredParameters, FakeQueryWithRequiredParametersResult } from '../FakeQueryWithRequiredParameters.js';
import { ArcContext, ArcConfiguration } from '../../../ArcContext.js';
import { QueryInstanceCacheContext } from '../../QueryInstanceCacheContext.js';

describe('when two hooks share a query and the arguments differ', () => {
    let fetchStub: sinon.SinonStub;
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };
    let firstResult: QueryResultWithState<unknown>;
    let secondResult: QueryResultWithState<unknown>;
    let signals: AbortSignal[];

    const config: ArcConfiguration = {
        microservice: 'test-microservice',
        apiBasePath: '/api',
        origin: 'https://example.com'
    };

    const responseFor = (userId: string) => ({
        json: async () => ({
            data: [{ id: userId, name: `for ${userId}` }],
            isSuccess: true,
            isAuthorized: true,
            isValid: true,
            hasExceptions: false,
            validationResults: [],
            exceptionMessages: [],
            exceptionStackTrace: '',
            paging: { page: 0, size: 0, totalItems: 0, totalPages: 0 }
        })
    } as Response);

    beforeEach(async () => {
        fetchHelper = createFetchHelper();
        fetchStub = fetchHelper.stubFetch();
        signals = [];

        fetchStub.callsFake(async (url: URL, init: RequestInit) => {
            signals.push(init.signal!);
            await new Promise(resolve => setTimeout(resolve, 0));
            return responseFor(url.toString().includes('userId=a') ? 'a' : 'b');
        });

        const queryCache = new QueryInstanceCache();
        const firstRenders: QueryResultWithState<unknown>[] = [];
        const secondRenders: QueryResultWithState<unknown>[] = [];

        const First = () => {
            const [result] = useQuery(FakeQueryWithRequiredParameters, { userId: 'a', category: 'x' });
            firstRenders.push(result);
            return null;
        };

        const Second = () => {
            const [result] = useQuery(FakeQueryWithRequiredParameters, { userId: 'b', category: 'x' });
            secondRenders.push(result);
            return null;
        };

        const wrapper = ({ children }: { children: React.ReactNode }) =>
            React.createElement(QueryInstanceCacheContext.Provider, { value: queryCache },
                React.createElement(ArcContext.Provider, { value: config }, children));

        render(
            React.createElement(React.Fragment, null,
                React.createElement(First),
                React.createElement(Second)),
            { wrapper });

        await waitFor(() => fetchStub.should.have.been.calledTwice);
        await waitFor(() => {
            firstRenders[firstRenders.length - 1].isReady.should.be.true;
            secondRenders[secondRenders.length - 1].isReady.should.be.true;
        });
        await act(async () => {
            await new Promise(resolve => setTimeout(resolve, 0));
        });

        firstResult = firstRenders[firstRenders.length - 1];
        secondResult = secondRenders[secondRenders.length - 1];
    });

    afterEach(() => {
        fetchHelper.restore();
    });

    it('should not abort either request', () => signals.some(_ => _.aborted).should.be.false);
    it('should give the first hook its own data', () => (firstResult.data as FakeQueryWithRequiredParametersResult[]).should.deep.equal([{ id: 'a', name: 'for a' }]));
    it('should give the second hook its own data', () => (secondResult.data as FakeQueryWithRequiredParametersResult[]).should.deep.equal([{ id: 'b', name: 'for b' }]));
});
