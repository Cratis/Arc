// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { renderHook, waitFor } from '@testing-library/react';
import { QueryResultWithState } from '@cratis/arc/queries';
import { useQuery } from '../useQuery.js';
import { FakeQuery, FakeQueryResult } from './FakeQuery.js';
import { a_shared_query_instance_cache } from './given/a_shared_query_instance_cache.js';
import { given } from '../../given.js';

describe('when an optional argument changes', given(a_shared_query_instance_cache, context => {
    let requestedUrls: string[];
    let result: QueryResultWithState<FakeQueryResult[]>;

    beforeEach(async () => {
        context.establish();
        requestedUrls = [];

        // FakeQuery requires no arguments, so the filter is optional.
        context.fetchStub.callsFake(async (url: URL) => {
            requestedUrls.push(url.toString());
            const filter = new URL(url.toString()).searchParams.get('filter') ?? 'none';
            return context.successfulResponseWith([{ id: filter, name: `filtered by ${filter}` }]);
        });

        const { result: hook, rerender } = renderHook(
            ({ filter }: { filter: string }) => useQuery<FakeQueryResult[], FakeQuery, { filter: string }>(FakeQuery, { filter }),
            { wrapper: context.wrapper, initialProps: { filter: 'first' } });

        await waitFor(() => hook.current[0].data.should.deep.equal([{ id: 'first', name: 'filtered by first' }]));
        const resultForFirstArgument = hook.current[0];
        rerender({ filter: 'second' });
        await waitFor(() => context.fetchStub.should.have.been.calledTwice);
        await waitFor(() => hook.current[0].should.not.equal(resultForFirstArgument));
        await context.letPendingWorkSettle();

        result = hook.current[0];
    });

    afterEach(() => context.cleanup());

    it('should fetch again', () => context.fetchStub.should.have.been.calledTwice);
    it('should send the new argument', () => new URL(requestedUrls[1]).searchParams.get('filter')!.should.equal('second'));
    it('should settle on the result for the new argument', () => result.data.should.deep.equal([{ id: 'second', name: 'filtered by second' }]));
}));
