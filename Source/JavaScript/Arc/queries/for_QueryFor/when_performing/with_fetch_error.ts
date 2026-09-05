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
        fetchHelper = createFetchHelper();
        fetchStub = fetchHelper.stubFetch();
        fetchStub.rejects(new Error('Network error'));

        context.query.setOrigin('https://api.example.com');

        result = await context.query.perform({ id: 'test-id' });
    });

    afterEach(() => {
        fetchHelper.restore();
    });

    it('should resolve rather than reject', () => (result !== undefined).should.be.true);

    it('should return an unsuccessful result', () => result.isSuccess.should.be.false);

    it('should report that it has exceptions', () => result.hasExceptions.should.be.true);

    it('should carry the transport error message', () => result.exceptionMessages.should.deep.equal(['Network error']));

    it('should return the default value as data', () => result.data.should.equal(''));

    // `hasData` is part of the public `IQueryResult` contract, and a consumer awaiting `perform()`
    // directly reads it off this object rather than off the `QueryResultWithState` the hooks build.
    it('should expose hasData as a boolean', () => (typeof result.hasData).should.equal('boolean'));
}));
