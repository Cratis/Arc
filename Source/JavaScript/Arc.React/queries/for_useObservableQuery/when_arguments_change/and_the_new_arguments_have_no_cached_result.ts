// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useLayoutEffect } from 'react';
import { renderHook } from '@testing-library/react';
import { useObservableQuery } from '../../useObservableQuery.js';
import { ObservableQueryWithArguments } from '../given/ObservableQueryWithArguments.js';
import { an_observable_query_cache } from '../given/an_observable_query_cache.js';
import { given } from '../../../given.js';

describe('when observable arguments change and the new arguments have no cached result', given(an_observable_query_cache, context => {
    beforeEach(() => context.establish(30_000));
    afterEach(() => context.cleanup());

    it('should reset before committing and ignore pushes for the previous arguments', async () => {
        const committed: string[][] = [];
        const { result, rerender } = renderHook(({ id }) => {
            const hook = useObservableQuery<string[], ObservableQueryWithArguments>(ObservableQueryWithArguments, { id });
            useLayoutEffect(() => { committed.push(hook[0].data); });
            return hook;
        }, { wrapper: context.wrapper, initialProps: { id: 'a' } });
        await context.push(0, ['a']);
        committed.length = 0;

        rerender({ id: 'b' });

        committed[0].should.deep.equal([]);
        result.current[0].data.should.deep.equal([]);
        result.current[0].isReady.should.be.false;
        result.current[0].isPerforming.should.be.true;
        context.connectStub.secondCall.args[1].should.deep.equal({ id: 'b' });
        await context.push(0, ['late a']);
        result.current[0].data.should.deep.equal([]);
        await context.push(1, ['b']);
        result.current[0].data.should.deep.equal(['b']);
        result.current[0].isReady.should.be.true;
    });
}));
