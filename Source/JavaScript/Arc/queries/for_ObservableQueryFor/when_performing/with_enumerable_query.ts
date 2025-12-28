// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { an_observable_query_for } from '../given/an_observable_query_for';
import { given } from '../../../given';
import * as sinon from 'sinon';
import { createFetchHelper } from '../../../helpers/fetchHelper';
import { QueryResult } from '../../QueryResult';

describe('when performing with enumerable query', given(an_observable_query_for, context => {
    let result: QueryResult<string[]>;
    let fetchStub: sinon.SinonStub;
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };
    const mockResponse = {
        data: ['item1', 'item2', 'item3'],
        isSuccess: true,
        isAuthorized: true,
        isValid: true,
        hasExceptions: false,
        validationResults: [],
        exceptionMessages: [],
        exceptionStackTrace: '',
        paging: {
            totalItems: 3,
            totalPages: 1,
            page: 0,
            size: 10
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

        context.enumerableQuery.setOrigin('https://api.example.com');
        context.enumerableQuery.setApiBasePath('/api/v1');

        result = await context.enumerableQuery.perform({ category: 'test-category' });
    });

    afterEach(() => {
        fetchHelper.restore();
    });

    it('should return successful result', () => {
        result.isSuccess.should.be.true;
    });

    it('should return array data', () => {
        result.data.should.be.an('array');
        result.data.should.have.lengthOf(3);
    });

    it('should contain correct items', () => {
        result.data[0].should.equal('item1');
        result.data[1].should.equal('item2');
        result.data[2].should.equal('item3');
    });
}));
