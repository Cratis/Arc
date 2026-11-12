// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandResult } from '../CommandResult.js';

describe('when the server reports a retryable failure', () => {
    const result = new CommandResult({
        correlationId: '00000000-0000-0000-0000-000000000000',
        isSuccess: false,
        isAuthorized: true,
        isValid: true,
        hasExceptions: true,
        validationResults: [],
        exceptionMessages: ['Availability could not be determined. Retry the request.'],
        exceptionStackTrace: '',
        authorizationFailureReason: '',
        isRetryable: true,
        response: null
    }, Object, false);

    it('should be retryable', () => result.isRetryable.should.be.true);
    it('should not be a denial', () => result.isAuthorized.should.be.true);
    it('should not say it is not available', () => result.isNotAvailable.should.be.false);
});
