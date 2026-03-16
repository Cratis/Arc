// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it, beforeEach } from 'vitest';
import { UrlHelpers } from '../../UrlHelpers';

describe('when building query params with mixed array and scalar values', () => {
    let unusedParameters: object;
    let result: URLSearchParams;

    beforeEach(() => {
        unusedParameters = { filter: 'active', ids: [1, 2, 3] };

        result = UrlHelpers.buildQueryParams(unusedParameters);
    });

    it('should contain scalar filter parameter', () => result.get('filter')!.should.equal('active'));
    it('should append first id value', () => result.getAll('ids').should.include('1'));
    it('should append second id value', () => result.getAll('ids').should.include('2'));
    it('should append third id value', () => result.getAll('ids').should.include('3'));
    it('should have three entries for ids key', () => result.getAll('ids').length.should.equal(3));
    it('should have two unique keys', () => Array.from(new Set(result.keys())).length.should.equal(2));
});
