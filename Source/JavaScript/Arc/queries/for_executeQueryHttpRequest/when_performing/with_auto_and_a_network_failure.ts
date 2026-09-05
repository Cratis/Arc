// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as sinon from 'sinon';
import { createFetchHelper } from '../../../helpers/fetchHelper';
import { executeQueryHttpRequest, resetQueryHttpMethodResolution } from '../../QueryHttpRequest';
import { QueryHttpMethod } from '../../QueryHttpMethod';
import { makeOptions } from '../options';

describe('when performing with auto and a network failure', () => {
    let fetchStub: sinon.SinonStub;
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };
    let result: Response;

    beforeEach(async () => {
        resetQueryHttpMethodResolution();
        fetchHelper = createFetchHelper();
        fetchStub = fetchHelper.stubFetch();
        fetchStub.callsFake((_url: URL, init: RequestInit) =>
            init.method === 'QUERY'
                ? Promise.reject(new TypeError('Failed to fetch'))
                : Promise.resolve({ status: 200 } as unknown as Response));
        result = await executeQueryHttpRequest(QueryHttpMethod.Auto, makeOptions());
    });

    afterEach(() => fetchHelper.restore());

    it('should attempt QUERY first', () => fetchStub.getCall(0).args[1].method.should.equal('QUERY'));
    it('should fall back to GET on the thrown error', () => fetchStub.getCall(1).args[1].method.should.equal('GET'));
    it('should return the GET response', () => result.status.should.equal(200));
});
