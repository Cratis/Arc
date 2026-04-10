// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryResult } from '../../QueryResult';
import { QueryResultWithState } from '../../QueryResultWithState';

describe('when creating from query result with null data', () => {
    const queryResult = new QueryResult<object>({
        data: null as unknown as object,
        isSuccess: true,
        isAuthorized: true,
        isValid: true,
        hasExceptions: false,
        validationResults: [],
        exceptionMessages: [],
        exceptionStackTrace: '',
        paging: {
            totalItems: 0,
            totalPages: 0,
            page: 0,
            size: 0
        }
    }, Object, true);

    const result = QueryResultWithState.fromQueryResult(queryResult, false);

    it('should have data that is not undefined', () => (result.data !== undefined).should.be.true);
    it('should have data that is not null', () => (result.data !== null).should.be.true);
    it('should not have data', () => result.hasData.should.be.false);
});
