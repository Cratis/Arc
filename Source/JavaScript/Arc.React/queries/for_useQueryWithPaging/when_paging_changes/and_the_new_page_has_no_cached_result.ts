// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useLayoutEffect } from 'react';
import { act, renderHook } from '@testing-library/react';
import { Paging } from '@cratis/arc/queries';
import { useQueryWithPaging } from '../../useQuery.js';
import { FakeQueryWithRequiredParameters, FakeQueryWithRequiredParametersResult } from '../../for_useQuery/FakeQueryWithRequiredParameters.js';
import { a_query_with_changing_arguments } from '../../for_useQuery/given/a_query_with_changing_arguments.js';
import { given } from '../../../given.js';

describe('when paging changes and the new page has no cached result', given(a_query_with_changing_arguments, context => {
    beforeEach(() => context.establish());
    afterEach(() => context.cleanup());

    it('should commit the initial state until the new page arrives', async () => {
        const committed: FakeQueryWithRequiredParametersResult[][] = [];
        const { result } = renderHook(() => {
            const hook = useQueryWithPaging<FakeQueryWithRequiredParametersResult[], FakeQueryWithRequiredParameters>(FakeQueryWithRequiredParameters, new Paging(0, 10), { userId: 'a', category: 'x' });
            useLayoutEffect(() => { committed.push(hook[0].data); });
            return hook;
        }, { wrapper: context.wrapper });
        await context.completeRequest(0, [{ id: 'a', name: 'page zero' }]);
        committed.length = 0;

        await act(async () => { await result.current[3](1); });

        committed[0].should.deep.equal([]);
        result.current[0].data.should.deep.equal([]);
        result.current[0].isReady.should.be.false;
        result.current[0].isPerforming.should.be.true;
        new URL(context.requests[1].url).searchParams.get('page')!.should.equal('1');
        await context.completeRequest(1, [{ id: 'a', name: 'page one' }]);
        result.current[0].data.should.deep.equal([{ id: 'a', name: 'page one' }]);
        result.current[0].isReady.should.be.true;
        result.current[0].isPerforming.should.be.false;
    });
}));
