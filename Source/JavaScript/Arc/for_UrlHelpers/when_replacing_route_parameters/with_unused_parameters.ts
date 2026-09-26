// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it, beforeEach } from 'vitest';
import { UrlHelpers } from '../../UrlHelpers.js';

describe('when replacing route parameters with unused parameters', () => {
    let route: string;
    let parameters: object;
    let result: { route: string; unusedParameters: object };

    beforeEach(() => {
        route = '/api/items/{id}';
        parameters = { id: '123', filter: 'active', search: 'test' };

        result = UrlHelpers.replaceRouteParameters(route, parameters);
    });

    it('should replace route parameter', () => result.route.should.equal('/api/items/123'));
    it('should have unused parameters', () => Object.keys(result.unusedParameters).length.should.equal(2));
    it('should contain filter in unused parameters', () => result.unusedParameters.should.have.property('filter', 'active'));
    it('should contain search in unused parameters', () => result.unusedParameters.should.have.property('search', 'test'));
});
