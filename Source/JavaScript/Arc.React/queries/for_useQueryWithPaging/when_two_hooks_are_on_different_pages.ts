// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { waitFor } from '@testing-library/react';
import { Paging, QueryResultWithState } from '@cratis/arc/queries';
import { useQueryWithPaging } from '../useQuery.js';
import { FakeQuery, FakeQueryResult } from '../for_useQuery/FakeQuery.js';
import { a_shared_query_instance_cache } from '../for_useQuery/given/a_shared_query_instance_cache.js';
import { given } from '../../given.js';

describe('when two hooks are on different pages', given(a_shared_query_instance_cache, context => {
    let firstPageResult: QueryResultWithState<unknown>;
    let secondPageResult: QueryResultWithState<unknown>;
    let signals: AbortSignal[];

    beforeEach(async () => {
        context.establish();
        signals = [];

        context.fetchStub.callsFake((url: URL, requestInit: RequestInit) => new Promise<Response>((resolve, reject) => {
            signals.push(requestInit.signal!);
            requestInit.signal?.addEventListener('abort', () => reject(context.abortError()));
            const page = new URL(url.toString()).searchParams.get('page') ?? '?';
            const response = context.successfulResponseWith(
                [{ id: page, name: `page ${page}` }],
                { page: Number(page), size: 10, totalItems: 20, totalPages: 2 });
            setTimeout(() => resolve(response), 0);
        }));

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

        context.render(
            React.createElement(React.Fragment, null,
                React.createElement(FirstPage),
                React.createElement(SecondPage)));

        await waitFor(() => context.fetchStub.should.have.been.calledTwice);
        await waitFor(() => {
            firstPageRenders[firstPageRenders.length - 1].isReady.should.be.true;
            secondPageRenders[secondPageRenders.length - 1].isReady.should.be.true;
        });

        firstPageResult = firstPageRenders[firstPageRenders.length - 1];
        secondPageResult = secondPageRenders[secondPageRenders.length - 1];
    });

    afterEach(() => context.cleanup());

    it('should not abort either request', () => signals.some(signal => signal.aborted).should.be.false);
    it('should give the first hook its own page', () => (firstPageResult.data as FakeQueryResult[]).should.deep.equal([{ id: '0', name: 'page 0' }]));
    it('should give the second hook its own page', () => (secondPageResult.data as FakeQueryResult[]).should.deep.equal([{ id: '1', name: 'page 1' }]));
}));
