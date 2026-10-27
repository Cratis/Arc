// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SliceLocation } from '@cratis/arc/reflection';
import { SliceScope, SliceScopeKind } from '../SliceScope.js';

describe('when resolving an explicit scope', () => {
    const declared = new SliceLocation('Support', 'Reports', '');
    let result: SliceLocation;

    beforeEach(() => {
        const scope = new SliceScope(undefined, declared, SliceScopeKind.explicit);
        scope.register(new SliceLocation('Support', 'Reports', 'Submit'));
        result = scope.resolve();
    });

    it('should resolve to the declared location, ignoring registrations', () => result.should.equal(declared));
});
