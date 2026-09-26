// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it, beforeEach } from 'vitest';
import { UrlHelpers } from '../../UrlHelpers.js';

describe('when replacing route parameters with all parameters used in route', () => {
    let route: string;
    let parameters: object;
    let result: { route: string; unusedParameters: object };

    beforeEach(() => {
        route = '/api/items/{id}/details/{type}';
        parameters = { id: '123', type: 'full' };

        result = UrlHelpers.replaceRouteParameters(route, parameters);
    });

    it('should replace all route parameters', () => result.route.should.equal('/api/items/123/details/full'));
    it('should have empty unused parameters', () => Object.keys(result.unusedParameters).length.should.equal(0));
});
