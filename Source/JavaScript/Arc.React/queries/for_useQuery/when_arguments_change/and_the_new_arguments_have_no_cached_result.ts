// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useLayoutEffect } from 'react';
import { renderHook } from '@testing-library/react';
import { useQuery } from '../../useQuery.js';
import { FakeQueryWithRequiredParameters, FakeQueryWithRequiredParametersResult } from '../FakeQueryWithRequiredParameters.js';
import { a_query_with_changing_arguments } from '../given/a_query_with_changing_arguments.js';
import { given } from '../../../given.js';

describe('when arguments change and the new arguments have no cached result', given(a_query_with_changing_arguments, context => {
    beforeEach(() => context.establish());
    afterEach(() => context.cleanup());

    it('should commit the initial state while pending and then show the new data', async () => {
        const committed: FakeQueryWithRequiredParametersResult[][] = [];
        const { result, rerender } = renderHook(({ userId }) => {
            const hook = useQuery<FakeQueryWithRequiredParametersResult[], FakeQueryWithRequiredParameters>(FakeQueryWithRequiredParameters, { userId, category: 'x' });
            useLayoutEffect(() => { committed.push(hook[0].data); });
            return hook;
        }, { wrapper: context.wrapper, initialProps: { userId: 'a' } });
        await context.completeRequest(0, [{ id: 'a', name: 'A' }]);
        committed.length = 0;

        rerender({ userId: 'b' });

        committed[0].should.deep.equal([]);
        result.current[0].data.should.deep.equal([]);
        result.current[0].isReady.should.be.false;
        result.current[0].isPerforming.should.be.true;
        await context.completeRequest(1, [{ id: 'b', name: 'B' }]);
        result.current[0].data.should.deep.equal([{ id: 'b', name: 'B' }]);
        result.current[0].isReady.should.be.true;
        result.current[0].isPerforming.should.be.false;
    });
}));
