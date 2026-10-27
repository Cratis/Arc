// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SliceLocation } from '@cratis/arc/reflection';
import { SliceScope } from '../SliceScope.js';

describe('when resolving an unchanged location again', () => {
    let scope: SliceScope;
    let first: SliceLocation;
    let second: SliceLocation;

    beforeEach(() => {
        scope = new SliceScope();
        scope.register(new SliceLocation('Billing', 'Invoices', 'Pay'));
        first = scope.getSnapshot();
        scope.register(new SliceLocation('Billing', 'Invoices', 'Pay'));
        second = scope.getSnapshot();
    });

    it('should keep the same instance while the location is unchanged', () => second.should.equal(first));
    it('should resolve the registered location', () => first.toString().should.equal('Billing/Invoices/Pay'));
});
