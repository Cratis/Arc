// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { lengthBasedQueryHttpMethod } from '../../QueryHttpMethodResolver';
import { QueryHttpMethod } from '../../QueryHttpMethod';

describe('when resolving with a long url and a custom method', () => {
    let result: QueryHttpMethod;

    beforeEach(() => {
        const resolve = lengthBasedQueryHttpMethod({ threshold: 100, whenLong: QueryHttpMethod.Query });
        const longValue = 'x'.repeat(500);
        result = resolve({ url: new URL(`https://api.example.com/api/test?search=${longValue}`), route: '/api/test', args: {} });
    });

    it('should use the configured method', () => result.should.equal(QueryHttpMethod.Query));
});
