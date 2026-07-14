// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { lengthBasedQueryHttpMethod } from '../../QueryHttpMethodResolver';
import { QueryHttpMethod } from '../../QueryHttpMethod';

describe('when resolving with a short url', () => {
    let result: QueryHttpMethod;

    beforeEach(() => {
        const resolve = lengthBasedQueryHttpMethod({ threshold: 100 });
        result = resolve({ url: new URL('https://api.example.com/api/test?id=1'), route: '/api/test', args: {} });
    });

    it('should choose GET', () => result.should.equal(QueryHttpMethod.Get));
});
