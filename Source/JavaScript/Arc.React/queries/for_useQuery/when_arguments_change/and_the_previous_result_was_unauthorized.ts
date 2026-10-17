// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act, renderHook } from '@testing-library/react';
import { useQuery } from '../../useQuery.js';
import { FakeQueryWithRequiredParameters } from '../FakeQueryWithRequiredParameters.js';
import { a_query_with_changing_arguments } from '../given/a_query_with_changing_arguments.js';
import { given } from '../../../given.js';

describe('when arguments change and the previous result was unauthorized', given(a_query_with_changing_arguments, context => {
    beforeEach(() => context.establish());
    afterEach(() => context.cleanup());

    it('should not carry the previous failure into the new request', async () => {
        const { result, rerender } = renderHook(({ userId }) => useQuery(FakeQueryWithRequiredParameters, { userId, category: 'x' }), { wrapper: context.wrapper, initialProps: { userId: 'a' } });
        await act(async () => {
            context.requests[0].resolve({ json: async () => ({
                data: [], isSuccess: false, isAuthorized: false, isValid: false,
                hasExceptions: true, validationResults: [], exceptionMessages: ['denied'], exceptionStackTrace: '',
                paging: { page: 0, size: 0, totalItems: 0, totalPages: 0 }
            }) } as Response);
            await context.nextTurn();
        });
        result.current[0].isAuthorized.should.be.false;
        result.current[0].isSuccess.should.be.false;

        rerender({ userId: 'b' });

        result.current[0].isAuthorized.should.be.true;
        result.current[0].isSuccess.should.be.true;
        result.current[0].isValid.should.be.true;
        result.current[0].hasExceptions.should.be.false;
        result.current[0].exceptionMessages.should.deep.equal([]);
        result.current[0].isReady.should.be.false;
        result.current[0].isPerforming.should.be.true;
        await context.completeRequest(1, [{ id: 'b', name: 'B' }]);
        result.current[0].isReady.should.be.true;
    });
}));
