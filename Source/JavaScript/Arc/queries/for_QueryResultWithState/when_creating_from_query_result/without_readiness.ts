// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { IQueryResult } from '../../IQueryResult';
import { PagingInfo } from '../../PagingInfo';
import { QueryResultWithState } from '../../QueryResultWithState';

describe('when creating from query result without readiness', () => {
    const queryResult: IQueryResult<string> = {
        data: 'ready result',
        paging: PagingInfo.noPaging,
        isSuccess: true,
        isAuthorized: true,
        isValid: true,
        validationResults: [],
        hasExceptions: false,
        exceptionMessages: [],
        exceptionStackTrace: '',
        hasData: true,
    };

    const result = QueryResultWithState.fromQueryResult(queryResult, false);

    it('should be ready', () => result.isReady.should.be.true);
});
