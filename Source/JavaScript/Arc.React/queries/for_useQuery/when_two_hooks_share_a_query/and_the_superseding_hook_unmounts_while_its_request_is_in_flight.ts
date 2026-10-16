// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, waitFor, act } from '@testing-library/react';
import sinon from 'sinon';
import { QueryInstanceCache, QueryResultWithState } from '@cratis/arc/queries';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';
import { useQuery } from '../../useQuery.js';
import { FakeQuery, FakeQueryResult } from '../FakeQuery.js';
import { ArcContext, ArcConfiguration } from '../../../ArcContext.js';
import { QueryInstanceCacheContext } from '../../QueryInstanceCacheContext.js';

describe('when two hooks share a query and the superseding hook unmounts while its request is in flight', () => {
    let fetchStub: sinon.SinonStub;
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };
    let remainingResult: QueryResultWithState<unknown>;

    const config: ArcConfiguration = {
        microservice: 'test-microservice',
        apiBasePath: '/api',
        origin: 'https://example.com'
    };

    const successfulResponse = {
        json: async () => ({
            data: [{ id: '1', name: 'shared' }],
            isSuccess: true,
            isAuthorized: true,
            isValid: true,
            hasExceptions: false,
            validationResults: [],
            exceptionMessages: [],
            exceptionStackTrace: '',
            paging: { page: 0, size: 0, totalItems: 0, totalPages: 0 }
        })
    } as Response;

    beforeEach(async () => {
        fetchHelper = createFetchHelper();
        fetchStub = fetchHelper.stubFetch();

        fetchStub.onFirstCall().callsFake((_url: URL, init: RequestInit) => new Promise<Response>((_resolve, reject) => {
            init.signal?.addEventListener('abort', () =>
                reject(Object.assign(new Error('The operation was aborted'), { name: 'AbortError' })));
        }));

        // The superseding request is held open until its hook has unmounted.
        let completeSupersedingRequest: (response: Response) => void = () => { };
        fetchStub.onSecondCall().returns(new Promise<Response>(resolve => {
            completeSupersedingRequest = resolve;
        }));

        const queryCache = new QueryInstanceCache();
        const remainingRenders: QueryResultWithState<unknown>[] = [];

        const Remaining = () => {
            const [result] = useQuery(FakeQuery);
            remainingRenders.push(result);
            return null;
        };

        const Superseding = () => {
            useQuery(FakeQuery);
            return null;
        };

        const Page = ({ showSuperseding }: { showSuperseding: boolean }) =>
            React.createElement(React.Fragment, null,
                React.createElement(Remaining),
                showSuperseding ? React.createElement(Superseding) : null);

        const wrapper = ({ children }: { children: React.ReactNode }) =>
            React.createElement(QueryInstanceCacheContext.Provider, { value: queryCache },
                React.createElement(ArcContext.Provider, { value: config }, children));

        const { rerender } = render(React.createElement(Page, { showSuperseding: true }), { wrapper });

        await waitFor(() => fetchStub.should.have.been.calledTwice);

        rerender(React.createElement(Page, { showSuperseding: false }));

        await act(async () => {
            completeSupersedingRequest(successfulResponse);
            await new Promise(resolve => setTimeout(resolve, 0));
        });

        await waitFor(() => remainingRenders[remainingRenders.length - 1].isReady.should.be.true);

        remainingResult = remainingRenders[remainingRenders.length - 1];
    });

    afterEach(() => {
        fetchHelper.restore();
    });

    it('should give the remaining hook the shared data', () => (remainingResult.data as FakeQueryResult[]).should.deep.equal([{ id: '1', name: 'shared' }]));
    it('should stop the remaining hook performing', () => remainingResult.isPerforming.should.be.false);
});
