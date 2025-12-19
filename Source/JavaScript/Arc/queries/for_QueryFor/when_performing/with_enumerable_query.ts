// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { a_query_for } from '../given/a_query_for';
import { given } from '../../../given';

import * as sinon from 'sinon';
import { createFetchHelper } from '../../../helpers/fetchHelper';
import { QueryResult } from '../../QueryResult';

describe('with enumerable query', given(a_query_for, context => {
    let result: QueryResult<string[]>;
    let fetchStub: sinon.SinonStub;
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };
    const mockResponse = {
        data: ['item1', 'item2'],
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

        context.enumerableQuery.setOrigin('https://api.example.com');

        // Call perform with valid arguments
        result = await context.enumerableQuery.perform({ category: 'test-category' });
    });

    afterEach(() => {
        fetchHelper.restore();
    });

    it('should return successful result', () => {
        result.isSuccess.should.be.true;
    });

    it('should return array data', () => {
        result.data.should.deep.equal(['item1', 'item2']);
    });

    it('should call fetch with correct URL', () => {
        fetchStub.should.have.been.calledOnce;
        const call = fetchStub.getCall(0);
        call.args[0].href.should.equal('https://api.example.com/api/items/test-category');
    });
}));