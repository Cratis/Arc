// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandResult } from '../CommandResult.js';
import { NotAvailable } from '../../availability/AvailabilityReason.js';

describe('when the command is refused because its part is not available', () => {
    const result = new CommandResult({
        correlationId: '00000000-0000-0000-0000-000000000000',
        isSuccess: false,
        isAuthorized: false,
        isValid: true,
        hasExceptions: false,
        validationResults: [],
        exceptionMessages: [],
        exceptionStackTrace: '',
        authorizationFailureReason: NotAvailable,
        response: null
    }, Object, false);
    let notAvailableCalled = false;
    let unauthorizedCalled = false;
    result.onNotAvailable(() => notAvailableCalled = true).onUnauthorized(() => unauthorizedCalled = true);

    it('should say it is not available', () => result.isNotAvailable.should.be.true);
    it('should not be a retryable failure', () => result.isRetryable.should.be.false);
    it('should call the not available callback', () => notAvailableCalled.should.be.true);
    it('should still count as unauthorized', () => unauthorizedCalled.should.be.true);
});
