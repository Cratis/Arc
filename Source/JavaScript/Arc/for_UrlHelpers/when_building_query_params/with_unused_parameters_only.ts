// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it, beforeEach } from 'vitest';
import { UrlHelpers } from '../../UrlHelpers.js';

describe('when building query params with unused parameters only', () => {
    let unusedParameters: object;
    let result: URLSearchParams;

    beforeEach(() => {
        unusedParameters = { filter: 'active', search: 'test', limit: 10 };

        result = UrlHelpers.buildQueryParams(unusedParameters);
    });

    it('should contain filter parameter', () => result.get('filter')!.should.equal('active'));
    it('should contain search parameter', () => result.get('search')!.should.equal('test'));
    it('should contain limit parameter', () => result.get('limit')!.should.equal('10'));
    it('should have three parameters', () => Array.from(result.keys()).length.should.equal(3));
});
