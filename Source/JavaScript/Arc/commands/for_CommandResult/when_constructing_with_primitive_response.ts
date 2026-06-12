// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandResult } from '../CommandResult';

describe('when constructing with primitive response', () => {
    const stringResult = new CommandResult<string>({
        correlationId: '0c0ee8c8-b5a6-4999-b030-6e6a0c931b91',
        isSuccess: true,
        isAuthorized: true,
        isValid: true,
        hasExceptions: false,
        validationResults: [],
        exceptionMessages: [],
        exceptionStackTrace: '',
        authorizationFailureReason: '',
        response: 'Handled: TestName'
    }, String, false);

    const zeroResult = new CommandResult<number>({
        correlationId: '0c0ee8c8-b5a6-4999-b030-6e6a0c931b91',
        isSuccess: true,
        isAuthorized: true,
        isValid: true,
        hasExceptions: false,
        validationResults: [],
        exceptionMessages: [],
        exceptionStackTrace: '',
        authorizationFailureReason: '',
        response: 0
    }, Number, false);

    const falseResult = new CommandResult<boolean>({
        correlationId: '0c0ee8c8-b5a6-4999-b030-6e6a0c931b91',
        isSuccess: true,
        isAuthorized: true,
        isValid: true,
        hasExceptions: false,
        validationResults: [],
        exceptionMessages: [],
        exceptionStackTrace: '',
        authorizationFailureReason: '',
        response: false
    }, Boolean, false);

    const stringArrayResult = new CommandResult<string[]>({
        correlationId: '0c0ee8c8-b5a6-4999-b030-6e6a0c931b91',
        isSuccess: true,
        isAuthorized: true,
        isValid: true,
        hasExceptions: false,
        validationResults: [],
        exceptionMessages: [],
        exceptionStackTrace: '',
        authorizationFailureReason: '',
        response: ['one', 'two']
    }, String, true);

    it('should preserve string response', () => stringResult.response!.should.equal('Handled: TestName'));
    it('should preserve zero response', () => zeroResult.response!.should.equal(0));
    it('should preserve false response', () => falseResult.response!.should.equal(false));
    it('should preserve primitive array response', () => stringArrayResult.response!.should.deep.equal(['one', 'two']));
});
