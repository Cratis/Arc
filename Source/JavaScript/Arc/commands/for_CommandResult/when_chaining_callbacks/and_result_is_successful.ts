// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandResult } from '../../CommandResult';

describe('when chaining callbacks and result is successful', () => {
    const result = new CommandResult<string>({
        correlationId: '0c0ee8c8-b5a6-4999-b030-6e6a0c931b91',
        isSuccess: true,
        isAuthorized: true,
        isValid: true,
        hasExceptions: false,
        validationResults: [],
        exceptionMessages: [],
        exceptionStackTrace: '',
        authorizationFailureReason: '',
        response: 'The ultimate response'
    }, Object, false);

    let onSuccessCalled = false;
    let onFailedCalled = false;
    let onUnauthorizedCalled = false;
    let onValidationFailureCalled = false;
    let onExceptionCalled = false;
    let receivedResponse: string | null = null;

    result
        .onSuccess(response => {
            onSuccessCalled = true;
            receivedResponse = response as string;
        })
        .onFailed(() => onFailedCalled = true)
        .onUnauthorized(() => onUnauthorizedCalled = true)
        .onValidationFailure(() => onValidationFailureCalled = true)
        .onException(() => onExceptionCalled = true);

    it('should call the on success callback', () => onSuccessCalled.should.be.true);
    it('should pass the response to the callback', () => receivedResponse!.should.equal(result.response));
    it('should not call the on failed callback', () => onFailedCalled.should.be.false);
    it('should not call the on unauthorized callback', () => onUnauthorizedCalled.should.be.false);
    it('should not call the on validation failure callback', () => onValidationFailureCalled.should.be.false);
    it('should not call the on exception callback', () => onExceptionCalled.should.be.false);
});
