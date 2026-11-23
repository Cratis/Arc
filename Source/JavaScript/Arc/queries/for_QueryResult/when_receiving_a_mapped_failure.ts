// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from 'chai';
import { QueryResult } from '../QueryResult.js';
import { QueryResultWithState } from '../QueryResultWithState.js';
import { PagingInfo } from '../PagingInfo.js';

describe('when receiving a mapped query failure', () => {
    const serverResult = {
        data: {},
        isSuccess: false,
        isAuthorized: true,
        isValid: true,
        hasExceptions: true,
        validationResults: [],
        exceptionMessages: ['An unexpected error occurred.'],
        exceptionStackTrace: '',
        paging: { page: 0, size: 0, totalItems: 0, totalPages: 0 },
    };

    it('should preserve the code through deserialization and state conversion', () => {
        const result = new QueryResult({ ...serverResult, errorCode: 'namespace_recovery_required' }, Object, false);
        const state = QueryResultWithState.fromQueryResult(result, false);
        result.errorCode!.should.equal('namespace_recovery_required');
        state.errorCode!.should.equal(result.errorCode);
        state.isRetryable.should.be.false;
    });

    it('should tolerate an older server without a code', () =>
        expect(new QueryResult(serverResult, Object, false).errorCode).to.be.undefined);

    it('should keep every existing positional constructor argument', () => {
        const state = new QueryResultWithState({}, PagingInfo.noPaging, false, false, true, [], false, [], '', false, undefined, false, 'denied', true);
        state.isReady.should.be.false;
        state.authorizationFailureReason.should.equal('denied');
        state.isRetryable.should.be.true;
        expect(state.errorCode).to.be.undefined;
    });

    it('should clear the code when a later successful result arrives', () => {
        const result = new QueryResult({ ...serverResult, isSuccess: true, hasExceptions: false, exceptionMessages: [] }, Object, false);
        expect(QueryResultWithState.fromQueryResult(result, false).errorCode).to.be.undefined;
    });
});
