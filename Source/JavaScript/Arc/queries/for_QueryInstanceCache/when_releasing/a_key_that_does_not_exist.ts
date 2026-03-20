// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryInstanceCache } from '../../../QueryInstanceCache';

describe('when releasing a key that does not exist', () => {
    let cache: QueryInstanceCache;
    let threwError: boolean;

    beforeEach(() => {
        cache = new QueryInstanceCache();
        threwError = false;
        try {
            cache.release('NonExistentKey::');
        } catch {
            threwError = true;
        }
    });

    it('should not throw', () => threwError.should.be.false);
});
