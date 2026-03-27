// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { a_query_for } from '../given/a_query_for';
import { given } from '../../../given';
import * as sinon from 'sinon';
import { createFetchHelper } from '../../../helpers/fetchHelper';
import { QueryResult } from '../../QueryResult';

describe('with enumerable parameter descriptor values', given(a_query_for, context => {
    let result: QueryResult<string[]>;
    let fetchStub: sinon.SinonStub;
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };
    const mockResponse = {
        data: [],
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
    };

    beforeEach(async () => {
        fetchHelper = createFetchHelper();
        fetchStub = fetchHelper.stubFetch();
        fetchStub.resolves({
            json: sinon.stub().resolves(mockResponse),
            ok: true,
            status: 200
        } as unknown as Response);

        context.queryWithEnumerableParameterDescriptorValues.setOrigin('https://api.example.com');
        context.queryWithEnumerableParameterDescriptorValues.setApiBasePath('/api/v1');

        // Set enumerable values on the query instance
        context.queryWithEnumerableParameterDescriptorValues.names = ['Alice', 'Bob'];
        context.queryWithEnumerableParameterDescriptorValues.ids = [1, 2, 3];

        result = await context.queryWithEnumerableParameterDescriptorValues.perform();
    });

    afterEach(() => {
        fetchHelper.restore();
    });

    it('should return successful result', () => {
        result.isSuccess.should.be.true;
    });

    it('should include first name in URL as repeated query parameter', () => {
        const url = fetchStub.getCall(0).args[0].href;
        url.should.include('names=Alice');
    });

    it('should include second name in URL as repeated query parameter', () => {
        const url = fetchStub.getCall(0).args[0].href;
        url.should.include('names=Bob');
    });

    it('should include first id in URL as repeated query parameter', () => {
        const url = fetchStub.getCall(0).args[0].href;
        url.should.include('ids=1');
    });

    it('should include second id in URL as repeated query parameter', () => {
        const url = fetchStub.getCall(0).args[0].href;
        url.should.include('ids=2');
    });

    it('should include third id in URL as repeated query parameter', () => {
        const url = fetchStub.getCall(0).args[0].href;
        url.should.include('ids=3');
    });
}));
