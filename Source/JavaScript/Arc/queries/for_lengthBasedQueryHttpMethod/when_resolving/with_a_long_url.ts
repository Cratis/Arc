// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { lengthBasedQueryHttpMethod } from '../../QueryHttpMethodResolver';
import { QueryHttpMethod } from '../../QueryHttpMethod';

describe('when resolving with a long url', () => {
    let result: QueryHttpMethod;

    beforeEach(() => {
        const resolve = lengthBasedQueryHttpMethod({ threshold: 100 });
        const longValue = 'x'.repeat(500);
        result = resolve({ url: new URL(`https://api.example.com/api/test?search=${longValue}`), route: '/api/test', args: {} });
    });

    it('should prefer QUERY with fallback', () => result.should.equal(QueryHttpMethod.Auto));
});
