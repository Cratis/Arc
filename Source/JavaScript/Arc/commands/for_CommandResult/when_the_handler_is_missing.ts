// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandResult } from '../CommandResult.js';

const result = new CommandResult({
    correlationId: '00000000-0000-0000-0000-000000000000',
    isSuccess: false,
    isAuthorized: true,
    isValid: true,
    hasExceptions: true,
    validationResults: [],
    exceptionMessages: ['No handler found for command of type Example'],
    exceptionStackTrace: '',
    authorizationFailureReason: '',
    isMissingHandler: true,
    response: null
}, Object, false);

describe('when the handler is missing', () => {
    it('should identify the missing handler', () => result.isMissingHandler.should.be.true);
    it('should still carry exceptions', () => result.hasExceptions.should.be.true);
    it('should not be successful', () => result.isSuccess.should.be.false);
    it('should not be a denial', () => result.isAuthorized.should.be.true);
    it('should not be retryable', () => result.isRetryable.should.be.false);
});
