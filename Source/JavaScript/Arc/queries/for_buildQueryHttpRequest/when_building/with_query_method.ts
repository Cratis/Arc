// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { buildQueryHttpRequest } from '../../QueryHttpRequest';
import { QueryHttpMethod } from '../../QueryHttpMethod';
import { Paging } from '../../Paging';
import { Sorting } from '../../Sorting';

describe('when building with query method', () => {
    let result: { url: URL; init: RequestInit };
    /* eslint-disable @typescript-eslint/no-explicit-any */
    let body: any;
    /* eslint-enable @typescript-eslint/no-explicit-any */

    beforeEach(() => {
        result = buildQueryHttpRequest(QueryHttpMethod.Query, {
            route: '/api/test/{id}',
            apiBasePath: '/api/v1',
            origin: 'https://api.example.com',
            args: { id: 'abc', filter: 'active' },
            parameterValues: {},
            paging: Paging.noPaging,
            sorting: Sorting.none,
            headers: { 'Accept': 'application/json' }
        });
        body = JSON.parse(result.init.body as string);
    });

    it('should use the QUERY method', () => result.init.method!.should.equal('QUERY'));

    it('should substitute route parameters in the path', () =>
        result.url.href.should.equal('https://api.example.com/api/v1/api/test/abc'));

    it('should not put arguments in the query string', () => result.url.search.should.equal(''));

    it('should carry non-route arguments in the body', () => body.arguments.filter.should.equal('active'));

    it('should not carry route arguments in the body', () => (body.arguments.id === undefined).should.be.true);

    it('should set the content type to json', () =>
        new Headers(result.init.headers).get('Content-Type')!.should.equal('application/json'));
});
