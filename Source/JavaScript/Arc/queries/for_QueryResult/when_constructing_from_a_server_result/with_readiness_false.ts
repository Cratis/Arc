// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryResult } from '../../QueryResult';

describe('when constructing from a server result with readiness false', () => {
    const result = new QueryResult(
        {
            data: null,
            isSuccess: false,
            isReady: false,
            isAuthorized: true,
            isValid: true,
            hasExceptions: false,
            validationResults: [],
            exceptionMessages: [],
            exceptionStackTrace: '',
            paging: { page: 0, size: 0, totalItems: 0, totalPages: 0 },
        },
        Object,
        false,
    );

    it('should not be ready', () => result.isReady.should.be.false);
});
