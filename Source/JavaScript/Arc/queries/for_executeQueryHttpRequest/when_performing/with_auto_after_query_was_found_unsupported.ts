// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as sinon from 'sinon';
import { createFetchHelper } from '../../../helpers/fetchHelper';
import { executeQueryHttpRequest, resetQueryHttpMethodResolution } from '../../QueryHttpRequest';
import { QueryHttpMethod } from '../../QueryHttpMethod';
import { makeOptions } from '../options';

describe('when performing with auto after query was found unsupported', () => {
    let fetchStub: sinon.SinonStub;
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };

    beforeEach(async () => {
        resetQueryHttpMethodResolution();
        fetchHelper = createFetchHelper();
        fetchStub = fetchHelper.stubFetch();
        fetchStub.callsFake((_url: URL, init: RequestInit) =>
            Promise.resolve({ status: init.method === 'QUERY' ? 405 : 200 } as unknown as Response));

        // First Auto query learns that QUERY is unsupported and falls back to GET.
        await executeQueryHttpRequest(QueryHttpMethod.Auto, makeOptions());
        // Second Auto query should go straight to GET without probing QUERY again.
        await executeQueryHttpRequest(QueryHttpMethod.Auto, makeOptions());
    });

    afterEach(() => fetchHelper.restore());

    it('should only attempt QUERY once across both queries', () =>
        fetchStub.getCalls().filter(call => call.args[1].method === 'QUERY').length.should.equal(1));
    it('should send the second query directly as GET', () => fetchStub.getCall(2).args[1].method.should.equal('GET'));
    it('should send three requests in total', () => fetchStub.callCount.should.equal(3));
});
