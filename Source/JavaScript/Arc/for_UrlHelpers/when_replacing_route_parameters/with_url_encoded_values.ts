// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it, beforeEach } from 'vitest';
import { UrlHelpers } from '../../UrlHelpers.js';

describe('when replacing route parameters with url encoded values', () => {
    let route: string;
    let parameters: object;
    let result: { route: string; unusedParameters: object };

    beforeEach(() => {
        route = '/api/items/{id}';
        parameters = { id: 'test value/with special&chars' };

        result = UrlHelpers.replaceRouteParameters(route, parameters);
    });

    it('should replace and encode route parameter', () => result.route.should.equal('/api/items/test%20value%2Fwith%20special%26chars'));
});
