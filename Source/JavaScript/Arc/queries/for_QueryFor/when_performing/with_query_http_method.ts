// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { a_query_for } from '../given/a_query_for';
import { given } from '../../../given';
import * as sinon from 'sinon';
import { createFetchHelper } from '../../../helpers/fetchHelper';
import { QueryResult } from '../../QueryResult';
import { QueryHttpMethod } from '../../QueryHttpMethod';

describe('with query http method', given(a_query_for, context => {
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
        paging: { totalItems: 0, totalPages: 0, page: 0, size: 0 }
    };

    beforeEach(async () => {
        fetchHelper = createFetchHelper();
        fetchStub = fetchHelper.stubFetch();
        fetchStub.resolves({
            json: sinon.stub().resolves(mockResponse),
            ok: true,
            status: 200
        } as unknown as Response);

        context.query.setOrigin('https://api.example.com');
        context.query.setApiBasePath('/api/v1');
        context.query.setHttpMethod(QueryHttpMethod.Query);

        result = await context.query.perform({ id: 'test-id' });
    });

    afterEach(() => {
        fetchHelper.restore();
    });

    it('should return successful result', () => result.isSuccess.should.be.true);

    it('should call fetch with QUERY method', () => {
        const options = fetchStub.getCall(0).args[1];
        options.method.should.equal('QUERY');
    });

    it('should keep the route parameter in the path', () => {
        const call = fetchStub.getCall(0);
        call.args[0].href.should.equal('https://api.example.com/api/v1/api/test/test-id');
    });

    it('should send a JSON request body', () => {
        const options = fetchStub.getCall(0).args[1];
        (options.body === undefined).should.be.false;
    });
}));
