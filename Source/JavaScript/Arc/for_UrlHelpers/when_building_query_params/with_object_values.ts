// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { UrlHelpers } from '../../UrlHelpers.js';

const parts = { quoted: '"abc"', number: '42', comma: 'a,b' };

describe('when building query params with dictionary values', () => {
    it('should encode one JSON object value', () => {
        const result = UrlHelpers.buildQueryParams({ parts });
        result.getAll('parts').should.deep.equal([JSON.stringify(parts)]);
    });

    it('should serialize an empty dictionary', () => {
        UrlHelpers.buildQueryParams({ parts: {} }).get('parts')!.should.equal('{}');
    });

    it('should preserve existing route parameter serialization', () => {
        const result = UrlHelpers.replaceRouteParameters('/parts/{parts}', { parts });
        result.route.should.equal('/parts/%5Bobject%20Object%5D');
    });

    it('should preserve existing scalar and array serialization', () => {
        const result = UrlHelpers.buildQueryParams({ count: 42, enabled: true, names: ['a', 'b'] });
        result.get('count')!.should.equal('42');
        result.get('enabled')!.should.equal('true');
        result.getAll('names').should.deep.equal(['a', 'b']);
    });
});
