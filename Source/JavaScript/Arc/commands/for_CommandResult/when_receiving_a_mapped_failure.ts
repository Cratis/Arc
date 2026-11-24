// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from 'chai';
import { CommandResult } from '../CommandResult.js';

describe('when receiving a mapped command failure', () => {
    const serverResult = {
        correlationId: '0c0ee8c8-b5a6-4999-b030-6e6a0c931b91',
        isSuccess: false,
        isAuthorized: true,
        isValid: true,
        hasExceptions: true,
        validationResults: [],
        exceptionMessages: ['An unexpected error occurred.'],
        exceptionStackTrace: '',
        authorizationFailureReason: '',
        response: null,
    };

    it('should carry the stable public code', () => {
        const result = new CommandResult({ ...serverResult, errorCode: 'namespace_closing' }, Object, false);
        result.errorCode!.should.equal('namespace_closing');
        result.isRetryable.should.be.false;
    });

    it('should tolerate an older server without a code', () =>
        expect(new CommandResult(serverResult, Object, false).errorCode).to.be.undefined);

    it('should not carry the code into a later successful result', () =>
        expect(new CommandResult({ ...serverResult, isSuccess: true, hasExceptions: false, exceptionMessages: [] }, Object, false).errorCode).to.be.undefined);
});
