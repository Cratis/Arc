// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as sinon from 'sinon';
import { createFetchHelper } from '../../../helpers/fetchHelper';
import { executeQueryHttpRequest, resetQueryHttpMethodResolution } from '../../QueryHttpRequest';
import { QueryHttpMethod } from '../../QueryHttpMethod';
import { makeOptions } from '../options';

describe('when performing with auto and an application error', () => {
    let fetchStub: sinon.SinonStub;
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };
    let result: Response;

    beforeEach(async () => {
        resetQueryHttpMethodResolution();
        fetchHelper = createFetchHelper();
        fetchStub = fetchHelper.stubFetch();
        // 400 is an application-level failure, not a transport signal — it must not trigger a fallback.
        fetchStub.resolves({ status: 400 } as unknown as Response);
        result = await executeQueryHttpRequest(QueryHttpMethod.Auto, makeOptions());
    });

    afterEach(() => fetchHelper.restore());

    it('should send a single request', () => fetchStub.callCount.should.equal(1));
    it('should keep using QUERY', () => fetchStub.getCall(0).args[1].method.should.equal('QUERY'));
    it('should return the QUERY response', () => result.status.should.equal(400));
});
