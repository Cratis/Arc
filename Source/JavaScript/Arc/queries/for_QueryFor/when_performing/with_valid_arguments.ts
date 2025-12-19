// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { a_query_for } from '../given/a_query_for';
import { given } from '../../../given';
import * as sinon from 'sinon';
import { createFetchHelper } from '../../../helpers/fetchHelper';
import { QueryResult } from '../../QueryResult';

describe('with valid arguments', given(a_query_for, context => {
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
        // Setup fetch mock using helper
        fetchHelper = createFetchHelper();
        fetchStub = fetchHelper.stubFetch();
        fetchStub.resolves({
            json: sinon.stub().resolves(mockResponse),
            ok: true,
            status: 200
        } as unknown as Response);

        context.query.setOrigin('https://api.example.com');
        context.query.setApiBasePath('/api/v1');
        context.query.setMicroservice('test-service');

        // Call perform with valid arguments
        result = await context.query.perform({ id: 'test-id' });
    });

    afterEach(() => {
        fetchHelper.restore();
    });

    it('should return successful result', () => {
        result.isSuccess.should.be.true;
    });

    it('should return correct data', () => {
        result.data.should.equal('test-result');
    });

    it('should have data', () => {
        result.hasData.should.be.true;
    });

    it('should call fetch with correct URL', () => {
        fetchStub.should.have.been.calledOnce;
        const call = fetchStub.getCall(0);
        call.args[0].href.should.equal('https://api.example.com/api/v1/api/test/test-id');
    });

    it('should call fetch with correct headers', () => {
        const call = fetchStub.getCall(0);
        const options = call.args[1];
        options.headers['Accept'].should.equal('application/json');
        options.headers['Content-Type'].should.equal('application/json');
        options.headers['x-cratis-microservice'].should.equal('test-service');
    });

    it('should call fetch with GET method', () => {
        const call = fetchStub.getCall(0);
        const options = call.args[1];
        options.method.should.equal('GET');
    });

    it('should set abort controller signal', () => {
        const call = fetchStub.getCall(0);
        const options = call.args[1];
        options.signal.should.not.be.undefined;
    });
}));