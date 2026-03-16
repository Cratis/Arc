// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it, beforeEach } from 'vitest';
import { UrlHelpers } from '../../UrlHelpers';

describe('when building query params with array values', () => {
    let unusedParameters: object;
    let result: URLSearchParams;

    beforeEach(() => {
        unusedParameters = { names: ['Alice', 'Bob', 'Charlie'] };

        result = UrlHelpers.buildQueryParams(unusedParameters);
    });

    it('should append first value', () => result.getAll('names').should.include('Alice'));
    it('should append second value', () => result.getAll('names').should.include('Bob'));
    it('should append third value', () => result.getAll('names').should.include('Charlie'));
    it('should have three entries for the key', () => result.getAll('names').length.should.equal(3));
    it('should have only one unique key', () => Array.from(new Set(result.keys())).length.should.equal(1));
});
