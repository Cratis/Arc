// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryResult } from '../../QueryResult';
import { QueryResultWithState } from '../../QueryResultWithState';

describe('when creating from query result with readiness false', () => {
    const queryResult = new QueryResult(
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

    const result = QueryResultWithState.fromQueryResult(queryResult, false);

    it('should not be ready', () => result.isReady.should.be.false);
});
