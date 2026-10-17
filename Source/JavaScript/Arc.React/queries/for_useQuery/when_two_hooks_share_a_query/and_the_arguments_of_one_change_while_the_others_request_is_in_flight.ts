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

describe('when two hooks share a query and the arguments of one change while the other\'s request is in flight', () => {
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };
    let changingResult: QueryResultWithState<unknown>;
    let stayingResult: QueryResultWithState<unknown>;
    let signals: { url: string; signal: AbortSignal }[];

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
        const fetchStub = fetchHelper.stubFetch();
        signals = [];

        // Every request honors its abort signal and is held until released, so the argument change happens
        // while the staying hook's request is still in flight.
        let releaseRequests: () => void = () => { };
        const released = new Promise<void>(resolve => releaseRequests = resolve);
        fetchStub.callsFake((url: URL, init: RequestInit) => new Promise<Response>((resolve, reject) => {
            signals.push({ url: url.toString(), signal: init.signal! });
            init.signal?.addEventListener('abort', () =>
                reject(Object.assign(new Error('The operation was aborted'), { name: 'AbortError' })));
            released.then(() => resolve(responseFor(url.toString().includes('userId=b') ? 'b' : 'a')));
        }));

        const queryCache = new QueryInstanceCache();
        const changingRenders: QueryResultWithState<unknown>[] = [];
        const stayingRenders: QueryResultWithState<unknown>[] = [];

        const Changing = ({ userId }: { userId: string }) => {
            const [result] = useQuery(FakeQueryWithRequiredParameters, { userId, category: 'x' });
            changingRenders.push(result);
            return null;
        };

        const Staying = () => {
            const [result] = useQuery(FakeQueryWithRequiredParameters, { userId: 'a', category: 'x' });
            stayingRenders.push(result);
            return null;
        };

        const Page = ({ userId }: { userId: string }) =>
            React.createElement(React.Fragment, null,
                React.createElement(Changing, { userId }),
                React.createElement(Staying));

        const wrapper = ({ children }: { children: React.ReactNode }) =>
            React.createElement(QueryInstanceCacheContext.Provider, { value: queryCache },
                React.createElement(ArcContext.Provider, { value: config }, children));

        const { rerender } = render(React.createElement(Page, { userId: 'a' }), { wrapper });
        await waitFor(() => fetchStub.should.have.been.calledTwice);

        rerender(React.createElement(Page, { userId: 'b' }));
        await waitFor(() => fetchStub.should.have.been.calledThrice);

        await act(async () => {
            releaseRequests();
            await new Promise(resolve => setTimeout(resolve, 0));
        });
        await waitFor(() => {
            changingRenders[changingRenders.length - 1].isReady.should.be.true;
            stayingRenders[stayingRenders.length - 1].isReady.should.be.true;
        });

        changingResult = changingRenders[changingRenders.length - 1];
        stayingResult = stayingRenders[stayingRenders.length - 1];
    });

    afterEach(() => {
        fetchHelper.restore();
    });

    it('should not abort the staying hook\'s request', () => signals[1].signal.aborted.should.be.false);
    it('should give the staying hook the data for its own arguments', () => (stayingResult.data as FakeQueryWithRequiredParametersResult[]).should.deep.equal([{ id: 'a', name: 'for a' }]));
    it('should give the changing hook the data for its new arguments', () => (changingResult.data as FakeQueryWithRequiredParametersResult[]).should.deep.equal([{ id: 'b', name: 'for b' }]));
});
