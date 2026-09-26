// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it, beforeEach } from 'vitest';
import { UrlHelpers } from '../../UrlHelpers.js';

describe('when building query params with empty parameters', () => {
    let result: URLSearchParams;

    beforeEach(() => {
        result = UrlHelpers.buildQueryParams({});
    });

    it('should return empty search params', () => result.toString().should.equal(''));
    it('should have no parameters', () => Array.from(result.keys()).length.should.equal(0));
});
