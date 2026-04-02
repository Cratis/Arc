// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryInstanceCache } from '../../QueryInstanceCache';

describe('when disposing an empty cache', () => {
    let cache: QueryInstanceCache;
    let threwError: boolean;

    beforeEach(() => {
        threwError = false;
        cache = new QueryInstanceCache();
        try {
            cache.dispose();
        } catch {
            threwError = true;
        }
    });

    it('should not throw', () => threwError.should.be.false);
});
