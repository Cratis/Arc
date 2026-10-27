// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SliceLocation } from '@cratis/arc/reflection';
import { SliceScope } from '../SliceScope.js';

describe('when resolving without registrations', () => {
    const parentLocation = new SliceLocation('Billing', 'Invoices', 'Pay');

    it('should be empty when there is no parent', () => new SliceScope().resolve().should.equal(SliceLocation.empty));
    it('should inherit the location of the parent', () => {
        const parent = new SliceScope();
        parent.register(parentLocation);
        new SliceScope(parent).resolve().equals(parentLocation).should.be.true;
    });
});
