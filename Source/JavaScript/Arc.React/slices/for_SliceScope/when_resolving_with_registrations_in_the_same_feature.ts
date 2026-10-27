// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SliceLocation } from '@cratis/arc/reflection';
import { SliceScope } from '../SliceScope.js';

describe('when resolving with registrations in the same feature', () => {
    let result: SliceLocation;

    beforeEach(() => {
        const scope = new SliceScope();
        scope.register(new SliceLocation('Billing', 'Invoices', 'Pay'));
        scope.register(new SliceLocation('Billing', 'Invoices', 'Listing'));
        result = scope.resolve();
    });

    it('should keep the module', () => result.module.should.equal('Billing'));
    it('should keep the feature', () => result.feature.should.equal('Invoices'));
    it('should leave the slice empty', () => result.slice.should.equal(''));
});
