// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { buildQueryHttpRequest } from '../../QueryHttpRequest';
import { QueryHttpMethod } from '../../QueryHttpMethod';
import { Paging } from '../../Paging';
import { Sorting } from '../../Sorting';

describe('when building with get method', () => {
    let result: { url: URL; init: RequestInit };

    beforeEach(() => {
        result = buildQueryHttpRequest(QueryHttpMethod.Get, {
            route: '/api/test/{id}',
            apiBasePath: '/api/v1',
            origin: 'https://api.example.com',
            args: { id: 'abc', filter: 'active' },
            parameterValues: {},
            paging: Paging.noPaging,
            sorting: Sorting.none,
            headers: { 'Accept': 'application/json' }
        });
    });

    it('should use the GET method', () => result.init.method!.should.equal('GET'));

    it('should substitute route parameters and append remaining arguments to the query string', () =>
        result.url.href.should.equal('https://api.example.com/api/v1/api/test/abc?filter=active'));

    it('should not have a body', () => (result.init.body === undefined).should.be.true);
});
