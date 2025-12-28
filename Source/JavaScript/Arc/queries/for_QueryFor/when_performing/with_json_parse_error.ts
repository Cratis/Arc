// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { a_query_for } from '../given/a_query_for';
import { given } from '../../../given';

import * as sinon from 'sinon';
import { createFetchHelper } from '../../../helpers/fetchHelper';
import { QueryResult } from '../../QueryResult';

describe('with json parse error', given(a_query_for, context => {
    let result: QueryResult<string>;
    let fetchStub: sinon.SinonStub;
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };

    beforeEach(async () => {
        // Setup fetch mock with json parse error using helper
        fetchHelper = createFetchHelper();
        fetchStub = fetchHelper.stubFetch();
        fetchStub.resolves({
            json: sinon.stub().rejects(new Error('Invalid JSON')),
            ok: true,
            status: 200
        } as unknown as Response);

        context.query.setOrigin('https://api.example.com');

        // Call perform with valid arguments
        result = await context.query.perform({ id: 'test-id' });
    });

    afterEach(() => {
        fetchHelper.restore();
    });

    it('should return no success result', () => {
        result.isSuccess.should.be.false;
    });

    it('should return default value', () => {
        result.data.should.equal('');
    });

    it('should return result without data', () => {
        result.data.should.equal('');
        result.isSuccess.should.be.false;
    });
}));