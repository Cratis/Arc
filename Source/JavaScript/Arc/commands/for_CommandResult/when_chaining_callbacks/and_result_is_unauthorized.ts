// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandResult } from '../../CommandResult';

describe('when chaining callbacks and result is unauthorized', () => {
    const result = new CommandResult({
        correlationId: '0c0ee8c8-b5a6-4999-b030-6e6a0c931b91',
        isSuccess: false,
        isAuthorized: false,
        isValid: true,
        hasExceptions: false,
        validationResults: [],
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

    result
        .onSuccess(() => onSuccessCalled = true)
        .onFailed((commandResult) => {
            onFailedCalled = true;
            receivedCommandResultOnFailed = commandResult;
        })
        .onUnauthorized(() => onUnauthorizedCalled = true)
        .onValidationFailure(() => onValidationFailureCalled = true)
        .onException(() => onExceptionCalled = true);

    it('should not the on success callback', () => onSuccessCalled.should.be.false);
    it('should call the on failed callback', () => onFailedCalled.should.be.true);
    it('should forward the command result to the on failed callback', () => receivedCommandResultOnFailed.should.equal(result));
    it('should call the on unauthorized callback', () => onUnauthorizedCalled.should.be.true);
    it('should not call the on validation failure callback', () => onValidationFailureCalled.should.be.false);
    it('should not call the on exception callback', () => onExceptionCalled.should.be.false);
});

