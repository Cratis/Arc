// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { buildQueryHttpRequest } from '../../QueryHttpRequest';
import { QueryHttpMethod } from '../../QueryHttpMethod';
import { Paging } from '../../Paging';
import { Sorting } from '../../Sorting';
import { SortDirection } from '../../SortDirection';

describe('when building with query method and paging and sorting', () => {
    let result: { url: URL; init: RequestInit };
    /* eslint-disable @typescript-eslint/no-explicit-any */
    let body: any;
    /* eslint-enable @typescript-eslint/no-explicit-any */

    beforeEach(() => {
        result = buildQueryHttpRequest(QueryHttpMethod.Query, {
            route: '/api/all',
            apiBasePath: '',
            origin: 'https://api.example.com',
            args: {},
            parameterValues: {},
            paging: new Paging(2, 25),
            sorting: new Sorting('name', SortDirection.descending),
            headers: {}
        });
        body = JSON.parse(result.init.body as string);
    });

    it('should carry paging in the body', () => body.paging.should.deep.equal({ page: 2, pageSize: 25 }));

    it('should carry sorting in the body', () => body.sorting.should.deep.equal({ field: 'name', direction: 'desc' }));

    it('should not put paging or sorting in the query string', () => result.url.search.should.equal(''));
});
