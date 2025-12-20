// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ValidationResult } from '../../../validation/ValidationResult';
import { ValidationResultSeverity } from '../../../validation/ValidationResultSeverity';
import { CommandResult } from '../../CommandResult';

describe('when chaining callbacks and result is invalid', () => {
    const result = new CommandResult({
        correlationId: '0c0ee8c8-b5a6-4999-b030-6e6a0c931b91',
        isSuccess: false,
        isAuthorized: true,
        isValid: false,
        hasExceptions: false,
        validationResults: [
            new ValidationResult(ValidationResultSeverity.Error, 'Something went wrong', ['someProperty'], 'Something went wrong'),
            new ValidationResult(ValidationResultSeverity.Warning, 'Something is not right', ['someOtherProperty'], 'Something is not right')
        ],
        exceptionMessages: [],
        exceptionStackTrace: '',
        authorizationFailureReason: '',
        response: {}
    }, Object, false);

    let onSuccessCalled = false;
    let onFailedCalled = false;
    let receivedCommandResultOnFailed: CommandResult<object>;
    let onUnauthorizedCalled = false;
    let onValidationFailureCalled = false;
    let onExceptionCalled = false;
    let receivedValidationResults: ValidationResult[] = [];

    result
        .onSuccess(() => onSuccessCalled = true)
        .onFailed((commandResult) => {
            onFailedCalled = true;
            receivedCommandResultOnFailed = commandResult;
        })
        .onUnauthorized(() => onUnauthorizedCalled = true)
        .onValidationFailure(validationResults => {
            onValidationFailureCalled = true;
            receivedValidationResults = validationResults;
        })
        .onException(() => onExceptionCalled = true);

    it('should not call the on success callback', () => onSuccessCalled.should.be.false);
    it('should forward the validation results to the callback', () => receivedValidationResults.should.equal(result.validationResults));
    it('should call the on failed callback', () => onFailedCalled.should.be.true);
    it('should forward the command result to the on failed callback', () => receivedCommandResultOnFailed.should.equal(result));
    it('should not call the on unauthorized callback', () => onUnauthorizedCalled.should.be.false);
    it('should not call the on validation failure callback', () => onValidationFailureCalled.should.be.true);
    it('should not call the on exception callback', () => onExceptionCalled.should.be.false);
});

