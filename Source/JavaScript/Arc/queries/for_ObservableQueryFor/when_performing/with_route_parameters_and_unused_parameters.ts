// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { an_observable_query_for } from '../given/an_observable_query_for';
import { given } from '../../../given';
import * as sinon from 'sinon';
import { createFetchHelper } from '../../../helpers/fetchHelper';
import { QueryResult } from '../../QueryResult';
import { expect } from 'chai';

describe('when performing with route parameters and unused parameters', given(an_observable_query_for, context => {
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

        context.query.setOrigin('https://api.example.com');
        context.query.setApiBasePath('/api/v1');

        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        result = await context.query.perform({ id: 'test-id', status: 'completed', priority: 'high' } as any);
    });

    afterEach(() => {
        fetchHelper.restore();
    });

    it('should return successful result', () => {
        expect(result.isSuccess).to.be.true;
    });
    
    it('should return correct data', () => {
        expect(result.data).to.equal('test-result');
    });
    
    it('should include route parameter in path', () => {
        const call = fetchStub.getCall(0);
        call.args[0].href.should.include('/test-id');
    });
    
    it('should include unused status parameter in query string', () => {
        const call = fetchStub.getCall(0);
        call.args[0].href.should.include('status=completed');
    });
    
    it('should include unused priority parameter in query string', () => {
        const call = fetchStub.getCall(0);
        call.args[0].href.should.include('priority=high');
    });
}));
