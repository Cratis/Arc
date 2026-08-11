// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { a_query_for } from '../given/a_query_for';
import { given } from '../../../given';

import * as sinon from 'sinon';
import { createFetchHelper } from '../../../helpers/fetchHelper';

describe('with an aborted request', given(a_query_for, context => {
    let rejection: unknown;
    let fetchStub: sinon.SinonStub;
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };

    beforeEach(async () => {
        fetchHelper = createFetchHelper();
        fetchStub = fetchHelper.stubFetch();
        fetchStub.rejects(Object.assign(new Error('The operation was aborted'), { name: 'AbortError' }));

        context.query.setOrigin('https://api.example.com');

        // An abort only ever happens because a newer request superseded this one, so the outcome is
        // recorded rather than swallowed - the assertions below are on what actually came back.
        rejection = await context.query.perform({ id: 'test-id' }).then(() => undefined, error => error);
    });

    afterEach(() => {
        fetchHelper.restore();
    });

    it('should reject so the superseded request cannot settle a result', () => (rejection !== undefined).should.be.true);

    it('should reject with the abort error', () => (rejection as Error).name.should.equal('AbortError'));
}));
