// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it, beforeEach } from 'vitest';
import { UrlHelpers } from '../../UrlHelpers';

describe('when replacing route parameters with array values', () => {
    let route: string;
    let parameters: object;
    let result: { route: string; unusedParameters: object };

    beforeEach(() => {
        route = '/api/search?names={names}&ids={ids}';
        parameters = { names: ['Alice', 'Bob'], ids: [1, 2, 3] };

        result = UrlHelpers.replaceRouteParameters(route, parameters);
    });

    it('should leave route template placeholders untouched for array values', () => result.route.should.equal('/api/search?names={names}&ids={ids}'));
    it('should keep all array parameters as unused', () => Object.keys(result.unusedParameters).length.should.equal(2));
    it('should contain names array in unused parameters', () => (result.unusedParameters as Record<string, string[]>)['names'].should.deep.equal(['Alice', 'Bob']));
    it('should contain ids array in unused parameters', () => (result.unusedParameters as Record<string, number[]>)['ids'].should.deep.equal([1, 2, 3]));
});
