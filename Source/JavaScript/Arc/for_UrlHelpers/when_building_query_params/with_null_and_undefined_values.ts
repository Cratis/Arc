// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it, beforeEach } from 'vitest';
import { UrlHelpers } from '../../UrlHelpers.js';

describe('when building query params with null and undefined values', () => {
    let unusedParameters: object;
    let result: URLSearchParams;

    beforeEach(() => {
        unusedParameters = { filter: 'active', nullValue: null, undefinedValue: undefined, search: 'test' };

        result = UrlHelpers.buildQueryParams(unusedParameters);
    });

    it('should contain filter parameter', () => result.get('filter')!.should.equal('active'));
    it('should contain search parameter', () => result.get('search')!.should.equal('test'));
    it('should not contain null value', () => (result.get('nullValue') === null).should.be.true);
    it('should not contain undefined value', () => (result.get('undefinedValue') === null).should.be.true);
    it('should have two parameters', () => Array.from(result.keys()).length.should.equal(2));
});
