// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { a_query_for } from '../given/a_query_for';
import { given } from '../../../given';
import * as sinon from 'sinon';
import { createFetchHelper } from '../../../helpers/fetchHelper';
import { QueryResult } from '../../QueryResult';

describe('when performing with partially missing required arguments', given(a_query_for, context => {
    let result: QueryResult<string>;
    let fetchStub: sinon.SinonStub;
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };

    beforeEach(async () => {
        fetchHelper = createFetchHelper();
        fetchStub = fetchHelper.stubFetch();
        fetchStub.resolves({
            json: sinon.stub().resolves({}),
            ok: true,
            status: 200
        } as unknown as Response);

        result = await context.queryWithMultipleRequiredParameters.perform({
            userId: 'user-1',
            category: ''
        });
    });

    afterEach(() => {
        fetchHelper.restore();
    });

    it('should return unsuccessful result', () => {
        result.isSuccess.should.be.false;
    });

    it('should return default value', () => {
        result.data.should.equal('');
    });

    it('should not call fetch', () => {
        fetchStub.should.not.have.been.called;
    });
}));