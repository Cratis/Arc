// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { an_observable_query_for } from '../given/an_observable_query_for';
import { given } from '../../../given';
import * as sinon from 'sinon';
import { Paging } from '../../Paging';

describe('when performing with paging', given(an_observable_query_for, context => {
    let fetchStub: sinon.SinonStub;
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
            totalItems: 100,
            totalPages: 10,
            page: 2,
            size: 10
        }
    };

    beforeEach(async () => {
        // Restore any existing fetch stub before creating a new one
        if ((globalThis.fetch as sinon.SinonStub)?.restore) {
            (globalThis.fetch as sinon.SinonStub).restore();
        }
        fetchStub = sinon.stub(global, 'fetch');
        fetchStub.resolves({
            json: sinon.stub().resolves(mockResponse),
            ok: true,
            status: 200
        } as unknown as Response);

        context.query.setOrigin('https://api.example.com');
        context.query.paging = new Paging(2, 10);

        await context.query.perform({ id: 'test-id' });
    });

    afterEach(() => {
        fetchStub.restore();
    });

    it('should include page parameter in URL', () => {
        const call = fetchStub.getCall(0);
        call.args[0].href.should.contain('page=2');
    });

    it('should include pageSize parameter in URL', () => {
        const call = fetchStub.getCall(0);
        call.args[0].href.should.contain('pageSize=10');
    });
}));
