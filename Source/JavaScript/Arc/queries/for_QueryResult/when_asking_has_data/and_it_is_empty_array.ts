// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryResult } from '../../QueryResult';

/* eslint-disable @typescript-eslint/no-explicit-any */

describe("when asking has data and it is empty array", () => {
    const queryResult = new QueryResult<any>({
        isSuccess: true,
        isAuthorized: true,
        isValid: true,
        hasExceptions: false,
        exceptionMessages: [],
        exceptionStackTrace: '',
        paging: {
            totalItems: 0,
            totalPages: 0,
            page: 0,
            size: 0
        },
        validationResults: [],
        data: []
    }, Object, true);

    it('should consider to not having data', () => queryResult.hasData.should.be.false);
});
