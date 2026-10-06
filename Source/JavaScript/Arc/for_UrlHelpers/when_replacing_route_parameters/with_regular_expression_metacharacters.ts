// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it, expect } from 'vitest';
import { UrlHelpers } from '../../UrlHelpers.js';

describe('when replacing route parameters with regular expression metacharacters', () => {
    for (const key of ['a[', 'a.b', 'a+b', 'a*b', 'a?b', 'a^b', 'a$b', 'a{b}', 'a(b)', 'a|b', 'a\\b', 'a]b']) {
        it(`should replace the literal placeholder for ${key}`, () => {
            const result = UrlHelpers.replaceRouteParameters(`/items/{${key}}/{${key}}`, { [key]: 'value /' });
            expect(result.route).to.equal('/items/value%20%2F/value%20%2F');
            expect(result.unusedParameters).to.deep.equal({});
        });

        it(`should leave ${key} unused on a literal route`, () => {
            const result = UrlHelpers.replaceRouteParameters('/items/snapshot', { [key]: 'value' });
            expect(result.route).to.equal('/items/snapshot');
            expect(result.unusedParameters).to.deep.equal({ [key]: 'value' });
        });
    }

    it('should not match a different placeholder through a wildcard', () => {
        const result = UrlHelpers.replaceRouteParameters('/items/{axb}/{a.b}', { 'a.b': 'value' });
        expect(result.route).to.equal('/items/{axb}/value');
    });

    it('should retain case insensitive matching', () => {
        const result = UrlHelpers.replaceRouteParameters('/items/{A.B}', { 'a.b': 'value' });
        expect(result.route).to.equal('/items/value');
    });
});
