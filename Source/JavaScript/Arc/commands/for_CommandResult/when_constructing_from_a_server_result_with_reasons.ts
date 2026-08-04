// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ValidationResultReason } from '../../validation/ValidationResultReason';
import { ValidationResultSeverity } from '../../validation/ValidationResultSeverity';
import { CommandResult } from '../CommandResult';

describe('when constructing from a server result with reasons', () => {
    const result = new CommandResult({
        correlationId: '0c0ee8c8-b5a6-4999-b030-6e6a0c931b91',
        isSuccess: false,
        isAuthorized: true,
        isValid: false,
        hasExceptions: false,
        validationResults: [
            {
                severity: ValidationResultSeverity.Error,
                message: 'Concurrency violation for event source abc: Expected sequence number 10, but actual is 15',
                members: [],
                state: {},
                reason: ValidationResultReason.ConcurrencyViolation
            },
            {
                severity: ValidationResultSeverity.Error,
                message: 'Name is required',
                members: ['name'],
                state: {}
            }
        ],
        exceptionMessages: [],
        exceptionStackTrace: '',
        authorizationFailureReason: '',
        response: null
    }, String, false);

    it('should carry the reason across the wire', () =>
        result.validationResults[0].reason.should.equal(ValidationResultReason.ConcurrencyViolation));

    // A server that predates the reason sends nothing, and an authored rule rejection is what that used to mean.
    it('should treat a missing reason as an authored rule', () =>
        result.validationResults[1].reason.should.equal(ValidationResultReason.Rule));

    it('should let a client separate the two without reading the message', () =>
        result.validationResults
            .filter(_ => _.reason === ValidationResultReason.ConcurrencyViolation)
            .length.should.equal(1));
});
