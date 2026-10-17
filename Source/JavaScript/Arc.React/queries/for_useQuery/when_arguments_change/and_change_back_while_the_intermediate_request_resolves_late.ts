// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { renderHook } from '@testing-library/react';
import { useQuery } from '../../useQuery.js';
import { FakeQueryWithRequiredParameters, FakeQueryWithRequiredParametersResult } from '../FakeQueryWithRequiredParameters.js';
import { a_query_with_changing_arguments } from '../given/a_query_with_changing_arguments.js';
import { given } from '../../../given.js';

describe('when arguments change back while the intermediate request resolves late', given(a_query_with_changing_arguments, context => {
    beforeEach(() => context.establish());
    afterEach(() => context.cleanup());

    it('should keep A displayed and cache the late B result under B only', async () => {
        const { result, rerender } = renderHook(({ userId }) => useQuery<FakeQueryWithRequiredParametersResult[], FakeQueryWithRequiredParameters>(FakeQueryWithRequiredParameters, { userId, category: 'x' }), { wrapper: context.wrapper, initialProps: { userId: 'a' } });
        await context.completeRequest(0, [{ id: 'a', name: 'cached A' }]);
        rerender({ userId: 'b' });
        result.current[0].data.should.deep.equal([]);

        rerender({ userId: 'a' });
        result.current[0].data.should.deep.equal([{ id: 'a', name: 'cached A' }]);
        result.current[0].isPerforming.should.be.true;
        await context.completeRequest(2, [{ id: 'a', name: 'fresh A' }]);
        const aCache = context.queryCache.getLastResult(context.keyFor('a'));

        await context.completeRequest(1, [{ id: 'b', name: 'late B' }]);

        result.current[0].data.should.deep.equal([{ id: 'a', name: 'fresh A' }]);
        result.current[0].isPerforming.should.be.false;
        context.queryCache.getLastResult(context.keyFor('a'))!.should.equal(aCache);
        context.queryCache.getLastResult<FakeQueryWithRequiredParametersResult[]>(context.keyFor('a'))!.data.should.deep.equal([{ id: 'a', name: 'fresh A' }]);
        context.queryCache.getLastResult<FakeQueryWithRequiredParametersResult[]>(context.keyFor('b'))!.data.should.deep.equal([{ id: 'b', name: 'late B' }]);
    });
}));
