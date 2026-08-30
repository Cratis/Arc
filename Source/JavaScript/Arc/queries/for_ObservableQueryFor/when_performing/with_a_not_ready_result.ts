// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as sinon from 'sinon';
import { given } from '../../../given';
import { createFetchHelper } from '../../../helpers/fetchHelper';
import type { QueryResult } from '../../QueryResult';
import { an_observable_query_for } from '../given/an_observable_query_for';

describe(
    'when performing with a not ready result',
    given(an_observable_query_for, (context) => {
        let result: QueryResult<string>;
        let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };

        beforeEach(async () => {
            fetchHelper = createFetchHelper();
            const fetchStub = fetchHelper.stubFetch();
            // SAFETY: The query only consumes this response's JSON payload, status, and success flag.
            fetchStub.resolves({
                json: sinon.stub().resolves({
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
                }),
                ok: true,
                status: 202,
            } as unknown as Response);

            context.query.setOrigin('https://api.example.com');
            result = await context.query.perform({ id: 'test-id' });
        });

        afterEach(() => fetchHelper.restore());

        it('should return the not ready result', () => result.isReady.should.be.false);
        it('should not report an exception', () => result.hasExceptions.should.be.false);
    }),
);
