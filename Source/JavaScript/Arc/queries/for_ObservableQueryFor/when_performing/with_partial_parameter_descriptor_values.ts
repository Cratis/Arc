// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { an_observable_query_for } from '../given/an_observable_query_for';
import { given } from '../../../given';
import * as sinon from 'sinon';
import { createFetchHelper } from '../../../helpers/fetchHelper';
import { QueryResult } from '../../QueryResult';

describe('with partial parameter descriptor values', given(an_observable_query_for, context => {
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

        // Only set one parameter value, leave the other undefined
        context.queryWithParameterDescriptorValues.filter = 'active';
        // limit is not set (undefined)

        result = await context.queryWithParameterDescriptorValues.perform();
    });

    afterEach(() => {
        fetchHelper.restore();
    });

    it('should return successful result', () => {
        result.isSuccess.should.be.true;
    });

    it('should include only set parameter descriptor values in URL', () => {
        fetchStub.should.have.been.calledOnce;
        const call = fetchStub.getCall(0);
        const url = call.args[0].href;
        url.should.include('filter=active');
    });

    it('should not include undefined parameter descriptor values in URL', () => {
        const call = fetchStub.getCall(0);
        const url = call.args[0].href;
        url.should.not.include('limit=');
    });
}));
