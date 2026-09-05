// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as sinon from 'sinon';
import { createFetchHelper } from '../../../helpers/fetchHelper';
import { executeQueryHttpRequest, resetQueryHttpMethodResolution } from '../../QueryHttpRequest';
import { QueryHttpMethod } from '../../QueryHttpMethod';
import { Globals } from '../../../Globals';
import { makeOptions } from '../options';

describe('when performing with a resolver and an explicit override', () => {
    let fetchStub: sinon.SinonStub;
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };

    beforeEach(async () => {
        resetQueryHttpMethodResolution();
        fetchHelper = createFetchHelper();
        fetchStub = fetchHelper.stubFetch();
        fetchStub.resolves({ status: 200 } as unknown as Response);
        Globals.queryHttpMethodResolver = () => QueryHttpMethod.Query;
        // An explicit per-query override must win over the resolver.
        await executeQueryHttpRequest(QueryHttpMethod.Get, makeOptions());
    });

    afterEach(() => {
        Globals.queryHttpMethodResolver = undefined;
        fetchHelper.restore();
    });

    it('should honor the explicit override over the resolver', () => fetchStub.getCall(0).args[1].method.should.equal('GET'));
});
