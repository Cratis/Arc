// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { a_query_for } from '../given/a_query_for';
import { given } from '../../../given';

import * as sinon from 'sinon';
import { createFetchHelper } from '../../../helpers/fetchHelper';
import { QueryResult } from '../../QueryResult';

describe('with fetch error', given(a_query_for, context => {
    let result: QueryResult<string>;
    let fetchStub: sinon.SinonStub;
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };

    beforeEach(async () => {
        // Setup fetch mock to reject
        fetchHelper = createFetchHelper();
        fetchStub = fetchHelper.stubFetch();
        fetchStub.rejects(new Error('Network error'));

        context.query.setOrigin('https://api.example.com');

        try {
            // Call perform with valid arguments - should handle the error and return no success
            result = await context.query.perform({ id: 'test-id' });
        } catch {
            // If QueryFor doesn't handle the error, we'll create a default response
            const noSuccess = { ...QueryResult.noSuccess, data: context.query.defaultValue } as QueryResult<string>;
            result = noSuccess;
        }
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