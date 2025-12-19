// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { a_query_for } from '../given/a_query_for';
import { given } from '../../../given';

import * as sinon from 'sinon';
import { createFetchHelper } from '../../../helpers/fetchHelper';
import { QueryResult } from '../../QueryResult';

describe('with abort controller', given(a_query_for, context => {
    let result1: QueryResult<string>;
    let result2: QueryResult<string>;
    let fetchStub: sinon.SinonStub;
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };
    let firstAbortController: AbortController;
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
        // Setup fetch mock
        fetchHelper = createFetchHelper();
        fetchStub = fetchHelper.stubFetch();
        fetchStub.resolves({
            json: sinon.stub().resolves(mockResponse),
            ok: true,
            status: 200
        } as unknown as Response);

        context.query.setOrigin('https://api.example.com');

        // Call perform first time to establish abort controller
        result1 = await context.query.perform({ id: 'test-id-1' });
        firstAbortController = context.query.abortController!;

        // Call perform second time - should abort the first controller
        result2 = await context.query.perform({ id: 'test-id-2' });
    });

    afterEach(() => {
        fetchHelper.restore();
    });

    it('should create new abort controller on second call', () => {
        context.query.abortController.should.not.equal(firstAbortController);
    });

    it('should abort first controller when performing second query', () => {
        firstAbortController.signal.aborted.should.be.true;
    });

    it('should return successful results for both calls', () => {
        result1.isSuccess.should.be.true;
        result2.isSuccess.should.be.true;
    });

    it('should call fetch twice', () => {
        fetchStub.should.have.been.calledTwice;
    });
}));