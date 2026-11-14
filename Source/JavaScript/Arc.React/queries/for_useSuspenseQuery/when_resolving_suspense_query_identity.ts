// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { resolveSuspenseQueryIdentity } from '../resolveSuspenseQueryIdentity.js';

describe('when resolving suspense query identity repeatedly', () => {
    let constructions: number;
    let disabledConstructions: number;
    let first: string;
    let second: string;

    beforeEach(() => {
        constructions = 0;
        class Query {
            readonly queryName = 'Namespace.Probed.Query';
            constructor() { constructions++; }
        }
        resolveSuspenseQueryIdentity(Query, false);
        disabledConstructions = constructions;
        first = resolveSuspenseQueryIdentity(Query);
        second = resolveSuspenseQueryIdentity(Query);
    });

    it('should not probe disabled queries', () => disabledConstructions.should.equal(0));
    it('should prefer the generated query name', () => first.should.equal('Namespace.Probed.Query'));
    it('should return the same identity on render retries', () => second.should.equal(first));
    it('should only construct one metadata probe', () => constructions.should.equal(1));
});
