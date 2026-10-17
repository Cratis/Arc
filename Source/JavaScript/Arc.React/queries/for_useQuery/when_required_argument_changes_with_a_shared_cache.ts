// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { renderHook, waitFor } from '@testing-library/react';
import { useQuery } from '../useQuery.js';
import { FakeQueryWithRequiredParameters, FakeQueryWithRequiredParametersResult } from './FakeQueryWithRequiredParameters.js';
import { a_shared_query_instance_cache } from './given/a_shared_query_instance_cache.js';
import { given } from '../../given.js';

describe('when a required argument changes with a shared cache', given(a_shared_query_instance_cache, context => {
    const keyFor = (userId: string) => context.queryCache.buildKey(FakeQueryWithRequiredParameters.name, { userId, category: 'x' });

    beforeEach(async () => {
        context.establish();
        context.fetchStub.callsFake(async (url: URL) => {
            const userId = url.toString().includes('userId=b') ? 'b' : 'a';
            return context.successfulResponseWith([{ id: userId, name: `for ${userId}` }]);
        });

        const { result, rerender } = renderHook(
            ({ userId }: { userId: string }) => useQuery(FakeQueryWithRequiredParameters, { userId, category: 'x' }),
            { wrapper: context.wrapper, initialProps: { userId: 'a' } });

        await waitFor(() => (result.current[0].data as FakeQueryWithRequiredParametersResult[]).should.deep.equal([{ id: 'a', name: 'for a' }]));
        rerender({ userId: 'b' });
        await waitFor(() => (result.current[0].data as FakeQueryWithRequiredParametersResult[]).should.deep.equal([{ id: 'b', name: 'for b' }]));
    });

    afterEach(() => context.cleanup());

    it('should store the new result under the key for the new arguments', () => (context.queryCache.getLastResult(keyFor('b'))!.data as FakeQueryWithRequiredParametersResult[]).should.deep.equal([{ id: 'b', name: 'for b' }]));
    it('should keep the previous result under the key for the previous arguments', () => (context.queryCache.getLastResult(keyFor('a'))!.data as FakeQueryWithRequiredParametersResult[]).should.deep.equal([{ id: 'a', name: 'for a' }]));
}));
