// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { a_query_for } from '../given/a_query_for';
import { given } from '../../../given';
import * as sinon from 'sinon';
import { createFetchHelper } from '../../../helpers/fetchHelper';
import { QueryResult } from '../../QueryResult';

describe('with parameter descriptor values', given(a_query_for, context => {
    let result: QueryResult<string>;
    let fetchStub: sinon.SinonStub;
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };
    const mockResponse = {
        data: 'test-result',
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

        context.queryWithParameterDescriptorValues.setOrigin('https://api.example.com');
        context.queryWithParameterDescriptorValues.setApiBasePath('/api/v1');

        // Set values on the query instance
        context.queryWithParameterDescriptorValues.filter = 'active';
        context.queryWithParameterDescriptorValues.limit = 10;

        result = await context.queryWithParameterDescriptorValues.perform();
    });

    afterEach(() => {
        fetchHelper.restore();
    });

    it('should return successful result', () => {
        result.isSuccess.should.be.true;
    });

    it('should include parameter descriptor values in URL as query string', () => {
        fetchStub.should.have.been.calledOnce;
        const call = fetchStub.getCall(0);
        const url = call.args[0].href;
        url.should.include('filter=active');
        url.should.include('limit=10');
    });

    it('should have query string separator in URL', () => {
        const call = fetchStub.getCall(0);
        const url = call.args[0].href;
        url.should.include('?');
    });
}));
