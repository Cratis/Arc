// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it, beforeEach } from 'vitest';
import { UrlHelpers } from '../../UrlHelpers.js';

describe('when replacing route parameters with no parameters', () => {
    let route: string;
    let result: { route: string; unusedParameters: object };

    beforeEach(() => {
        route = '/api/items/{id}';

        result = UrlHelpers.replaceRouteParameters(route);
    });

    it('should return original route', () => result.route.should.equal('/api/items/{id}'));
    it('should have empty unused parameters', () => Object.keys(result.unusedParameters).length.should.equal(0));
});
