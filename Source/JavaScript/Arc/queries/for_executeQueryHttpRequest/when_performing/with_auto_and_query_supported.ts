// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as sinon from 'sinon';
import { createFetchHelper } from '../../../helpers/fetchHelper';
import { executeQueryHttpRequest, resetQueryHttpMethodResolution } from '../../QueryHttpRequest';
import { QueryHttpMethod } from '../../QueryHttpMethod';
import { makeOptions } from '../options';

describe('when performing with auto and query supported', () => {
    let fetchStub: sinon.SinonStub;
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };

    beforeEach(async () => {
        resetQueryHttpMethodResolution();
        fetchHelper = createFetchHelper();
        fetchStub = fetchHelper.stubFetch();
        fetchStub.resolves({ status: 200 } as unknown as Response);
        await executeQueryHttpRequest(QueryHttpMethod.Auto, makeOptions());
    });

    afterEach(() => fetchHelper.restore());

    it('should send a single request', () => fetchStub.callCount.should.equal(1));
    it('should use the QUERY method', () => fetchStub.getCall(0).args[1].method.should.equal('QUERY'));
});
