// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryResult } from '../QueryResult.js';
import { NotAvailable } from '../../availability/AvailabilityReason.js';
import { isNotAvailable } from '../../availability/isNotAvailable.js';

describe('when the query is refused because its part is not available', () => {
    const result = new QueryResult<string>({
        isSuccess: false,
        isAuthorized: false,
        isValid: true,
        hasExceptions: false,
        exceptionMessages: [],
        exceptionStackTrace: '',
        authorizationFailureReason: NotAvailable,
        paging: { totalItems: 0, totalPages: 0, page: 0, size: 0 },
        validationResults: [],
        data: undefined
    }, String, false);

    it('should say it is not available', () => isNotAvailable(result).should.be.true);
    it('should carry the reason', () => (result.authorizationFailureReason ?? "").should.equal(NotAvailable));
    it('should not be retryable', () => (result.isRetryable === true).should.be.false);
});
