// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, waitFor } from '@testing-library/react';
import sinon from 'sinon';
import { Paging, QueryInstanceCache, QueryResultWithState } from '@cratis/arc/queries';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';
import { useQueryWithPaging } from '../useQuery.js';
import { FakeQuery, FakeQueryResult } from '../for_useQuery/FakeQuery.js';
import { ArcContext, ArcConfiguration } from '../../ArcContext.js';
import { QueryInstanceCacheContext } from '../QueryInstanceCacheContext.js';

describe('when two hooks are on different pages', () => {
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };
    let firstPageResult: QueryResultWithState<unknown>;
    let secondPageResult: QueryResultWithState<unknown>;
    let signals: AbortSignal[];

    const config: ArcConfiguration = {
        microservice: 'test-microservice',
        apiBasePath: '/api',
        origin: 'https://example.com'
    };

    const responseFor = (page: string) => ({
        json: async () => ({
            data: [{ id: page, name: `page ${page}` }],
            isSuccess: true,
            isAuthorized: true,
            isValid: true,
            hasExceptions: false,
            validationResults: [],
            exceptionMessages: [],
            exceptionStackTrace: '',
            paging: { page: Number(page), size: 10, totalItems: 20, totalPages: 2 }
        })
    } as Response);

    beforeEach(async () => {
        fetchHelper = createFetchHelper();
        const fetchStub = fetchHelper.stubFetch();
        signals = [];

        fetchStub.callsFake((url: URL, init: RequestInit) => new Promise<Response>((resolve, reject) => {
            signals.push(init.signal!);
            init.signal?.addEventListener('abort', () =>
                reject(Object.assign(new Error('The operation was aborted'), { name: 'AbortError' })));
            const page = new URL(url.toString()).searchParams.get('page') ?? '?';
            setTimeout(() => resolve(responseFor(page)), 0);
        }));

        const queryCache = new QueryInstanceCache();
        const firstPageRenders: QueryResultWithState<unknown>[] = [];
        const secondPageRenders: QueryResultWithState<unknown>[] = [];

        const FirstPage = () => {
            const [result] = useQueryWithPaging(FakeQuery, new Paging(0, 10));
            firstPageRenders.push(result);
            return null;
        };

        const SecondPage = () => {
            const [result] = useQueryWithPaging(FakeQuery, new Paging(1, 10));
            secondPageRenders.push(result);
            return null;
        };

        const wrapper = ({ children }: { children: React.ReactNode }) =>
            React.createElement(QueryInstanceCacheContext.Provider, { value: queryCache },
                React.createElement(ArcContext.Provider, { value: config }, children));

        render(
            React.createElement(React.Fragment, null,
                React.createElement(FirstPage),
                React.createElement(SecondPage)),
            { wrapper });

        await waitFor(() => fetchStub.should.have.been.calledTwice);
        await waitFor(() => {
            firstPageRenders[firstPageRenders.length - 1].isReady.should.be.true;
            secondPageRenders[secondPageRenders.length - 1].isReady.should.be.true;
        });

        firstPageResult = firstPageRenders[firstPageRenders.length - 1];
        secondPageResult = secondPageRenders[secondPageRenders.length - 1];
    });

    afterEach(() => {
        fetchHelper.restore();
    });

    it('should not abort either request', () => signals.some(_ => _.aborted).should.be.false);
    it('should give the first hook its own page', () => (firstPageResult.data as FakeQueryResult[]).should.deep.equal([{ id: '0', name: 'page 0' }]));
    it('should give the second hook its own page', () => (secondPageResult.data as FakeQueryResult[]).should.deep.equal([{ id: '1', name: 'page 1' }]));
});
