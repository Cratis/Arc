// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it, beforeEach, afterEach, expect } from 'vitest';
import * as sinon from 'sinon';
import { QueryFor } from '../../QueryFor.js';
import { QueryHttpMethod } from '../../QueryHttpMethod.js';
import { ParameterDescriptor } from '../../../reflection/ParameterDescriptor.js';
import { createFetchHelper } from '../../../helpers/fetchHelper.js';

class QueryWithLiteralParameterKeys extends QueryFor<string, Record<string, string>> {
    readonly defaultValue = '';
    readonly parameterDescriptors: ParameterDescriptor[] = [];
    readonly requiredRequestParameters: string[] = [];

    constructor(readonly route: string) {
        super(String, false);
    }
}

for (const method of [QueryHttpMethod.Get, QueryHttpMethod.Query]) {
    describe(`when performing ${method} with metacharacters in parameter keys`, () => {
        let fetchHelper: ReturnType<typeof createFetchHelper>;
        let fetchStub: sinon.SinonStub;

        beforeEach(() => {
            fetchHelper = createFetchHelper();
            fetchStub = fetchHelper.stubFetch();
            fetchStub.resolves({
                ok: true,
                status: 200,
                json: sinon.stub().resolves({
                    data: 'result', isSuccess: true, isAuthorized: true, isValid: true,
                    hasExceptions: false, validationResults: [], exceptionMessages: [],
                    exceptionStackTrace: '', paging: { totalItems: 0, totalPages: 0, page: 0, size: 0 }
                })
            } as unknown as Response);
        });

        afterEach(() => fetchHelper.restore());

        for (const route of ['/items/snapshot', '/items/{a[}/{a.b}']) {
            it(`should fetch ${route} with literal keys`, async () => {
                const query = new QueryWithLiteralParameterKeys(route);
                query.setOrigin('https://api.example.com');
                query.setHttpMethod(method);
                const parameters = { 'a[': 'first', 'a.b': 'second' };

                const result = await query.perform(parameters);

                expect(result.isSuccess).to.be.true;
                expect(fetchStub.calledOnce).to.be.true;
                const [url, options] = fetchStub.firstCall.args as [URL, RequestInit];
                expect(options.method).to.equal(method.toUpperCase());
                expect(url.pathname).to.equal(route.includes('{') ? '/items/first/second' : '/items/snapshot');
                if (!route.includes('{')) {
                    if (method === QueryHttpMethod.Get) {
                        expect(url.searchParams.get('a[')).to.equal('first');
                        expect(url.searchParams.get('a.b')).to.equal('second');
                    } else {
                        expect(JSON.parse(options.body as string).arguments).to.deep.equal(parameters);
                    }
                }
            });
        }
    });
}
