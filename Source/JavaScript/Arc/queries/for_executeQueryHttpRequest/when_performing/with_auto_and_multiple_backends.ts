// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as sinon from 'sinon';
import { createFetchHelper } from '../../../helpers/fetchHelper';
import { executeQueryHttpRequest, resetQueryHttpMethodResolution } from '../../QueryHttpRequest';
import { QueryHttpMethod } from '../../QueryHttpMethod';
import { makeOptions } from '../options';

describe('when performing with auto and multiple backends', () => {
    let fetchStub: sinon.SinonStub;
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };
    const unsupportedOrigin = 'https://legacy.example.com';
    const supportedOrigin = 'https://modern.example.com';

    beforeEach(async () => {
        resetQueryHttpMethodResolution();
        fetchHelper = createFetchHelper();
        fetchStub = fetchHelper.stubFetch();
        // The legacy backend rejects QUERY; the modern backend supports it.
        fetchStub.callsFake((url: URL, init: RequestInit) => {
            const rejectsQuery = url.origin === unsupportedOrigin && init.method === 'QUERY';
            return Promise.resolve({ status: rejectsQuery ? 405 : 200 } as unknown as Response);
        });

        // Probe the legacy backend first — it pins to GET.
        await executeQueryHttpRequest(QueryHttpMethod.Auto, makeOptions({ origin: unsupportedOrigin }));
        // The modern backend should still be probed and settle on QUERY.
        await executeQueryHttpRequest(QueryHttpMethod.Auto, makeOptions({ origin: supportedOrigin }));
    });

    afterEach(() => fetchHelper.restore());

    it('should use QUERY for the backend that supports it', () => {
        const modernCalls = fetchStub.getCalls().filter(call => (call.args[0] as URL).origin === supportedOrigin);
        modernCalls.some(call => call.args[1].method === 'QUERY').should.be.true;
    });

    it('should not downgrade the supported backend to GET', () => {
        const modernCalls = fetchStub.getCalls().filter(call => (call.args[0] as URL).origin === supportedOrigin);
        modernCalls.every(call => call.args[1].method === 'QUERY').should.be.true;
    });
});
