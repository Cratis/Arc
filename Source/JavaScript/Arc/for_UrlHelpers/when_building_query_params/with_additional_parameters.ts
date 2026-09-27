// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it, beforeEach } from 'vitest';
import { UrlHelpers } from '../../UrlHelpers.js';

describe('when building query params with additional parameters', () => {
    let unusedParameters: object;
    let additionalParams: Record<string, string | number>;
    let result: URLSearchParams;

    beforeEach(() => {
        unusedParameters = { filter: 'active' };
        additionalParams = { page: 1, pageSize: 25 };

        result = UrlHelpers.buildQueryParams(unusedParameters, additionalParams);
    });

    it('should contain filter parameter', () => result.get('filter')!.should.equal('active'));
    it('should contain page parameter', () => result.get('page')!.should.equal('1'));
    it('should contain pageSize parameter', () => result.get('pageSize')!.should.equal('25'));
    it('should have three parameters', () => Array.from(result.keys()).length.should.equal(3));
});
