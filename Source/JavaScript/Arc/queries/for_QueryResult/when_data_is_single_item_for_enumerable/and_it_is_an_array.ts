// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryResult } from '../../QueryResult';

/* eslint-disable @typescript-eslint/no-explicit-any */

describe('when data is single item for enumerable and it is an array', () => {
    const queryResult = new QueryResult<any>({
        isSuccess: true,
        isAuthorized: true,
        isValid: true,
        hasExceptions: false,
        exceptionMessages: [],
        exceptionStackTrace: '',
        paging: {
            totalItems: 1,
            totalPages: 1,
            page: 0,
            size: 0
        },
        validationResults: [],
        data: [{ id: '1', name: 'Single Item' }]
    }, Object, true);

    it('should have data as an array', () => Array.isArray(queryResult.data).should.be.true);
    it('should have one item', () => (queryResult.data as any[]).length.should.equal(1));
});
