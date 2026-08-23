// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryResult } from '../../QueryResult';

describe('when constructing from a server result without readiness', () => {
    const result = new QueryResult(
        {
            data: {},
            isSuccess: true,
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

    it('should be ready', () => result.isReady.should.be.true);
});
