// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryResult } from '../QueryResult.js';
import { isNotAvailable } from '../../availability/isNotAvailable.js';

describe('when the server reports a retryable failure for a query', () => {
    const result = new QueryResult<string>({
        isSuccess: false,
        isAuthorized: true,
        isValid: true,
        hasExceptions: true,
        exceptionMessages: ['Availability could not be determined. Retry the request.'],
        exceptionStackTrace: '',
        isRetryable: true,
        paging: { totalItems: 0, totalPages: 0, page: 0, size: 0 },
        validationResults: [],
        data: undefined
    }, String, false);

    it('should be retryable', () => (result.isRetryable === true).should.be.true);
    it('should not be a denial', () => result.isAuthorized.should.be.true);
    it('should not say it is not available', () => isNotAvailable(result).should.be.false);
});
