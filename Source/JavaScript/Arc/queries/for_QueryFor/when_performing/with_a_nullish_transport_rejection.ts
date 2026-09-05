// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { a_query_for } from '../given/a_query_for';
import { given } from '../../../given';

import * as sinon from 'sinon';
import { createFetchHelper } from '../../../helpers/fetchHelper';
import { QueryResult } from '../../QueryResult';

describe('with a nullish transport rejection', given(a_query_for, context => {
    let outcome: { resolved: boolean; result?: QueryResult<string> };
    let fetchStub: sinon.SinonStub;
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };

    beforeEach(async () => {
        fetchHelper = createFetchHelper();
        fetchStub = fetchHelper.stubFetch();

        // Nothing guarantees a rejection value is an object - `Promise.reject()` and a thrown `undefined`
        // both reach here, and an interceptor or a polyfilled fetch can produce either.
        fetchStub.callsFake(() => Promise.reject(undefined));

        context.query.setOrigin('https://api.example.com');

        outcome = await context.query.perform({ id: 'test-id' }).then(
            result => ({ resolved: true, result }),
            () => ({ resolved: false }));
    });

    afterEach(() => {
        fetchHelper.restore();
    });

    it('should resolve rather than reject', () => outcome.resolved.should.be.true);

    it('should report that it has exceptions', () => outcome.result!.hasExceptions.should.be.true);

    it('should describe the rejection it could not read a message from', () => outcome.result!.exceptionMessages.should.deep.equal(['undefined']));
}));
