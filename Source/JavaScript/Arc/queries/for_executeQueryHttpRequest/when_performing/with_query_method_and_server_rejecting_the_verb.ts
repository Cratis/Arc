// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as sinon from 'sinon';
import { createFetchHelper } from '../../../helpers/fetchHelper.js';
import { executeQueryHttpRequest, resetQueryHttpMethodResolution } from '../../QueryHttpRequest.js';
import { QueryHttpMethod } from '../../QueryHttpMethod.js';
import { makeOptions } from '../options.js';

describe('when performing with query method and server rejecting the verb', () => {
    let fetchStub: sinon.SinonStub;
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };

    beforeEach(async () => {
        resetQueryHttpMethodResolution();
        fetchHelper = createFetchHelper();
        fetchStub = fetchHelper.stubFetch();
        fetchStub.resolves({ status: 405 } as unknown as Response);
        await executeQueryHttpRequest(QueryHttpMethod.Query, makeOptions());
    });

    afterEach(() => fetchHelper.restore());

    it('should send a single request', () => fetchStub.callCount.should.equal(1));
    it('should not fall back when the method was chosen explicitly', () => fetchStub.getCall(0).args[1].method.should.equal('QUERY'));
});
