// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandResult } from '../../CommandResult';

describe('when chaining callbacks and result has exceptions', () => {
    const result = new CommandResult({
        correlationId: '0c0ee8c8-b5a6-4999-b030-6e6a0c931b91',
        isSuccess: false,
        isAuthorized: true,
        isValid: true,
        hasExceptions: true,
        validationResults: [],
        exceptionMessages: [
            'Something went wrong',
            'Something is not right'
        ],
        exceptionStackTrace: 'Some stack trace',
        authorizationFailureReason: '',
        response: {}
    }, Object, false);

    let onSuccessCalled = false;
    let onFailedCalled = false;
    let receivedCommandResultOnFailed: CommandResult<object>;
    let onUnauthorizedCalled = false;
    let onValidationFailureCalled = false;
    let onExceptionCalled = false;
    let receivedMessages: string[] = [];
    let receivedStackTrace: string = '';

    result
        .onSuccess(() => onSuccessCalled = true)
        .onFailed((commandResult) => {
            onFailedCalled = true;
            receivedCommandResultOnFailed = commandResult;
        })
        .onUnauthorized(() => onUnauthorizedCalled = true)
        .onValidationFailure(() => onValidationFailureCalled = true)
        .onException((messages, stackTrace) => {
            onExceptionCalled = true;
            receivedMessages = messages;
            receivedStackTrace = stackTrace;
        });

    it('should not call the on success callback', () => onSuccessCalled.should.be.false);
    it('should forward the exception messages to the callback', () => receivedMessages.should.equal(result.exceptionMessages));
    it('should forward the exception stack trace to the callback', () => receivedStackTrace.should.equal(result.exceptionStackTrace));
    it('should call the on failed callback', () => onFailedCalled.should.be.true);
    it('should forward the command result to the on failed callback', () => receivedCommandResultOnFailed.should.equal(result));
    it('should not call the on unauthorized callback', () => onUnauthorizedCalled.should.be.false);
    it('should not call the on validation failure callback', () => onValidationFailureCalled.should.be.false);
    it('should call the on exception callback', () => onExceptionCalled.should.be.true);
});

