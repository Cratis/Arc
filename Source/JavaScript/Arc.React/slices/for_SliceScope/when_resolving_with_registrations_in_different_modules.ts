// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SliceLocation } from '@cratis/arc/reflection';
import { SliceScope } from '../SliceScope.js';

describe('when resolving with registrations in different modules', () => {
    const parentLocation = new SliceLocation('Support', 'Reports', 'Submit');
    let result: SliceLocation;

    beforeEach(() => {
        const parent = new SliceScope();
        parent.register(parentLocation);
        const scope = new SliceScope(parent);
        scope.register(new SliceLocation('Billing', 'Invoices', 'Pay'));
        scope.register(new SliceLocation('Sales', 'Orders', 'Place'));
        result = scope.resolve();
    });

    it('should fall back to the parent location', () => result.equals(parentLocation).should.be.true);
});
